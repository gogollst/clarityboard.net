# Entity Recognition & Booking Suggestion Improvement — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Extend AI extraction with recipient fields for entity matching, and improve booking suggestions by providing the entity's actual chart of accounts + double-entry bookkeeping rules.

**Architecture:** Two independent improvements to the document processing pipeline. Part 1 adds recipient extraction → LegalEntity matching → UI display. Part 2 enriches the booking suggestion prompt with the entity's real accounts and proper Soll/Haben rules for each document type.

**Tech Stack:** .NET 10 (CQRS/MediatR), PostgreSQL, React 19 + TanStack Query + shadcn/ui, Azure Document Intelligence SDK v1.0.0

---

## Part 1: Recipient Extraction & Entity Matching

### Task 1: Extend `DocumentExtractionResult` with Recipient Fields

**Files:**
- Modify: `src/backend/src/ClarityBoard.Application/Common/Interfaces/IAiService.cs:20-39`

**Step 1: Add recipient properties to `DocumentExtractionResult`**

Add after `VendorBic` (line 29):

```csharp
public string? RecipientName { get; init; }
public string? RecipientTaxId { get; init; }
public string? RecipientVatId { get; init; }
public string? RecipientStreet { get; init; }
public string? RecipientCity { get; init; }
public string? RecipientPostalCode { get; init; }
public string? RecipientCountry { get; init; }
public string? DocumentDirection { get; init; } // "incoming" or "outgoing"
```

**Step 2: Verify build**

Run: `cd /home/stefan/Documents/GitHub/clarityboard.net && grep -r "DocumentExtractionResult" src/backend/src/ --include="*.cs" -l` — check for compile issues.

**Step 3: Commit**

```bash
git add src/backend/src/ClarityBoard.Application/Common/Interfaces/IAiService.cs
git commit -m "feat: add recipient fields and document direction to DocumentExtractionResult"
```

---

### Task 2: Update AI Extraction Prompts & Schemas

**Files:**
- Modify: `src/backend/src/ClarityBoard.Infrastructure/Persistence/Seed/AiPromptsSeed.cs:54-78` (document_extraction prompt)
- Modify: `src/backend/src/ClarityBoard.Infrastructure/Persistence/Seed/AiPromptsSeed.cs:80-104` (document.ocr_extraction prompt)
- Modify: `src/backend/src/ClarityBoard.Infrastructure/Services/AI/ClaudeAiProvider.cs:208-245` (BuildExtractionSchema)

**Step 1: Update both extraction prompts in `AiPromptsSeed.cs`**

Add to the `Rules:` section of both `document_extraction` (line 60-73) and `document.ocr_extraction` (line 86-99):

```
- Extract recipient/customer details: recipient_name, recipient_tax_id (USt-IdNr or Steuernummer), recipient_vat_id, recipient_street, recipient_city, recipient_postal_code, recipient_country (ISO 3166-1 alpha-2)
- Determine document_direction: "incoming" if the document was received (Eingangsrechnung — vendor bills you), "outgoing" if the document was sent (Ausgangsrechnung — you bill a customer)
- The vendor/supplier is the party ISSUING the document; the recipient is the party RECEIVING the document
```

**Step 2: Update `BuildExtractionSchema()` in `ClaudeAiProvider.cs`**

Add after `"raw_fields"` property (line 238):

```json
"recipient_name": { "type": "string", "description": "Name of the recipient/customer" },
"recipient_tax_id": { "type": "string", "description": "Recipient tax ID (Steuernummer)" },
"recipient_vat_id": { "type": "string", "description": "Recipient VAT ID (USt-IdNr)" },
"recipient_street": { "type": "string", "description": "Recipient street address" },
"recipient_city": { "type": "string", "description": "Recipient city" },
"recipient_postal_code": { "type": "string", "description": "Recipient postal code" },
"recipient_country": { "type": "string", "description": "Recipient country (ISO 3166-1 alpha-2)" },
"document_direction": { "type": "string", "enum": ["incoming", "outgoing"], "description": "incoming = Eingangsrechnung (vendor bills you), outgoing = Ausgangsrechnung (you bill customer)" }
```

