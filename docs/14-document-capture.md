# Document & Receipt Capture

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Upload Process

### Supported Input Methods

| Method | Description | Supported Formats |
|--------|-------------|-------------------|
| **Drag & Drop** | Desktop browser upload area | PDF, PNG, JPG, HEIC, TIFF |
| **Mobile Camera** | Direct capture from device camera | Camera output (auto-converted) |
| **Email Forward** | Forward documents to dedicated inbox | PDF, image attachments |
| **API Upload** | Programmatic upload via REST endpoint | All formats |
| **Bulk Import** | ZIP file with multiple documents | ZIP containing supported formats |

### Upload Constraints

| Constraint | Value |
|-----------|-------|
| Max file size | 20 MB per document |
| Max batch size | 50 documents per upload |
| Max resolution | 10,000 x 10,000 pixels |
| Min resolution | 300 x 300 pixels (for OCR quality) |
| Supported formats | PDF, PNG, JPG/JPEG, HEIC, TIFF, BMP |

---

## 2. AI Processing Pipeline

### Step-by-Step Processing

```
Document Processing Pipeline:

1. UPLOAD
   └── File received, virus scanned, stored encrypted

2. PREPROCESSING
   ├── Image enhancement (contrast, rotation, deskew)
   ├── Page detection (multi-page PDF handling)
   └── Quality assessment (reject if unreadable)

3. OCR & EXTRACTION (AI Middleware)
   ├── Text extraction (full OCR)
   ├── Language detection
   ├── Translation (if non-German/English, via DeepL)
   └── Structured field extraction:
       ├── Document type (invoice, receipt, credit note, etc.)
       ├── Vendor/Customer name and address
       ├── Invoice/receipt number
       ├── Date (document date, due date, delivery date)
       ├── Amounts (net, VAT, gross per line item)
       ├── VAT rate and amount
       ├── Currency
       ├── Payment terms
       ├── Bank details (IBAN, BIC)
       ├── Line items (description, quantity, unit price, amount)
       └── PO/reference numbers

4. CLASSIFICATION
   ├── Document type determination
   ├── Vendor matching (against existing vendor database)
   ├── Recurring document detection
   └── Duplicate detection (same vendor, amount, date)

5. BOOKING SUGGESTION
   ├── Account mapping based on vendor + document type + history
   ├── Cost center assignment based on historical patterns
   ├── VAT rate validation
   ├── Split suggestion (if multi-period)
   └── Confidence score generation

6. USER REVIEW
   ├── Present suggestion with extracted data
   ├── Highlight uncertain fields (confidence < 80%)
   └── Accept / Modify / Reject

7. BOOKING
   ├── Create journal entries
   ├── Update affected KPIs
   ├── Store document-to-entry link
   └── Update AI learning model
```

### Field Extraction Confidence

| Confidence Level | Display | Action |
|-----------------|---------|--------|
| > 95% | Green field, auto-filled | User confirms |
| 80-95% | Yellow field, suggested | User verifies |
| 60-80% | Orange field, best guess | User must review |
| < 60% | Red field, manual entry | User must fill in |
| Not found | Empty field | User must fill in |

---

## 3. Booking Suggestion

### Suggestion Display

```
┌─────────────────────────────────────────────────────────────────┐
│ Document Processing Result                    Confidence: 97%   │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ Type: Incoming Invoice                                          │
│ Vendor: Amazon Web Services EMEA SARL         [Matched ✓]      │
│ Invoice No: INV-2026-02-AWS-1234                                │
│ Date: 2026-02-15                                                │
│ Due Date: 2026-03-15                                            │
│                                                                  │
│ Line Items:                                                      │
│   1. EC2 Instances (Feb 2026)          5,200.00 EUR  19% VAT   │
│   2. S3 Storage (Feb 2026)             1,800.00 EUR  19% VAT   │
│   3. CloudFront CDN (Feb 2026)         1,500.00 EUR  19% VAT   │
│   ──────────────────────────────────────────────────────        │
│   Net Total:                           8,500.00 EUR             │
│   VAT (19%):                           1,615.00 EUR             │
│   Gross Total:                        10,115.00 EUR             │
│                                                                  │
│ Suggested Booking:                                               │
│   Debit  4964 (Cloud Infrastructure)   8,500.00 EUR            │
│   Debit  1576 (Input VAT 19%)          1,615.00 EUR            │
│   Credit 1600 (Accounts Payable)      10,115.00 EUR            │
│                                                                  │
│ Cost Center: IT Department                                       │
│ Period Allocation: February 2026 (single month)                 │
│                                                                  │
│ ⓘ This matches recurring pattern: AWS monthly invoice           │
│   Previous months: Jan €7,800 | Dec €7,200 | Nov €6,900        │
│   Trend: +8.9% MoM growth                                       │
│                                                                  │
│ [✓ Accept]  [✎ Modify]  [✗ Reject]  [📋 Split Period]          │
└─────────────────────────────────────────────────────────────────┘
```

### Account Suggestion Logic

