# Zoho Books Webhook-Konfiguration für ClarityBoard

## Voraussetzung: Webhook-Config in ClarityBoard anlegen

Bevor du in Zoho Books konfigurierst, muss die Webhook-Config in ClarityBoard existieren. Das geht per API-Call:

```bash
curl -X POST https://api.clarityboard.net/api/webhookconfig \
  -H "Authorization: Bearer {DEIN-JWT-TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "sourceType": "zoho_books",
    "name": "Zoho Books Rechnungen",
    "secretKey": "{DEIN-WEBHOOK-SECRET}",
    "headerSignatureKey": "X-Webhook-Signature",
    "eventFilter": ["invoice.sent", "invoice.paid"]
  }'
```

Das Secret muss in beiden Systemen identisch sein — Zoho sendet es als Header, ClarityBoard validiert es.

---

## Claude for Chrome Prompt

Den folgenden Prompt in Claude for Chrome einfuegen. Vorher die Platzhalter `{DEINE-ENTITY-GUID}` und `{DEIN-WEBHOOK-SECRET}` mit den echten Werten ersetzen.

---

```
Du bist mein Assistent fuer die Konfiguration einer Webhook-Integration in Zoho Books.

## Ziel

Erstelle eine Workflow-Automatisierungsregel in Zoho Books, die bei Rechnungsereignissen
einen Webhook an mein ClarityBoard-Backend sendet.

## Zugangsdaten & Konfiguration

- ClarityBoard Webhook-URL: https://api.clarityboard.net/api/webhooks/zoho_books/events
- Entity-ID: {DEINE-ENTITY-GUID}
- Webhook-Secret: {DEIN-WEBHOOK-SECRET}

## Was konfiguriert werden muss

### Regel 1: "ClarityBoard – Rechnung versendet"

Gehe zu: Settings → Automation → Workflow Rules → + New Rule

1. **Module:** Invoices
2. **Rule Name:** ClarityBoard – Invoice Sent
3. **Trigger:** When an invoice is sent (Event Based → "Sent")
4. **Conditions:** Keine (alle Rechnungen)
5. **Action → Webhook:**
   - **URL:** `https://api.clarityboard.net/api/webhooks/zoho_books/events`
   - **Method:** POST
   - **Content-Type / Format:** JSON
   - **Custom Headers** (alle drei hinzufuegen):
     - `X-Entity-Id` → `{DEINE-ENTITY-GUID}`
     - `X-Event-Type` → `invoice.sent`
     - `X-Webhook-Signature` → `{DEIN-WEBHOOK-SECRET}`
   - **Payload / Body:** Waehle "Custom Payload" und konfiguriere folgendes JSON:

{
  "event_type": "invoice.sent",
  "invoice": {
    "invoice_id": "${Invoice.Invoice ID}",
    "invoice_number": "${Invoice.Invoice Number}",
    "date": "${Invoice.Invoice Date}",
    "due_date": "${Invoice.Due Date}",
    "customer_name": "${Invoice.Customer Name}",
    "customer_id": "${Invoice.Customer ID}",
    "currency_code": "${Invoice.Currency Code}",
    "sub_total": "${Invoice.SubTotal}",
    "tax_total": "${Invoice.Tax Total}",
    "total": "${Invoice.Total}",
    "balance": "${Invoice.Balance}",
    "status": "${Invoice.Invoice Status}",
    "reference_number": "${Invoice.Reference Number}",
    "is_reverse_charge_applied": "${Invoice.Is Reverse Charge Applied}",
    "line_items": ${Invoice.Line Items}
  }
}

### Regel 2: "ClarityBoard – Rechnung bezahlt" (optional, aber empfohlen)

Gleiche Schritte wie oben, aber:
1. **Rule Name:** ClarityBoard – Invoice Paid
2. **Trigger:** When an invoice is paid (Event Based → "Paid")
3. **X-Event-Type Header:** `invoice.paid`
4. **event_type im Payload:** `"invoice.paid"`

## Wichtige Hinweise

- Die Platzhalter wie `${Invoice.Invoice Number}` sind Zoho-Merge-Fields.
  Zoho zeigt sie oft als Dropdown-Auswahl an — waehle das passende Feld aus
  der Liste, anstatt den Platzhalter manuell zu tippen.
- Falls Zoho kein "Custom Payload" als freies JSON-Feld unterstuetzt, sondern
  nur Key-Value-Paare: Erstelle die Felder einzeln mit den Zoho-Merge-Fields
  und verschachtele sie unter einem "invoice"-Objekt.
- Falls "Line Items" nicht als einzelnes Merge-Field verfuegbar ist, lass es
  weg — die Pflichtfelder sind: invoice_number, date, customer_name,
  sub_total, tax_total, total.
- Stelle sicher, dass die Regel auf "Active" steht.

## Ablauf

Fuehre mich Schritt fuer Schritt durch die Zoho Books UI:
1. Navigiere zu Settings → Automation → Workflow Rules
2. Erstelle Regel 1 (Invoice Sent)
3. Erstelle Regel 2 (Invoice Paid)
4. Bestaetige am Ende, dass beide Regeln aktiv sind

Wenn du ein UI-Element nicht findest oder Zoho die Konfiguration anders
strukturiert als erwartet, beschreibe mir was du siehst und schlage eine
Alternative vor. Ueberspringe nichts stillschweigend.
```