**Step 3: Commit**

```bash
git add src/backend/src/ClarityBoard.Infrastructure/Persistence/Seed/AiPromptsSeed.cs
git add src/backend/src/ClarityBoard.Infrastructure/Services/AI/ClaudeAiProvider.cs
git commit -m "feat: add recipient fields and document_direction to AI extraction prompts and schemas"
```

---

### Task 3: Update Extraction Response Parsers

**Files:**
- Modify: `src/backend/src/ClarityBoard.Infrastructure/Services/AI/ClaudeAiProvider.cs:270-317` (ParseExtractionResult)
- Modify: `src/backend/src/ClarityBoard.Infrastructure/Services/AI/PromptBackedAiServiceAdapter.cs:12-59` (ExtractDocumentFieldsAsync)

**Step 1: Add recipient parsing in `ClaudeAiProvider.ParseExtractionResult()`**

Add after `VendorBic` (around line 308) in the return statement:

```csharp
RecipientName = GetStringOrNull(root, "recipient_name"),
RecipientTaxId = GetStringOrNull(root, "recipient_tax_id"),
RecipientVatId = GetStringOrNull(root, "recipient_vat_id"),
RecipientStreet = GetStringOrNull(root, "recipient_street"),
RecipientCity = GetStringOrNull(root, "recipient_city"),
RecipientPostalCode = GetStringOrNull(root, "recipient_postal_code"),
RecipientCountry = GetStringOrNull(root, "recipient_country"),
DocumentDirection = GetStringOrNull(root, "document_direction"),
```

Note: `ClaudeAiProvider.ParseExtractionResult()` doesn't currently parse VendorTaxId etc. — it only has VendorName. Add vendor fields too if missing.

**Step 2: Add recipient parsing in `PromptBackedAiServiceAdapter.ExtractDocumentFieldsAsync()`**

Add after the `VendorBic` line (around line 49) in the return statement:

```csharp
RecipientName = GetString(root, "recipient_name", "recipientName", "customer_name")
                ?? GetNestedString(GetNestedObject(root, "recipient", "customer"), "name"),
RecipientTaxId = GetString(root, "recipient_tax_id", "recipientTaxId", "customer_tax_id")
                 ?? GetNestedString(GetNestedObject(root, "recipient", "customer"), "tax_id", "taxId"),
RecipientVatId = GetString(root, "recipient_vat_id", "recipientVatId", "customer_vat_id", "recipient_ust_id_nr")
                 ?? GetNestedString(GetNestedObject(root, "recipient", "customer"), "vat_id", "ust_id_nr"),
RecipientStreet = GetString(root, "recipient_street", "recipientStreet")
                  ?? GetNestedAddressField(GetNestedObject(root, "recipient", "customer"), "street"),
RecipientCity = GetString(root, "recipient_city", "recipientCity")
                ?? GetNestedAddressField(GetNestedObject(root, "recipient", "customer"), "city"),
RecipientPostalCode = GetString(root, "recipient_postal_code", "recipientPostalCode")
                      ?? GetNestedAddressField(GetNestedObject(root, "recipient", "customer"), "postal_code", "postalCode", "zip"),
RecipientCountry = GetString(root, "recipient_country", "recipientCountry")
                   ?? GetNestedAddressField(GetNestedObject(root, "recipient", "customer"), "country"),
DocumentDirection = GetString(root, "document_direction", "documentDirection", "direction"),
```

**Step 3: Commit**

```bash
git add src/backend/src/ClarityBoard.Infrastructure/Services/AI/ClaudeAiProvider.cs
git add src/backend/src/ClarityBoard.Infrastructure/Services/AI/PromptBackedAiServiceAdapter.cs
git commit -m "feat: parse recipient fields in AI extraction response parsers"
```

