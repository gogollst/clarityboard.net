using System.Diagnostics;
using System.Text.Json;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Messaging;
using ClarityBoard.Domain.Entities.Accounting;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Messaging.Consumers;

public class WebhookProcessorConsumer : IConsumer<ProcessWebhookEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WebhookProcessorConsumer> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public WebhookProcessorConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<WebhookProcessorConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // Source types handled by dedicated consumers — skip in generic processor
    private static readonly HashSet<string> DedicatedSourceTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "zoho_books",
    };

    public async Task Consume(ConsumeContext<ProcessWebhookEvent> context)
    {
        var message = context.Message;

        // Skip source types that have their own dedicated consumer
        if (DedicatedSourceTypes.Contains(message.SourceType))
            return;

        _logger.LogInformation(
            "Processing webhook event {EventId} from {Source} type {EventType}",
            message.WebhookEventId, message.SourceType, message.EventType);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var sw = Stopwatch.StartNew();

        // 1. Load the WebhookEvent from DB
        var webhookEvent = await db.WebhookEvents
            .FirstOrDefaultAsync(e => e.Id == message.WebhookEventId, context.CancellationToken);

        if (webhookEvent is null)
        {
            _logger.LogWarning("Webhook event {EventId} not found in database", message.WebhookEventId);
            return;
        }

        webhookEvent.MarkProcessing();
        await db.SaveChangesAsync(context.CancellationToken);

        try
        {
            // 2. Load MappingRule for the source type + event type
            var mappingRule = await db.MappingRules
                .Where(r =>
                    r.EntityId == message.EntityId
                    && r.SourceType == message.SourceType
                    && r.EventType == message.EventType
                    && r.IsActive)
                .OrderByDescending(r => r.Priority)
                .FirstOrDefaultAsync(context.CancellationToken);

            // 3. If no mapping rule, mark event as "no_mapping" and return
            if (mappingRule is null)
            {
                _logger.LogInformation(
                    "No mapping rule found for source {Source} event type {EventType} on entity {EntityId}",
                    message.SourceType, message.EventType, message.EntityId);
                webhookEvent.MarkNoMapping();
                sw.Stop();
                webhookEvent.SetProcessingDuration((int)sw.ElapsedMilliseconds);
                await db.SaveChangesAsync(context.CancellationToken);
                return;
            }

            webhookEvent.SetMappingRuleId(mappingRule.Id);

            // 4. Parse the JSON payload
            using var jsonDoc = JsonDocument.Parse(webhookEvent.Payload);
            var root = jsonDoc.RootElement;

            // 5. Apply mapping rule transforms (extract fields from JSON using dot-notation paths)
            var fieldMapping = JsonSerializer.Deserialize<Dictionary<string, string>>(
                mappingRule.FieldMapping, JsonOptions)
                ?? throw new InvalidOperationException("FieldMapping is empty or invalid JSON.");

            var extractedValues = new Dictionary<string, string>();
            foreach (var (targetField, sourcePath) in fieldMapping)
            {
                var value = ExtractJsonValue(root, sourcePath);
                if (value is not null)
                    extractedValues[targetField] = value;
            }

            // 6. Create JournalEntry + JournalEntryLines from mapped data
            var entryDate = extractedValues.TryGetValue("entryDate", out var dateStr) && DateOnly.TryParse(dateStr, out var parsedDate)
                ? parsedDate
                : DateOnly.FromDateTime(DateTime.UtcNow);

            var description = extractedValues.GetValueOrDefault("description")
                ?? $"[Webhook] {message.SourceType}.{message.EventType}";

            var amount = extractedValues.TryGetValue("amount", out var amountStr) && decimal.TryParse(amountStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedAmount)
                ? parsedAmount
                : 0m;

            if (amount <= 0)
            {
                _logger.LogWarning(
                    "Webhook event {EventId} has invalid or zero amount, marking as failed",
                    message.WebhookEventId);
                webhookEvent.MarkFailed("Mapped amount is zero or negative.");
                sw.Stop();
                webhookEvent.SetProcessingDuration((int)sw.ElapsedMilliseconds);
                await db.SaveChangesAsync(context.CancellationToken);
                return;
            }

            // Resolve debit/credit accounts from mapping rule or extracted values
            var debitAccountId = mappingRule.DebitAccountId
                ?? (extractedValues.TryGetValue("debitAccountId", out var debitStr) && Guid.TryParse(debitStr, out var parsedDebit)
                    ? parsedDebit
                    : (Guid?)null);

            var creditAccountId = mappingRule.CreditAccountId
                ?? (extractedValues.TryGetValue("creditAccountId", out var creditStr) && Guid.TryParse(creditStr, out var parsedCredit)
                    ? parsedCredit
                    : (Guid?)null);

            if (debitAccountId is null || creditAccountId is null)
            {
                webhookEvent.MarkFailed("Debit or credit account not specified in mapping rule or payload.");
                sw.Stop();
                webhookEvent.SetProcessingDuration((int)sw.ElapsedMilliseconds);
                await db.SaveChangesAsync(context.CancellationToken);
                return;
            }

            // Find or create fiscal period
            var fiscalPeriod = await GetOrCreateFiscalPeriodAsync(db, message.EntityId, entryDate, context.CancellationToken);

            // Get next entry number
            var lastEntryNumber = await db.JournalEntries
                .Where(je => je.EntityId == message.EntityId)
                .MaxAsync(je => (long?)je.EntryNumber, context.CancellationToken) ?? 0;

            var currency = extractedValues.GetValueOrDefault("currency") ?? "EUR";
            var vatCode = mappingRule.VatCode ?? extractedValues.GetValueOrDefault("vatCode");
            var costCenter = mappingRule.CostCenter ?? extractedValues.GetValueOrDefault("costCenter");

            var journalEntry = JournalEntry.Create(
                entityId: message.EntityId,
                entryNumber: lastEntryNumber + 1,
                entryDate: entryDate,
                description: description,
                fiscalPeriodId: fiscalPeriod.Id,
                createdBy: Guid.Empty, // System-generated
                sourceType: "webhook",
                sourceRef: webhookEvent.Id.ToString());

            var debitLine = JournalEntryLine.CreateDebit(
                lineNumber: 1,
                accountId: debitAccountId.Value,
                amount: amount,
                vatCode: vatCode,
                costCenter: costCenter,
                description: description,
                currency: currency);

            var creditLine = JournalEntryLine.CreateCredit(
                lineNumber: 2,
                accountId: creditAccountId.Value,
                amount: amount,
                vatCode: vatCode,
                costCenter: costCenter,
                description: description,
                currency: currency);

            journalEntry.AddLine(debitLine);
            journalEntry.AddLine(creditLine);

            db.JournalEntries.Add(journalEntry);

            // 7. Update WebhookEvent status to "completed"
            webhookEvent.MarkCompleted();
            sw.Stop();
            webhookEvent.SetProcessingDuration((int)sw.ElapsedMilliseconds);
            await db.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "Webhook event {EventId} processed successfully. JournalEntry {EntryId} created in {Duration}ms",
                message.WebhookEventId, journalEntry.Id, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "Error processing webhook event {EventId}: {Error}",
                message.WebhookEventId, ex.Message);

            webhookEvent.MarkFailed(ex.Message);
            webhookEvent.SetProcessingDuration((int)sw.ElapsedMilliseconds);

            // 9. After 3 retries, move to dead_letter status
            if (webhookEvent.RetryCount >= 3)
            {
                _logger.LogWarning(
                    "Webhook event {EventId} exceeded max retries, moving to dead_letter",
                    message.WebhookEventId);
                webhookEvent.MarkDeadLetter();
            }

            await db.SaveChangesAsync(context.CancellationToken);
            throw; // Re-throw so MassTransit handles retry
        }
    }

    /// <summary>
    /// Extracts a value from a JsonElement using a dot-notation path (e.g., "data.object.amount").
    /// </summary>
    private static string? ExtractJsonValue(JsonElement root, string path)
    {
        var segments = path.Split('.');
        var current = root;

        foreach (var segment in segments)
        {
            if (current.ValueKind != JsonValueKind.Object)
                return null;

            if (!current.TryGetProperty(segment, out var next))
            {
                // Try case-insensitive match
                var found = false;
                foreach (var prop in current.EnumerateObject())
                {
                    if (string.Equals(prop.Name, segment, StringComparison.OrdinalIgnoreCase))
                    {
                        current = prop.Value;
                        found = true;
                        break;
                    }
                }
                if (!found) return null;
                continue;
            }

            current = next;
        }

        return current.ValueKind switch
        {
            JsonValueKind.String => current.GetString(),
            JsonValueKind.Number => current.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            _ => current.GetRawText(),
        };
    }

    private static async Task<FiscalPeriod> GetOrCreateFiscalPeriodAsync(
        IAppDbContext db, Guid entityId, DateOnly date, CancellationToken ct)
    {
        var year = (short)date.Year;
        var month = (short)date.Month;

        var fiscalPeriod = await db.FiscalPeriods
            .FirstOrDefaultAsync(fp =>
                fp.EntityId == entityId
                && fp.Year == year
                && fp.Month == month, ct);

        if (fiscalPeriod is not null)
            return fiscalPeriod;

        fiscalPeriod = FiscalPeriod.Create(entityId, year, month);
        db.FiscalPeriods.Add(fiscalPeriod);
        await db.SaveChangesAsync(ct);

        return fiscalPeriod;
    }
}
