# Send Test Email Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a "Test" button to `/admin/mail` that lets admins send a test email using the current form values (not saved DB config) to verify SMTP settings before or after saving.

**Architecture:** New `SendTestEmailCommand` on the backend builds an `SmtpClient` directly from request fields (no DB lookup), sends once, returns `{ success, errorMessage }` as HTTP 200 always. Frontend adds `useSendTestEmail` hook + inline `TestEmailDialog` in `MailConfig.tsx` triggered by a manual "Test" button in the form footer.

**Tech Stack:** .NET 9 MediatR, FluentValidation, System.Net.Mail — React 19, TanStack Query `useMutation`, shadcn/ui Dialog, Sonner (no toast here — result shown inline in dialog)

---

### Task 1: Backend — `SendTestEmailCommand`

**Files:**
- Create: `src/backend/src/ClarityBoard.Application/Features/Admin/Mail/Commands/SendTestEmailCommand.cs`

**Step 1: Create the file with full implementation**

```csharp
using ClarityBoard.Application.Common.Attributes;
using FluentValidation;
using MediatR;
using System.Net;
using System.Net.Mail;

namespace ClarityBoard.Application.Features.Admin.Mail.Commands;

[RequirePermission("admin.mail.manage")]
public record SendTestEmailCommand : IRequest<SendTestEmailResult>
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required string FromEmail { get; init; }
    public required string FromName { get; init; }
    public bool EnableSsl { get; init; } = true;
    public required string RecipientEmail { get; init; }
}

public record SendTestEmailResult(bool Success, string? ErrorMessage);

public class SendTestEmailValidator : AbstractValidator<SendTestEmailCommand>
{
    public SendTestEmailValidator()
    {
        RuleFor(x => x.Host).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Port).InclusiveBetween(1, 65535);
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.FromEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.FromName).NotEmpty();
        RuleFor(x => x.RecipientEmail).NotEmpty().EmailAddress();
    }
}

public class SendTestEmailHandler : IRequestHandler<SendTestEmailCommand, SendTestEmailResult>
{
    public async Task<SendTestEmailResult> Handle(SendTestEmailCommand request, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new SmtpClient(request.Host, request.Port)
            {
                Credentials    = new NetworkCredential(request.Username, request.Password),
                EnableSsl      = request.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout        = 15_000,
            };

            using var message = new MailMessage
            {
                From       = new MailAddress(request.FromEmail, request.FromName),
                Subject    = "ClarityBoard — Test Email",
                Body       = "<p>This is a test email from <strong>ClarityBoard</strong> to verify your SMTP configuration is working correctly.</p>",
                IsBodyHtml = true,
            };
            message.To.Add(request.RecipientEmail);

            await client.SendMailAsync(message, cancellationToken);
            return new SendTestEmailResult(true, null);
        }
        catch (Exception ex)
        {
            return new SendTestEmailResult(false, ex.Message);
        }
    }
}
```

**Step 2: Commit**

```bash
git add src/backend/src/ClarityBoard.Application/Features/Admin/Mail/Commands/SendTestEmailCommand.cs
git commit -m "feat: add SendTestEmailCommand"
```

---

### Task 2: Backend — Add endpoint to `MailConfigController`

**Files:**
- Modify: `src/backend/src/ClarityBoard.API/Controllers/MailConfigController.cs`

**Step 1: Add the endpoint after the existing `UpsertConfig` method**

Insert after the closing `}` of `UpsertConfig`:

```csharp
    /// <summary>Sends a test email using the provided SMTP config (not saved to DB).</summary>
    [HttpPost("test")]
    [ProducesResponseType(typeof(SendTestEmailResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<SendTestEmailResult>> TestMailConfig(
        [FromBody] SendTestEmailCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }
```

**Step 2: Commit**

```bash
git add src/backend/src/ClarityBoard.API/Controllers/MailConfigController.cs
git commit -m "feat: add POST /api/admin/mail/test endpoint"
```

---

### Task 3: Frontend — Add `useSendTestEmail` hook

**Files:**
- Modify: `src/frontend/src/hooks/useAdmin.ts`
- Modify: `src/frontend/src/types/admin.ts`

**Step 1: Add types to `admin.ts`**

Add at the end of the file:

```typescript
export interface SendTestEmailRequest {
  host: string;
  port: number;
  username: string;
  password: string;
  fromEmail: string;
  fromName: string;
  enableSsl: boolean;
  recipientEmail: string;
}

export interface SendTestEmailResult {
  success: boolean;
  errorMessage: string | null;
}
```

**Step 2: Add hook to `useAdmin.ts`**

Add the import of the new types at the top (inside the existing import from `@/types/admin`):

```typescript
import type {
  // ...existing...
  SendTestEmailRequest,
  SendTestEmailResult,
} from '@/types/admin';
```

Add the hook in the Mail Config section:

```typescript
export function useSendTestEmail() {
  return useMutation({
    mutationFn: async (request: SendTestEmailRequest) => {
      const { data } = await api.post<SendTestEmailResult>(
        '/admin/mail/test',
        request,
      );
      return data;
    },
  });
}
```

**Step 3: Verify build**

```bash
cd src/frontend && npm run build 2>&1 | tail -5
```

Expected: `✓ built in Xs` — 0 TypeScript errors.

**Step 4: Commit**

```bash
git add src/frontend/src/types/admin.ts src/frontend/src/hooks/useAdmin.ts
git commit -m "feat: add useSendTestEmail hook"
```

---

### Task 4: Frontend — Add Test button + TestEmailDialog to `MailConfig.tsx`

**Files:**
- Modify: `src/frontend/src/features/admin/MailConfig.tsx`

**Step 1: Update imports**