---

### Task 4: Update Azure Document Intelligence Mapping for Recipient Fields

**Files:**
- Modify: `src/backend/src/ClarityBoard.Infrastructure/Services/Documents/AzureDocIntelligenceService.cs`

**Step 1: Add recipient mapping**

In the `MapInvoiceOrReceiptResult` method, Azure prebuilt-invoice provides fields:
- `CustomerName` → RecipientName
- `CustomerTaxId` → RecipientTaxId
- `CustomerAddress` → RecipientStreet, RecipientCity, RecipientPostalCode, RecipientCountry (via `ValueAddress`)

Add after vendor field extraction:

```csharp
RecipientName = GetStringValue(fields, "CustomerName"),
RecipientTaxId = GetStringValue(fields, "CustomerTaxId"),
```

For `CustomerAddress` (AddressValue type):
```csharp
if (fields.TryGetValue("CustomerAddress", out var custAddrField)
    && custAddrField.FieldType == DocumentFieldType.Address
    && custAddrField.ValueAddress is { } custAddr)
{
    recipientStreet = custAddr.Road;
    recipientCity = custAddr.City;
    recipientPostalCode = custAddr.PostalCode;
    recipientCountry = custAddr.CountryRegion;
}
```

Set `DocumentDirection`:
```csharp
// If vendor fields are populated → incoming (vendor billed us)
// If customer/recipient fields are populated but no vendor → outgoing
DocumentDirection = !string.IsNullOrWhiteSpace(vendorName) ? "incoming"
    : !string.IsNullOrWhiteSpace(recipientName) ? "outgoing"
    : null,
```

**Step 2: Commit**

```bash
git add src/backend/src/ClarityBoard.Infrastructure/Services/Documents/AzureDocIntelligenceService.cs
git commit -m "feat: map recipient fields from Azure Document Intelligence prebuilt-invoice"
```

---

### Task 5: Add Entity Matching Logic

**Files:**
- Modify: `src/backend/src/ClarityBoard.Infrastructure/Messaging/Consumers/DocumentProcessingConsumer.cs`

**Step 1: Add entity matching after extraction**

In `DocumentProcessingConsumer.Consume()`, after the partner matching stage (line ~186) and before the booking suggestion stage (line ~194), add an entity matching stage.

The logic:
1. Determine which party to match against the owning LegalEntity:
   - If `DocumentDirection == "incoming"`: match **recipient** fields against the document's `EntityId` LegalEntity (the recipient is us)
   - If `DocumentDirection == "outgoing"`: match **vendor** fields against the document's `EntityId` LegalEntity (the vendor/issuer is us — unusual, but for Ausgangsrechnungen the company sends the invoice)
   - If direction is null: try both, prefer the one that matches
2. Compare against `LegalEntity` fields: `Name`, `TaxId`, `VatId`
3. If name/tax/vat matches → confirmed (high confidence). If partial match → flag for review.

```csharp
// 6c. Entity recognition — verify this document belongs to the correct entity
currentStage = "entity_recognition";
var entityRecognitionStopwatch = Stopwatch.StartNew();

var entity = await _db.LegalEntities
    .FirstOrDefaultAsync(e => e.Id == entityId, ct);

if (entity is not null)
{
    var (matchConfidence, matchReason) = MatchEntityFields(entity, extraction);

    if (matchConfidence < 0.5m)
    {
        reviewReasons.Add("entity_mismatch_suspected");
        _logger.LogWarning(
            "Document processing stage {Stage} completed in {DurationMs}ms with result {Result} — matchReason: {MatchReason}",
            currentStage, entityRecognitionStopwatch.ElapsedMilliseconds, "low_match", matchReason);
    }
    else
    {
        LogStageInformation(currentStage, entityRecognitionStopwatch.ElapsedMilliseconds, "matched",
            "confidence {Confidence} reason {Reason}", matchConfidence, matchReason);
    }
}
entityRecognitionStopwatch.Stop();
```

