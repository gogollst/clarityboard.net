import { useState, useRef } from 'react';
import { useParams } from 'react-router-dom';
import { useEmployeeDocuments, useDeleteDocument, useEmployee, useUploadDocument } from '@/hooks/useHr';
import { api } from '@/lib/api';
import PageHeader from '@/components/shared/PageHeader';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
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
import { Download, Trash2, Lock, Upload } from 'lucide-react';
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

  // Upload state
  const [uploadOpen, setUploadOpen] = useState(false);
  const [uploadFile, setUploadFile] = useState<File | null>(null);
  const [uploadTitle, setUploadTitle] = useState('');
  const [uploadType, setUploadType] = useState('other');
  const fileInputRef = useRef<HTMLInputElement>(null);

  const { data: employee } = useEmployee(employeeId ?? '');
  const { data: documents, isLoading, isError } = useEmployeeDocuments(employeeId ?? '');
  const deleteDoc = useDeleteDocument(employeeId ?? '');
  const uploadDocument = useUploadDocument(employeeId ?? '');

  const handleDelete = () => {
    if (!deleteTarget) return;
    deleteDoc.mutate(deleteTarget.id, {
      onSettled: () => setDeleteTarget(null),
    });
  };

  function handleUpload() {
    if (!uploadFile || !employeeId) return;
    const formData = new FormData();
    formData.append('file', uploadFile);
    formData.append('documentType', uploadType);
    formData.append('title', uploadTitle || uploadFile.name);
    uploadDocument.mutate(formData, {
      onSuccess: () => {
        setUploadOpen(false);
        setUploadFile(null);
        setUploadTitle('');
        setUploadType('other');
      },
    });
  }

  async function downloadDocument(docId: string, fileName: string) {
    const response = await api.get(
      `/hr/employees/${employeeId}/documents/${docId}/download`,
      { responseType: 'blob' }
    );
    const url = URL.createObjectURL(response.data);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    a.click();
    URL.revokeObjectURL(url);
  }

  const employeeName = employee
    ? `${employee.firstName} ${employee.lastName}`
    : 'Mitarbeiter';

  return (
    <div className="flex flex-col gap-6 p-6">
      <PageHeader
        title="Dokumente"
        description={`Dokumentenverwaltung für ${employeeName}`}
        actions={
          <Button onClick={() => setUploadOpen(true)}>
            <Upload className="h-4 w-4 mr-2" />
            Dokument hochladen
          </Button>
        }
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
                        <Button
                          variant="ghost"
                          size="icon"
                          title="Herunterladen"
                          onClick={() => downloadDocument(doc.id, doc.fileName)}
                        >
                          <Download className="h-4 w-4" />
                        </Button>
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

      {/* Upload Dialog */}
      <Dialog open={uploadOpen} onOpenChange={(open) => { if (!open) setUploadOpen(false); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Dokument hochladen</DialogTitle>
          </DialogHeader>
          <div className="flex flex-col gap-4 py-2">
            {/* Hidden file input */}
            <input
              ref={fileInputRef}
              type="file"
              className="hidden"
              onChange={(e) => {
                const file = e.target.files?.[0] ?? null;
                setUploadFile(file);
                if (file && !uploadTitle) setUploadTitle(file.name);
              }}
            />
            <div className="flex flex-col gap-1.5">
              <Label>Datei</Label>
              <div className="flex items-center gap-2">
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => fileInputRef.current?.click()}
                >
                  Datei auswählen
                </Button>
                {uploadFile && (
                  <span className="text-sm text-muted-foreground truncate max-w-[200px]">
                    {uploadFile.name}
                  </span>
                )}
              </div>
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="upload-type">Dokumenttyp</Label>
              <Select value={uploadType} onValueChange={setUploadType}>
                <SelectTrigger id="upload-type">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="contract">Vertrag</SelectItem>
                  <SelectItem value="certificate">Zertifikat</SelectItem>
                  <SelectItem value="id_copy">Ausweiskopie</SelectItem>
                  <SelectItem value="payslip">Gehaltszettel</SelectItem>
                  <SelectItem value="other">Sonstiges</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="upload-title">Titel (optional)</Label>
              <Input
                id="upload-title"
                placeholder="Dateiname wird verwendet, wenn leer"
                value={uploadTitle}
                onChange={(e) => setUploadTitle(e.target.value)}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setUploadOpen(false)}>
              Abbrechen
            </Button>
            <Button
              onClick={handleUpload}
              disabled={!uploadFile || uploadDocument.isPending}
            >
              {uploadDocument.isPending ? 'Wird hochgeladen…' : 'Hochladen'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <DeleteDialog
        document={deleteTarget}
        onConfirm={handleDelete}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  );
}