Add to lucide-react imports: `FlaskConical`
Add new hook import: `useSendTestEmail`
Add auth store import: `import { useAuthStore } from '@/stores/authStore';`
Add Dialog imports:

```typescript
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
```

Add `cn` import: `import { cn } from '@/lib/utils';`

**Step 2: Add state inside `Component()`**

Add after existing hook declarations:

```typescript
const sendTest = useSendTestEmail();
const user = useAuthStore((s) => s.user);

const [isTestOpen, setIsTestOpen] = useState(false);
const [testConfig, setTestConfig] = useState<FormValues | null>(null);
const [recipientEmail, setRecipientEmail] = useState('');
const [testResult, setTestResult] = useState<{ success: boolean; errorMessage: string | null } | null>(null);
```

**Step 3: Add handler**

```typescript
const handleOpenTest = () => {
  setTestConfig(getValues());
  setTestResult(null);
  setRecipientEmail(user?.email ?? '');
  setIsTestOpen(true);
};

const handleSendTest = () => {
  if (!testConfig) return;
  sendTest.mutate(
    { ...testConfig, recipientEmail },
    { onSuccess: (result) => setTestResult(result) },
  );
};
```

Note: `getValues` must be destructured from `useForm`:

```typescript
const { register, handleSubmit, setValue, watch, reset, getValues, formState: { errors } } = useForm<FormValues>({ ... });
```

**Step 4: Replace the Submit button section**

Replace:
```tsx
{/* Submit */}
<div className="pt-1">
  <Button type="submit" disabled={upsert.isPending}>
    ...
  </Button>
</div>
```

With:
```tsx
{/* Submit + Test */}
<div className="flex gap-2 pt-1">
  <Button type="submit" disabled={upsert.isPending}>
    {upsert.isPending ? (
      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
    ) : (
      <Save className="mr-2 h-4 w-4" />
    )}
    {config ? 'Save Changes' : 'Save Configuration'}
  </Button>
  <Button type="button" variant="outline" onClick={handleOpenTest}>
    <FlaskConical className="mr-2 h-4 w-4" />
    Test
  </Button>
</div>
```

**Step 5: Add TestEmailDialog before the closing `</div>` of the component**

```tsx
{/* Test Email Dialog */}
<Dialog open={isTestOpen} onOpenChange={(open) => { setIsTestOpen(open); if (!open) setTestResult(null); }}>
  <DialogContent className="max-w-md">
    <DialogHeader>
      <DialogTitle>Send Test Email</DialogTitle>
      <DialogDescription>
        Verify your SMTP configuration by sending a test email using the current form values.
      </DialogDescription>
    </DialogHeader>
    <div className="space-y-4">
      {/* Config summary */}
      <dl className="rounded-md bg-muted/50 p-3 text-sm space-y-1.5">
        <div className="flex justify-between">
          <dt className="text-muted-foreground">Host</dt>
          <dd className="font-mono text-xs">{testConfig?.host}:{testConfig?.port}</dd>
        </div>
        <div className="flex justify-between">
          <dt className="text-muted-foreground">Username</dt>
          <dd className="font-mono text-xs">{testConfig?.username}</dd>
        </div>
        <div className="flex justify-between">
          <dt className="text-muted-foreground">From</dt>
          <dd className="text-xs">{testConfig?.fromName} &lt;{testConfig?.fromEmail}&gt;</dd>
        </div>
        <div className="flex justify-between">
          <dt className="text-muted-foreground">SSL/TLS</dt>
          <dd className="text-xs">{testConfig?.enableSsl ? 'Enabled' : 'Disabled'}</dd>
        </div>
      </dl>

      {/* Recipient */}
      <div className="space-y-1.5">
        <Label htmlFor="testRecipient">Send to</Label>
        <Input
          id="testRecipient"
          type="email"
          autoComplete="email"
          value={recipientEmail}
          onChange={(e) => setRecipientEmail(e.target.value)}
          placeholder="admin@example.com"
        />
      </div>

      {/* Result */}
      {testResult && (
        <div className={cn(
          'rounded-md px-3 py-2 text-sm',
          testResult.success
            ? 'bg-emerald-50 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300'
            : 'bg-red-50 text-red-800 dark:bg-red-950 dark:text-red-300',
        )}>
          {testResult.success
            ? 'Test email sent successfully!'
            : `Failed: ${testResult.errorMessage}`}
        </div>
      )}
    </div>
    <DialogFooter>
      <Button variant="outline" onClick={() => setIsTestOpen(false)}>Close</Button>
      <Button
        onClick={handleSendTest}
        disabled={sendTest.isPending || !recipientEmail}
      >
        {sendTest.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
        Send Test Email
      </Button>
    </DialogFooter>
  </DialogContent>
</Dialog>
```

**Step 6: Verify build**

```bash
cd src/frontend && npm run build 2>&1 | tail -5
```

Expected: `✓ built in Xs` — 0 TypeScript errors.

**Step 7: Commit**

```bash
git add src/frontend/src/features/admin/MailConfig.tsx
git commit -m "feat: add Test button and TestEmailDialog to mail config page"
```

---

### Task 5: Deploy & verify

**Step 1: Run deploy**

```bash
sudo bash deploy.sh
```

**Step 2: Manual verification**

1. Open `https://app.clarityboard.net/admin/mail`
2. Fill in SMTP form (enter password)
3. Click **Test** → dialog opens with config summary + your admin email pre-filled
4. Click **Send Test Email** → loading spinner → success/error message inline
5. Enter an invalid host → error message like "No such host is known" appears inline
6. Close dialog → everything resets cleanly

**Step 3: Push**

Tell Claude Code: `push`