**Step 2: Add `MatchEntityFields` private method**

```csharp
private static (decimal Confidence, string Reason) MatchEntityFields(
    LegalEntity entity, DocumentExtractionResult extraction)
{
    // For incoming documents: recipient is us (the entity)
    // For outgoing documents: vendor/issuer is us
    var isIncoming = extraction.DocumentDirection == "incoming"
                     || extraction.DocumentDirection is null; // default assumption

    var nameToMatch = isIncoming ? extraction.RecipientName : extraction.VendorName;
    var taxIdToMatch = isIncoming ? extraction.RecipientTaxId : extraction.VendorTaxId;
    var vatIdToMatch = isIncoming ? extraction.RecipientVatId : null; // VendorTaxId is often USt-IdNr already

    var reasons = new List<string>();
    var totalScore = 0m;
    var checks = 0;

    // Tax ID match (strongest signal)
    if (!string.IsNullOrWhiteSpace(taxIdToMatch) && !string.IsNullOrWhiteSpace(entity.TaxId))
    {
        checks++;
        if (NormalizeTaxId(taxIdToMatch) == NormalizeTaxId(entity.TaxId))
        {
            totalScore += 1.0m;
            reasons.Add("tax_id_match");
        }
    }

    // VAT ID match
    if (!string.IsNullOrWhiteSpace(vatIdToMatch) && !string.IsNullOrWhiteSpace(entity.VatId))
    {
        checks++;
        if (NormalizeVatId(vatIdToMatch) == NormalizeVatId(entity.VatId))
        {
            totalScore += 1.0m;
            reasons.Add("vat_id_match");
        }
    }

    // Name match (fuzzy — company names vary)
    if (!string.IsNullOrWhiteSpace(nameToMatch) && !string.IsNullOrWhiteSpace(entity.Name))
    {
        checks++;
        if (NormalizeCompanyName(nameToMatch).Contains(NormalizeCompanyName(entity.Name))
            || NormalizeCompanyName(entity.Name).Contains(NormalizeCompanyName(nameToMatch)))
        {
            totalScore += 0.8m;
            reasons.Add("name_match");
        }
    }

    if (checks == 0)
        return (0.5m, "no_fields_to_match"); // Can't determine — neutral

    var confidence = totalScore / checks;
    return (confidence, string.Join(",", reasons.Count > 0 ? reasons : ["no_match"]));
}

private static string NormalizeTaxId(string id) => id.Replace("/", "").Replace(" ", "").Replace("-", "").Trim();
private static string NormalizeVatId(string id) => id.Replace(" ", "").Trim().ToUpperInvariant();
private static string NormalizeCompanyName(string name)
    => name.ToLowerInvariant()
        .Replace("gmbh", "").Replace("ag", "").Replace("ug", "")
        .Replace("e.k.", "").Replace("ohg", "").Replace("kg", "")
        .Replace("&", "").Replace("co.", "")
        .Trim();
```

**Step 3: Add `LegalEntities` DbSet if not present in `IAppDbContext`**

Check `IAppDbContext.cs` — if `DbSet<LegalEntity> LegalEntities` is missing, add it.

**Step 4: Commit**

```bash
git add src/backend/src/ClarityBoard.Infrastructure/Messaging/Consumers/DocumentProcessingConsumer.cs
git commit -m "feat: add entity recognition matching in document processing pipeline"
```

---

### Task 6: Store Recipient Fields as DocumentFields

**Files:**
- Modify: `src/backend/src/ClarityBoard.Infrastructure/Messaging/Consumers/DocumentProcessingConsumer.cs:595-640`

**Step 1: Add recipient fields to `StoreDocumentFields`**

After `Add("vendor_country", extraction.VendorCountry);` (line 611), add:

