import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  useEmployee,
  useSalaryHistory,
  useContractHistory,
  useTerminateEmployee,
} from '@/hooks/useHr';
import type { SalaryEntry, ContractEntry } from '@/types/hr';
import PageHeader from '@/components/shared/PageHeader';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Tabs,
  TabsList,
  TabsTrigger,
  TabsContent,
} from '@/components/ui/tabs';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog';
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
} from '@/components/ui/table';
import { Skeleton } from '@/components/ui/skeleton';
import { ArrowLeft, Loader2 } from 'lucide-react';

// ---------------------------------------------------------------------------
// Formatters
// ---------------------------------------------------------------------------

function formatCents(cents: number): string {
  return (cents / 100).toLocaleString('de-DE', {
    style: 'currency',
    currency: 'EUR',
  });
}

function formatDate(iso: string | undefined): string {
  if (!iso) return '—';
  return new Date(iso).toLocaleDateString('de-DE');
}

function salaryTypeLabel(type: string): string {
  switch (type) {
    case 'Monthly':
      return 'Monatlich';
    case 'Hourly':
      return 'Stündlich';
    case 'DailyRate':
      return 'Tagessatz';
    default:
      return type;
  }
}

function contractTypeLabel(type: string): string {
  switch (type) {
    case 'Permanent':
      return 'Unbefristet';
    case 'FixedTerm':
      return 'Befristet';
    case 'Freelance':
      return 'Freiberuflich';
    case 'WorkingStudent':
      return 'Werkstudent';
    default:
      return type;
  }
}

function employeeTypeLabel(type: string): string {
  return type === 'Contractor' ? 'Contractor' : 'Festangestellt';
}

// ---------------------------------------------------------------------------
// Status badge
// ---------------------------------------------------------------------------

function StatusBadge({ status }: { status: string }) {
  switch (status) {
    case 'Active':
      return (
        <Badge className="bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-300">
          Aktiv
        </Badge>
      );
    case 'OnLeave':
      return (
        <Badge className="bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-300">
          Im Urlaub
        </Badge>
      );
    case 'Terminated':
      return (
        <Badge className="bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400">
          Gekündigt
        </Badge>
      );
    default:
      return <Badge variant="secondary">{status}</Badge>;
  }
}

// ---------------------------------------------------------------------------
// Current badge
// ---------------------------------------------------------------------------

function CurrentBadge({ isCurrent }: { isCurrent: boolean }) {
  if (!isCurrent) return null;
  return (
    <Badge className="bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300">
      Aktuell
    </Badge>
  );
}

// ---------------------------------------------------------------------------
// Termination dialog
// ---------------------------------------------------------------------------

interface TerminationDialogProps {
  open: boolean;
  onClose: () => void;
  employeeId: string;
}

