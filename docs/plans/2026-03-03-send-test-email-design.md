# Design: Send Test Email

**Date:** 2026-03-03
**Status:** Approved

## Overview

Add a "Send Test Email" feature to the `/admin/mail` page so admins can verify SMTP configuration without saving to the database first.

## Backend

### Endpoint

`POST /api/admin/mail/test`
Permission: `admin.mail.manage`

### Command: `SendTestEmailCommand`

Fields mirror `UpsertMailConfigCommand` plus `RecipientEmail`:

```csharp
Host, Port, Username, Password, FromEmail, FromName, EnableSsl, RecipientEmail
```

### Response: `SendTestEmailResult`

Always HTTP 200 — SMTP errors are user-facing, not exceptional:

```csharp
record SendTestEmailResult(bool Success, string? ErrorMessage);
```

### Handler behaviour

- Creates `SmtpClient` directly from request fields (no DB lookup, no cache, no retry)
- Single send attempt with 15s timeout
- Catches `SmtpException` / `Exception` → returns `Success = false, ErrorMessage = ex.Message`
- On success → returns `Success = true, ErrorMessage = null`
- No `EmailLog` entry written (this is a test, not production traffic)

## Frontend

### Hook: `useSendTestEmail` (in `useAdmin.ts`)

TanStack Query `useMutation` calling `POST /admin/mail/test`.
Returns raw data so the caller can inspect `success` and `errorMessage`.
No automatic toast — the dialog handles feedback itself.

### Test Dialog trigger

Single trigger: manual "Test" button in the form footer (next to "Save Changes" button).
Reads current form values via `getValues()` — password must be filled in the form.
No auto-open after save.

### `TestEmailDialog` component (inline in `MailConfig.tsx`)

- **Read-only summary**: Host:Port, Username, From Name \<From Email\>, SSL on/off
- **Recipient input**: pre-filled with the logged-in admin's `user.email` from `useAuthStore`
- **Send button**: loading state while pending
- **Result feedback**: inline success/error message inside dialog (green / red) — no toast needed since the dialog is already open
- Dialog closes via Cancel or after a successful send

## Files changed

| File | Action |
|---|---|
| `Application/Features/Admin/Mail/Commands/SendTestEmailCommand.cs` | New |
| `API/Controllers/MailConfigController.cs` | Add endpoint |
| `frontend/src/hooks/useAdmin.ts` | Add `useSendTestEmail` |
| `frontend/src/features/admin/MailConfig.tsx` | Add Test button + dialog |