```csharp
Add("recipient_name", extraction.RecipientName);
Add("recipient_tax_id", extraction.RecipientTaxId);
Add("recipient_vat_id", extraction.RecipientVatId);
Add("recipient_street", extraction.RecipientStreet);
Add("recipient_city", extraction.RecipientCity);
Add("recipient_postal_code", extraction.RecipientPostalCode);
Add("recipient_country", extraction.RecipientCountry);
Add("document_direction", extraction.DocumentDirection);
```

**Step 2: Update `CountDetectedFields` to include recipient fields**

After `if (!string.IsNullOrWhiteSpace(extraction.VendorCity)) count++;` (line 585), add:

```csharp
if (!string.IsNullOrWhiteSpace(extraction.RecipientName)) count++;
if (!string.IsNullOrWhiteSpace(extraction.RecipientTaxId)) count++;
if (!string.IsNullOrWhiteSpace(extraction.DocumentDirection)) count++;
```

**Step 3: Commit**

```bash
git add src/backend/src/ClarityBoard.Infrastructure/Messaging/Consumers/DocumentProcessingConsumer.cs
git commit -m "feat: store recipient fields and document direction as document fields"
```

---

## Part 2: Booking Suggestion Improvement

### Task 7: Extend `IAiService.SuggestBookingAsync` Signature

**Files:**
- Modify: `src/backend/src/ClarityBoard.Application/Common/Interfaces/IAiService.cs:11-12`

**Step 1: Add `chartOfAccounts` and `accounts` parameters**

Change:
```csharp
Task<BookingSuggestionResult> SuggestBookingAsync(
    DocumentExtractionResult extraction, Guid entityId, CancellationToken ct);
```

To:
```csharp
Task<BookingSuggestionResult> SuggestBookingAsync(
    DocumentExtractionResult extraction, Guid entityId,
    string chartOfAccounts, IReadOnlyList<AccountInfo> accounts,
    CancellationToken ct);
```

**Step 2: Add `AccountInfo` record**

After `BookingSuggestionResult` (line 59):

```csharp
public record AccountInfo(string AccountNumber, string Name, string AccountType, string? VatDefault);
```

**Step 3: Update all implementations**

Both `ClaudeAiProvider` and `PromptBackedAiServiceAdapter` need the new signature. Adjust their method signatures (implementation in Task 8 & 9).

**Step 4: Commit**

```bash
git add src/backend/src/ClarityBoard.Application/Common/Interfaces/IAiService.cs
git commit -m "feat: extend SuggestBookingAsync with chart of accounts and account list"
```

---

### Task 8: Improve Booking Prompt & Schema

**Files:**
- Modify: `src/backend/src/ClarityBoard.Infrastructure/Persistence/Seed/AiPromptsSeed.cs:147-168` (document.booking_suggestion)
- Modify: `src/backend/src/ClarityBoard.Infrastructure/Services/AI/ClaudeAiProvider.cs:67-88` (SuggestBookingAsync)

**Step 1: Replace the booking suggestion prompt in `AiPromptsSeed.cs`**

Replace the `SystemPrompt` of `document.booking_suggestion` (lines 153-164):

