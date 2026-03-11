using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.AI;
using ClarityBoard.Infrastructure.Services.AI;

namespace ClarityBoard.Infrastructure.Tests.Services.AI;

public class PromptBackedAiServiceAdapterTests
{
    [Fact]
    public async Task ExtractDocumentFieldsAsync_ShouldFallbackToLegacyPromptKey_AndParseJson()
    {
        var promptService = new FakePromptAiService
        {
            MissingPromptKeys = ["document_extraction"],
            Responses =
            {
                ["document.ocr_extraction"] = "{" +
                    "\"vendor_name\":\"Acme GmbH\"," +
                    "\"invoice_number\":\"INV-42\"," +
                    "\"invoice_date\":\"2026-03-01\"," +
                    "\"total_amount\":123.45," +
                    "\"currency\":\"EUR\"," +
                    "\"confidence\":0.98}" 
            }
        };

        var sut = new PromptBackedAiServiceAdapter(promptService);

        var result = await sut.ExtractDocumentFieldsAsync("ocr text", "application/pdf", CancellationToken.None);

        Assert.Equal("Acme GmbH", result.VendorName);
        Assert.Equal("INV-42", result.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 3, 1), result.InvoiceDate);
        Assert.Equal(123.45m, result.TotalAmount);
        Assert.Equal("EUR", result.Currency);
        Assert.Equal(0.98m, result.Confidence);
        Assert.Contains("document_extraction", promptService.Calls);
        Assert.Contains("document.ocr_extraction", promptService.Calls);
    }

    [Fact]
    public async Task SuggestBookingAsync_ShouldParseJsonFromCodeFence()
    {
        var promptService = new FakePromptAiService
        {
            Responses =
            {
                ["document.booking_suggestion"] = "```json\n{\"debit_account_number\":\"3400\",\"credit_account_number\":\"1200\",\"amount\":99.95,\"vat_code\":\"VSt19\",\"description\":\"Office supplies\",\"confidence\":0.88,\"reasoning\":\"Standard office expense\"}\n```"
            }
        };

        var sut = new PromptBackedAiServiceAdapter(promptService);
        var extraction = new DocumentExtractionResult { TotalAmount = 99.95m };

        var result = await sut.SuggestBookingAsync(extraction, Guid.NewGuid(), CancellationToken.None);

        Assert.Equal("3400", result.DebitAccountNumber);
        Assert.Equal("1200", result.CreditAccountNumber);
        Assert.Equal(99.95m, result.Amount);
        Assert.Equal("VSt19", result.VatCode);
        Assert.Equal(0.88m, result.Confidence);
    }

    private sealed class FakePromptAiService : IPromptAiService
    {
        public HashSet<string> MissingPromptKeys { get; init; } = [];
        public Dictionary<string, string> Responses { get; init; } = [];
        public List<string> Calls { get; } = [];

        public Task<AiResponse> ExecuteAsync(string promptKey, Dictionary<string, string> variables, CancellationToken ct)
        {
            Calls.Add(promptKey);

            if (MissingPromptKeys.Contains(promptKey))
                throw new InvalidOperationException($"AI prompt '{promptKey}' not found.");

            return Task.FromResult(new AiResponse
            {
                Content = Responses[promptKey],
                UsedProvider = AiProvider.Anthropic,
            });
        }

        public Task<string> EnhancePromptAsync(string currentSystemPrompt, string? userTemplate, string description, string functionDescription, CancellationToken ct)
            => Task.FromResult(currentSystemPrompt);

        public Task<bool> TestProviderAsync(AiProvider provider, CancellationToken ct)
            => Task.FromResult(true);
    }
}