```
Account Determination Priority:

1. VENDOR HISTORY: If vendor has been booked before
   → Use most frequent account for this vendor
   → If multiple accounts used: suggest based on line item description

2. KEYWORD MATCHING: Match line item descriptions to account keywords
   → "Cloud", "Server", "AWS" → 4964 (Cloud Infrastructure)
   → "Office supplies" → 4930 (Office Supplies)
   → "Legal consulting" → 4950 (Legal & Consulting)

3. VENDOR CATEGORY: If vendor is categorized in system
   → Category "IT Services" → 4964
   → Category "Marketing" → 4600-4699 range

4. AI CLASSIFICATION: Use AI to classify document content
   → Natural language understanding of invoice context
   → Consider amount, vendor type, and document content

5. FALLBACK: Present most common accounts for manual selection
```

---

## 4. Recurring Document Detection

### Detection Logic

```
Recurring Pattern Detection:

When a new document is processed:
  1. Match vendor (name, tax ID, or IBAN)
  2. Check if vendor has > 2 previous documents
  3. Analyze pattern:
     - Frequency: Monthly, quarterly, annual
     - Amount: Fixed, growing, variable
     - Timing: Day of month consistency
  4. If pattern confidence > 85%:
     - Flag as recurring
     - Offer to create recurring entry

Example:
  Vendor: AWS
  History:
    Nov 2025: 6,900 EUR (15th)
    Dec 2025: 7,200 EUR (15th)
    Jan 2026: 7,800 EUR (15th)
    Feb 2026: 8,500 EUR (15th)  ← Current

  Pattern:
    Frequency: Monthly (15th)
    Amount: Growing (~8.4% MoM average)
    Confidence: 94%

  Prompt: "This appears to be a recurring monthly expense from AWS
           with approximately 8.4% monthly growth.
           Mark as recurring for future cash flow projections?"

  If accepted:
    - Create recurring expense entry
    - Amount: 8,500 EUR base, growth 8.4%/month
    - Account: 4964
    - Auto-match future AWS documents
```

### Variance Alerting

When actual recurring document deviates from expected:

| Deviation | Alert | Action |
|-----------|-------|--------|
| < 5% | None | Auto-process |
| 5-15% | Info | Note on booking suggestion |
| 15-30% | Warning | Require review before booking |
| > 30% | Alert | Notify Finance, require approval |
| Missing (expected not received) | Warning | Remind after expected date + 5 days |

---

## 5. Learning System

### Correction Learning

When a user modifies an AI suggestion:

```json
{
  "documentId": "doc_001",
  "corrections": [
    {
      "field": "account",
      "aiSuggestion": "4964",
      "userCorrection": "4930",
      "reason": "Office supplies, not IT infrastructure"
    },
    {
      "field": "costCenter",
      "aiSuggestion": "IT",
      "userCorrection": "Operations",
      "reason": null
    }
  ],
  "userId": "usr_controller",
  "timestamp": "2026-02-27T10:30:00Z"
}
```

**Learning Outcomes:**
- Update vendor → account mapping weights
- Update keyword → account associations
- Improve cost center prediction model
- Track per-user correction patterns (users may have different preferences)

### Model Performance Metrics

| Metric | Target | Current | Trend |
|--------|--------|---------|-------|
| Field extraction accuracy | > 95% | 93.2% | Improving |
| Account suggestion accuracy | > 85% | 81.5% | Improving |
| Cost center suggestion accuracy | > 80% | 76.0% | Stable |
| Recurring detection accuracy | > 90% | 92.3% | Stable |
| Average corrections per document | < 0.5 | 0.8 | Improving |
| Full auto-process rate (100% confidence) | > 60% | 48% | Improving |

---

## 6. Document Storage & Compliance

### GoBD Compliance (German Digital Bookkeeping)

| Requirement | Implementation |
|-------------|---------------|
| **Integrity** | Documents stored with SHA-256 checksum, hash-chained |
| **Completeness** | All processed documents retained, deletion impossible |
| **Accessibility** | Full-text search, filter by date/vendor/amount/type |
| **Traceability** | Each document linked to journal entries, user actions logged |
| **Immutability** | Processed documents cannot be modified, only annotated |
| **Retention** | 10-year retention period (configurable, minimum 10 years) |
| **Machine Readability** | Extracted data stored in structured format alongside original |
| **Process Documentation** | Processing pipeline documented and auditable |
| **Verifiability** | Original document always accessible for comparison |

### Storage Architecture

```
Document Storage:

/documents/{entity_id}/{year}/{month}/
  ├── {document_id}_original.pdf        (Original upload, immutable)
  ├── {document_id}_processed.json      (Extracted data, immutable)
  ├── {document_id}_thumbnail.png       (Preview image)
  └── {document_id}_audit.json          (Processing audit trail)

Encryption: AES-256 at rest
Access: Role-based (Finance, Admin, Auditor)
Backup: Included in standard backup schedule
Migration: Export capability for system change
```

---

## Document Navigation

- Previous: [Scenario Engine](./13-scenario-engine.md)
- Next: [Budget Planning](./15-budget-planning.md)
- [Back to Index](./README.md)