```csharp
SystemPrompt: """
You are a certified German accountant (Buchhalter) with deep expertise in SKR03 and SKR04 chart of accounts and German double-entry bookkeeping (doppelte Buchführung).

Given extracted document data and the entity's available accounts, suggest the correct double-entry booking.

## Double-Entry Bookkeeping Rules (Soll/Haben)

### Eingangsrechnung (incoming invoice — vendor bills us):
- **Soll (Debit):** Aufwandskonto (expense account, class 4xxx in SKR03, e.g. 4400 Bürobedarf, 4200 Raumkosten) or Bestandskonto (asset, e.g. 0400-0690 Anlagevermögen for capital goods)
- **Haben (Credit):** Verbindlichkeiten aus Lieferungen und Leistungen (1600 in SKR03 / 3300 in SKR04) — this is ALWAYS the credit for unpaid incoming invoices
- **Vorsteuer (VSt):** If 19% → VSt19, if 7% → VSt7. Vorsteuer is booked separately to 1576 (SKR03) / 1406 (SKR04)

### Ausgangsrechnung (outgoing invoice — we bill a customer):
- **Soll (Debit):** Forderungen aus Lieferungen und Leistungen (1400 in SKR03 / 1200 in SKR04) — this is ALWAYS the debit for outgoing invoices
- **Haben (Credit):** Erlöskonto (revenue account, class 8xxx in SKR03, e.g. 8400 Erlöse 19% USt)
- **Umsatzsteuer (USt):** If 19% → USt19, if 7% → USt7. USt is booked separately to 1776 (SKR03) / 3806 (SKR04)

### Key Rules:
- ALWAYS use accounts from the provided account list — do NOT invent account numbers
- Match the account to the nature of the expense/revenue (e.g. office supplies → 4930 SKR03, travel → 4660 SKR03)
- For incoming invoices the credit is ALWAYS the payables account (1600/3300), never a revenue account
- For outgoing invoices the debit is ALWAYS the receivables account (1400/1200), never an expense account
- The document_direction field tells you if it's incoming or outgoing

Rules:
- Return ONLY a valid JSON object with snake_case field names
- Use the chart of accounts specified (SKR03 or SKR04)
- Apply correct Vorsteuer (VSt) or Umsatzsteuer (USt) codes: VSt19, VSt7, USt19, USt7
- For tax-exempt transactions use code "steuerfrei"
- Provide detailed reasoning for account selection
- Confidence 0.0–1.0 reflects certainty of the booking recommendation
""",
```

**Step 2: Update UserTemplate to include accounts**

Replace UserTemplate (line 165):

```csharp
UserTemplate: "Suggest a double-entry booking for this extracted document. The entity uses {{chart_of_accounts}}.\n\nAvailable accounts:\n{{accounts_json}}\n\nExtracted document data:\n{{extraction_json}}",
```

**Step 3: Update `ClaudeAiProvider.SuggestBookingAsync()` to include accounts**

```csharp
public async Task<BookingSuggestionResult> SuggestBookingAsync(
    DocumentExtractionResult extraction, Guid entityId,
    string chartOfAccounts, IReadOnlyList<AccountInfo> accounts,
    CancellationToken ct)
{
    var tool = new ClaudeTool
    {
        Name = "suggest_booking",
        Description = "Suggest a double-entry booking for a document based on German SKR03/SKR04 chart of accounts.",
        InputSchema = BuildBookingSchema(),
    };

    var systemPrompt =
        $"You are a German accounting expert familiar with {chartOfAccounts} chart of accounts. " +
        "Given extracted document fields and the entity's available accounts, suggest the appropriate debit and credit accounts. " +
        "For incoming invoices (Eingangsrechnung): debit an expense account, credit the payables account (1600 SKR03 / 3300 SKR04). " +
        "For outgoing invoices (Ausgangsrechnung): debit the receivables account (1400 SKR03 / 1200 SKR04), credit a revenue account. " +
        "ONLY use account numbers from the provided list. Always use the suggest_booking tool to return results.";

    var accountsSummary = string.Join("\n", accounts.Select(a => $"{a.AccountNumber} {a.Name} ({a.AccountType})"));
    var extractionJson = JsonSerializer.Serialize(extraction, JsonOptions);
    var userMessage = $"Chart of accounts: {chartOfAccounts}\n\nAvailable accounts:\n{accountsSummary}\n\nExtracted document data:\n{extractionJson}";

    var response = await SendMessageWithToolAsync(systemPrompt, userMessage, tool, ct);
    return ParseBookingResult(response);
}
```

**Step 4: Commit**

```bash
git add src/backend/src/ClarityBoard.Infrastructure/Persistence/Seed/AiPromptsSeed.cs
git add src/backend/src/ClarityBoard.Infrastructure/Services/AI/ClaudeAiProvider.cs
git commit -m "feat: improve booking suggestion prompt with double-entry rules and account list"
```

---

