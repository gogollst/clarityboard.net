import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { useEmployeeDocuments, useDeleteDocument, useEmployee } from '@/hooks/useHr';
import PageHeader from '@/components/shared/PageHeader';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Skeleton } from '@/components/ui/skeleton';
import { Card, CardContent } from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Download, Trash2, Lock } from 'lucide-react';
import type { EmployeeDocument } from '@/types/hr';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function formatDate(iso: string | undefined): string {
  if (!iso) return '—';
  return new Date(iso).toLocaleDateString('de-DE');
}

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1_048_576) return `${(bytes / 1024).toFixed(0)} KB`;
  return `${(bytes / 1_048_576).toFixed(1)} MB`;
}

function documentTypeLabel(type: string): string {
  switch (type) {
    case 'Contract':     return 'Vertrag';
    case 'Certificate':  return 'Zertifikat';
    case 'IdCopy':       return 'Ausweiskopie';
    case 'Payslip':      return 'Gehaltszettel';
    default:             return type;
  }
}

// ---------------------------------------------------------------------------
// Delete Confirmation Dialog
// ---------------------------------------------------------------------------

interface DeleteDialogProps {
  document: EmployeeDocument | null;
  onConfirm: () => void;
  onCancel: () => void;
}

function DeleteDialog({ document, onConfirm, onCancel }: DeleteDialogProps) {
  return (
    <Dialog open={!!document} onOpenChange={(open) => { if (!open) onCancel(); }}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Dokument löschen?</DialogTitle>
          <DialogDescription>
            Soll das Dokument &quot;{document?.title}&quot; ({document?.fileName}) wirklich gelöscht
            werden? Diese Aktion kann nicht rückgängig gemacht werden.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="outline" onClick={onCancel}>
            Abbrechen
          </Button>
          <Button
            variant="destructive"
            onClick={onConfirm}
          >
            Löschen
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ---------------------------------------------------------------------------
// Main Page Component
// ---------------------------------------------------------------------------

export function Component() {
  const { employeeId } = useParams<{ employeeId: string }>();
  const [deleteTarget, setDeleteTarget] = useState<EmployeeDocument | null>(null);

  const { data: employee } = useEmployee(employeeId ?? '');
  const { data: documents, isLoading, isError } = useEmployeeDocuments(employeeId ?? '');
  const deleteDoc = useDeleteDocument(employeeId ?? '');

  const handleDelete = () => {
    if (!deleteTarget) return;
    deleteDoc.mutate(deleteTarget.id, {
      onSettled: () => setDeleteTarget(null),
    });
  };

  const employeeName = employee
    ? `${employee.firstName} ${employee.lastName}`
    : 'Mitarbeiter';

  return (
    <div className="flex flex-col gap-6 p-6">
      <PageHeader
        title="Dokumente"
        description={`Dokumentenverwaltung für ${employeeName}`}
      />

      <Card>
        <CardContent className="p-0">
          {isLoading && (
            <div className="space-y-2 p-4">
              {[...Array(4)].map((_, i) => (
                <Skeleton key={i} className="h-12 w-full" />
              ))}
            </div>
          )}

          {isError && (
            <div className="p-8 text-center text-sm text-muted-foreground">
              Fehler beim Laden der Dokumente.
            </div>
          )}

          {!isLoading && !isError && documents && documents.length === 0 && (
            <div className="p-8 text-center text-sm text-muted-foreground">
              Keine Dokumente vorhanden.
            </div>
          )}

          {!isLoading && !isError && documents && documents.length > 0 && (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Titel</TableHead>
                  <TableHead>Typ</TableHead>
                  <TableHead>Dateiname</TableHead>
                  <TableHead>Größe</TableHead>
                  <TableHead>Hochgeladen am</TableHead>
                  <TableHead>Läuft ab</TableHead>
                  <TableHead>Flags</TableHead>
                  <TableHead className="text-right">Aktionen</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {documents.map((doc) => (
                  <TableRow key={doc.id} className={doc.deletionScheduledAt ? 'opacity-50' : ''}>
                    <TableCell className="font-medium">{doc.title}</TableCell>
                    <TableCell>
                      <Badge variant="outline">{documentTypeLabel(doc.documentType)}</Badge>
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground max-w-[180px] truncate">
                      {doc.fileName}
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground whitespace-nowrap">
                      {formatFileSize(doc.fileSizeBytes)}
                    </TableCell>
                    <TableCell className="text-sm whitespace-nowrap">
                      {formatDate(doc.uploadedAt)}
                    </TableCell>
                    <TableCell className="text-sm whitespace-nowrap">
                      {doc.expiresAt ? formatDate(doc.expiresAt) : '—'}
                    </TableCell>
                    <TableCell>
                      <div className="flex flex-wrap gap-1">
                        {doc.isConfidential && (
                          <Badge className="bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400 gap-1">
                            <Lock className="h-3 w-3" />
                            Vertraulich
                          </Badge>
                        )}
                        {doc.deletionScheduledAt && (
                          <Badge className="bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400">
                            Zur Löschung vorgesehen
                          </Badge>
                        )}
                      </div>
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-2">
                        <a
                          href={`/api/hr/employees/${employeeId}/documents/${doc.id}/download`}
                          target="_blank"
                          rel="noopener noreferrer"
                        >
                          <Button variant="ghost" size="icon" title="Herunterladen">
                            <Download className="h-4 w-4" />
                          </Button>
                        </a>
                        {!doc.deletionScheduledAt && (
                          <Button
                            variant="ghost"
                            size="icon"
                            title="Löschen"
                            className="text-red-500 hover:text-red-700"
                            onClick={() => setDeleteTarget(doc)}
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <DeleteDialog
        document={deleteTarget}
        onConfirm={handleDelete}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  );
}