function TerminationDialog({ open, onClose, employeeId }: TerminationDialogProps) {
  const [terminationDate, setTerminationDate] = useState('');
  const [reason, setReason] = useState('');
  const terminateEmployee = useTerminateEmployee();

  const handleClose = () => {
    setTerminationDate('');
    setReason('');
    onClose();
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!terminationDate || !reason) return;

    terminateEmployee.mutate(
      { id: employeeId, terminationDate, reason },
      { onSuccess: handleClose },
    );
  };

  return (
    <Dialog open={open} onOpenChange={(v) => { if (!v) handleClose(); }}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Mitarbeiter kündigen</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="terminationDate">Kündigungsdatum *</Label>
            <Input
              id="terminationDate"
              type="date"
              value={terminationDate}
              onChange={(e) => setTerminationDate(e.target.value)}
              required
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="reason">Grund *</Label>
            <Input
              id="reason"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="Kündigungsgrund eingeben..."
              required
            />
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={handleClose}>
              Abbrechen
            </Button>
            <Button
              type="submit"
              variant="destructive"
              disabled={terminateEmployee.isPending}
            >
              {terminateEmployee.isPending && (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              )}
              Kündigung bestätigen
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ---------------------------------------------------------------------------
// Salary history tab
// ---------------------------------------------------------------------------

function SalaryTab({ entries }: { entries: SalaryEntry[] }) {
  if (entries.length === 0) {
    return (
      <p className="py-8 text-center text-sm text-muted-foreground">
        Keine Gehaltseinträge vorhanden.
      </p>
    );
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Typ</TableHead>
          <TableHead className="text-right">Bruttobetrag</TableHead>
          <TableHead>Gültig ab</TableHead>
          <TableHead>Änderungsgrund</TableHead>
          <TableHead />
        </TableRow>
      </TableHeader>
      <TableBody>
        {entries.map((entry) => (
          <TableRow key={entry.id}>
            <TableCell className="text-sm text-muted-foreground">
              {salaryTypeLabel(entry.salaryType)}
            </TableCell>
            <TableCell className="text-right font-medium tabular-nums">
              {formatCents(entry.grossAmountCents)}
            </TableCell>
            <TableCell className="text-sm">
              {formatDate(entry.validFrom)}
            </TableCell>
            <TableCell className="text-sm text-muted-foreground">
              {entry.changeReason || '—'}
            </TableCell>
            <TableCell>
              <CurrentBadge isCurrent={entry.isCurrent} />
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}

// ---------------------------------------------------------------------------
// Contract history tab
// ---------------------------------------------------------------------------

function ContractTab({ entries }: { entries: ContractEntry[] }) {
  if (entries.length === 0) {
    return (
      <p className="py-8 text-center text-sm text-muted-foreground">
        Keine Vertragseinträge vorhanden.
      </p>
    );
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Vertragsart</TableHead>
          <TableHead className="text-right">Wochenstunden</TableHead>
          <TableHead>Startdatum</TableHead>
          <TableHead>Gültig ab</TableHead>
          <TableHead />
        </TableRow>
      </TableHeader>
      <TableBody>
        {entries.map((entry) => (
          <TableRow key={entry.id}>
            <TableCell className="font-medium">
              {contractTypeLabel(entry.contractType)}
            </TableCell>
            <TableCell className="text-right text-sm tabular-nums">
              {entry.weeklyHours} h
            </TableCell>
            <TableCell className="text-sm">
              {formatDate(entry.startDate)}
            </TableCell>
            <TableCell className="text-sm">
              {formatDate(entry.validFrom)}
            </TableCell>
            <TableCell>
              <CurrentBadge isCurrent={entry.isCurrent} />
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}

// ---------------------------------------------------------------------------
// Profile detail row helper
// ---------------------------------------------------------------------------

function DetailRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
        {label}
      </dt>
      <dd className="mt-1 text-sm font-medium text-foreground">{value}</dd>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Main Component
// ---------------------------------------------------------------------------

export function Component() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [terminateOpen, setTerminateOpen] = useState(false);

  const { data: employee, isLoading } = useEmployee(id ?? '');
  const { data: salaryHistory = [] } = useSalaryHistory(id ?? '');
  const { data: contractHistory = [] } = useContractHistory(id ?? '');

  if (isLoading) {
    return (
      <div>
        <Skeleton className="mb-6 h-8 w-64" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (!employee) {
    return (
      <div className="py-12 text-center text-muted-foreground">
        Mitarbeiter nicht gefunden.
      </div>
    );
  }

  const fullName = `${employee.firstName} ${employee.lastName}`;

  return (
    <div>
      <PageHeader
        title={fullName}
        actions={
          <div className="flex items-center gap-2">
            <StatusBadge status={employee.status} />
            <Button
              variant="outline"
              size="sm"
              onClick={() => navigate('/hr/employees')}
            >
              <ArrowLeft className="mr-1 h-4 w-4" />
              Zurück
            </Button>
          </div>
        }
      />

      <Tabs defaultValue="profil">
        <TabsList className="mb-6">
          <TabsTrigger value="profil">Profil</TabsTrigger>
          <TabsTrigger value="gehalt">Gehalt</TabsTrigger>
          <TabsTrigger value="vertraege">Verträge</TabsTrigger>
        </TabsList>

        {/* ----------------------------------------------------------------- */}
        {/* Profil tab                                                         */}
        {/* ----------------------------------------------------------------- */}
        <TabsContent value="profil">
          <Card>
            <CardContent className="pt-6">
              <dl className="grid grid-cols-2 gap-6">
                <DetailRow
                  label="Personalnummer"
                  value={employee.employeeNumber}
                />
                <DetailRow
                  label="Anstellungsart"
                  value={employeeTypeLabel(employee.employeeType)}
                />
                <DetailRow
                  label="Status"
                  value={<StatusBadge status={employee.status} />}
                />
                <DetailRow
                  label="Einstellungsdatum"
                  value={formatDate(employee.hireDate)}
                />
                <DetailRow
                  label="Geburtsdatum"
                  value={formatDate(employee.dateOfBirth)}
                />
                <DetailRow
                  label="Steuer-ID"
                  value={employee.taxId || '—'}
                />
                <DetailRow
                  label="Abteilung"
                  value={employee.departmentName ?? '—'}
                />
                <DetailRow
                  label="Manager"
                  value={employee.managerName ?? '—'}
                />
              </dl>

              {employee.status !== 'Terminated' && (
                <div className="mt-8 flex justify-end border-t pt-4">
                  <Button
                    variant="destructive"
                    onClick={() => setTerminateOpen(true)}
                  >
                    Mitarbeiter kündigen
                  </Button>
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        {/* ----------------------------------------------------------------- */}
        {/* Gehalt tab                                                         */}
        {/* ----------------------------------------------------------------- */}
        <TabsContent value="gehalt">
          <Card>
            <CardContent className="pt-6">
              <SalaryTab entries={salaryHistory} />
            </CardContent>
          </Card>
        </TabsContent>

        {/* ----------------------------------------------------------------- */}
        {/* Verträge tab                                                       */}
        {/* ----------------------------------------------------------------- */}
        <TabsContent value="vertraege">
          <Card>
            <CardContent className="pt-6">
              <ContractTab entries={contractHistory} />
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      <TerminationDialog
        open={terminateOpen}
        onClose={() => setTerminateOpen(false)}
        employeeId={employee.id}
      />
    </div>
  );
}