### Task 9: Update `PromptBackedAiServiceAdapter.SuggestBookingAsync()`

**Files:**
- Modify: `src/backend/src/ClarityBoard.Infrastructure/Services/AI/PromptBackedAiServiceAdapter.cs:61-88`

**Step 1: Update method signature and pass accounts to prompt**

```csharp
public async Task<BookingSuggestionResult> SuggestBookingAsync(
    DocumentExtractionResult extraction, Guid entityId,
    string chartOfAccounts, IReadOnlyList<AccountInfo> accounts,
    CancellationToken ct)
{
    var accountsSummary = string.Join("\n", accounts.Select(a => $"{a.AccountNumber} {a.Name} ({a.AccountType})"));

    var response = await promptAiService.ExecuteAsync(
        "document.booking_suggestion",
        new Dictionary<string, string>
        {
            ["entity_id"] = entityId.ToString(),
            ["chart_of_accounts"] = chartOfAccounts,
            ["accounts_json"] = accountsSummary,
            ["extraction_json"] = JsonSerializer.Serialize(extraction),
        },
        ct);

    using var doc = JsonDocument.Parse(ExtractJsonObject(response.Content));
    var root = doc.RootElement;

    return new BookingSuggestionResult
    {
        DebitAccountNumber = GetString(root, "debit_account_number", "debitAccountNumber", "debit_account"),
        CreditAccountNumber = GetString(root, "credit_account_number", "creditAccountNumber", "credit_account"),
        Amount = GetDecimal(root, "amount") ?? extraction.TotalAmount ?? 0m,
        VatCode = GetString(root, "vat_code", "vatCode", "tax_code"),
        Description = GetString(root, "description", "booking_text"),
        Confidence = GetDecimal(root, "confidence") ?? 0m,
        Reasoning = GetString(root, "reasoning", "rationale"),
    };
}
```

**Step 2: Commit**

```bash
git add src/backend/src/ClarityBoard.Infrastructure/Services/AI/PromptBackedAiServiceAdapter.cs
git commit -m "feat: pass chart of accounts and accounts to PromptBackedAiServiceAdapter booking suggestion"
```

---

### Task 10: Update Document Processing Consumer to Load Accounts

**Files:**
- Modify: `src/backend/src/ClarityBoard.Infrastructure/Messaging/Consumers/DocumentProcessingConsumer.cs:194-215`

**Step 1: Load entity's chart of accounts and accounts before booking suggestion**

Replace the booking suggestion section (lines 194-215). Before the `SuggestBookingAsync` call, load accounts:

```csharp
// 7. Call AI for booking suggestion
currentStage = "suggest_booking";
var suggestBookingStopwatch = Stopwatch.StartNew();
BookingSuggestionResult? bookingSuggestion = null;
var bookingSuggestionCreated = false;
try
{
    // Load entity's chart of accounts and active accounts
    var entity = await _db.LegalEntities
        .FirstOrDefaultAsync(e => e.Id == entityId, ct);
    var chartOfAccounts = entity?.ChartOfAccounts ?? "SKR03";

    var accounts = await _db.Accounts
        .Where(a => a.EntityId == entityId && a.IsActive)
        .OrderBy(a => a.AccountNumber)
        .Select(a => new AccountInfo(a.AccountNumber, a.Name, a.AccountType, a.VatDefault))
        .ToListAsync(ct);

    bookingSuggestion = await _aiService.SuggestBookingAsync(
        extraction, entityId, chartOfAccounts, accounts, ct);
    // ... rest unchanged
```

Note: The `entity` variable may already be loaded from Task 5's entity matching stage. If so, reuse it — don't query twice. Move the LegalEntity query to earlier in the pipeline and share it.

**Step 2: Commit**

```bash
git add src/backend/src/ClarityBoard.Infrastructure/Messaging/Consumers/DocumentProcessingConsumer.cs
git commit -m "feat: load entity accounts and pass to booking suggestion AI"
```

---

### Task 11: Update AI Prompt Seed in Database

**Files:**
- Create: `src/backend/src/ClarityBoard.Infrastructure/Persistence/Migrations/20260313200000_UpdateAiExtractionAndBookingPrompts.sql`

**Step 1: Write SQL migration to update the prompts in the database**

The prompts are seeded on first startup but may already exist. Write an UPDATE statement for both prompt keys:

```sql
-- Update document_extraction prompt to include recipient fields
UPDATE ai.ai_prompts
SET system_prompt = '<new system prompt with recipient rules>',
    user_template = '<new user template>',
    updated_at = NOW()
WHERE prompt_key = 'document_extraction';

UPDATE ai.ai_prompts
SET system_prompt = '<new system prompt with recipient rules>',
    user_template = '<new user template>',
    updated_at = NOW()
WHERE prompt_key = 'document.ocr_extraction';

-- Update booking suggestion prompt with double-entry rules and account list placeholders
UPDATE ai.ai_prompts
SET system_prompt = '<new booking system prompt>',
    user_template = 'Suggest a double-entry booking for this extracted document. The entity uses {{chart_of_accounts}}.\n\nAvailable accounts:\n{{accounts_json}}\n\nExtracted document data:\n{{extraction_json}}',
    updated_at = NOW()
WHERE prompt_key = 'document.booking_suggestion';
```

Use the exact prompt text from Tasks 2 and 8.

**Step 2: Commit**

```bash
git add src/backend/src/ClarityBoard.Infrastructure/Persistence/Migrations/20260313200000_UpdateAiExtractionAndBookingPrompts.sql
git commit -m "chore: add SQL migration to update AI extraction and booking prompts"
```

---

### Task 12: Build Verification & Deploy

**Step 1: Verify Docker build**

```bash
sudo /home/stefan/Documents/GitHub/clarityboard.net/deploy.sh
```

**Step 2: Run SQL migration**

```bash
sudo -u postgres psql -d clarityboard -f src/backend/src/ClarityBoard.Infrastructure/Persistence/Migrations/20260313200000_UpdateAiExtractionAndBookingPrompts.sql
```

**Step 3: Test**

1. Upload an Eingangsrechnung → check logs:
   - Recipient fields extracted (recipient_name, recipient_tax_id in DocumentFields)
   - Document direction = "incoming"
   - Entity matching logged
   - Booking suggestion uses correct Soll/Haben: expense account (Soll) / 1600 Verbindlichkeiten (Haben)

2. Upload an Ausgangsrechnung → check:
   - Booking suggestion: 1400 Forderungen (Soll) / revenue account (Haben)

3. Verify the AI uses actual entity accounts (not invented numbers)

**Step 4: Commit any fixes**

```bash
git add -A
git commit -m "fix: post-deploy adjustments for entity recognition and booking improvement"
```

---

## Critical Files Summary

| File | Change |
|------|--------|
| `Application/Common/Interfaces/IAiService.cs` | Add recipient fields to `DocumentExtractionResult`, add `AccountInfo`, extend `SuggestBookingAsync` signature |
| `Infrastructure/Persistence/Seed/AiPromptsSeed.cs` | Add recipient rules to extraction prompts, rewrite booking prompt with double-entry rules |
| `Infrastructure/Services/AI/ClaudeAiProvider.cs` | Add recipient fields to schema + parser, update `SuggestBookingAsync` |
| `Infrastructure/Services/AI/PromptBackedAiServiceAdapter.cs` | Parse recipient fields, pass accounts to booking prompt |
| `Infrastructure/Services/Documents/AzureDocIntelligenceService.cs` | Map Azure CustomerName/CustomerTaxId/CustomerAddress to recipient fields |
| `Infrastructure/Messaging/Consumers/DocumentProcessingConsumer.cs` | Add entity matching stage, store recipient fields, load accounts for booking |
| `Infrastructure/Persistence/Migrations/20260313200000_*.sql` | Update AI prompts in database |
