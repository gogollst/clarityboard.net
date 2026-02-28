import { useState, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { useDropzone } from 'react-dropzone';
import { useEntity } from '@/hooks/useEntity';
import { useUploadDocument } from '@/hooks/useDocuments';
import PageHeader from '@/components/shared/PageHeader';
import StatusBadge from '@/components/shared/StatusBadge';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Progress } from '@/components/ui/progress';
import { cn } from '@/lib/utils';
import { Upload, FileText, Check, X, Loader2 } from 'lucide-react';

const ACCEPTED_TYPES = {
  'application/pdf': ['.pdf'],
  'image/jpeg': ['.jpg', '.jpeg'],
  'image/png': ['.png'],
  'image/tiff': ['.tif', '.tiff'],
};

const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB

export function Component() {
  const { selectedEntityId } = useEntity();
  const uploadMutation = useUploadDocument();
  const [uploadedDoc, setUploadedDoc] = useState<{
    id: string;
    status: string;
  } | null>(null);

  const onDrop = useCallback(
    (acceptedFiles: File[]) => {
      if (!selectedEntityId || acceptedFiles.length === 0) return;

      const file = acceptedFiles[0];
      uploadMutation.mutate(
        { file, entityId: selectedEntityId },
        {
          onSuccess: (doc) => {
            setUploadedDoc({ id: doc.id, status: doc.status });
          },
        },
      );
    },
    [selectedEntityId, uploadMutation],
  );

  const { getRootProps, getInputProps, isDragActive, fileRejections } =
    useDropzone({
      onDrop,
      accept: ACCEPTED_TYPES,
      maxSize: MAX_FILE_SIZE,
      maxFiles: 1,
      disabled: uploadMutation.isPending,
    });

  return (
    <div>
      <PageHeader
        title="Upload Document"
        description="Upload invoices and receipts for AI-powered processing"
      />

      {/* Drag and Drop Zone */}
      <Card>
        <CardContent className="pt-6">
          <div
            {...getRootProps()}
            className={cn(
              'flex flex-col items-center justify-center rounded-lg border-2 border-dashed p-12 text-center transition-colors cursor-pointer',
              isDragActive
                ? 'border-primary bg-primary/5'
                : 'border-border hover:border-primary/50',
              uploadMutation.isPending && 'pointer-events-none opacity-60',
            )}
          >
            <input {...getInputProps()} />
            {uploadMutation.isPending ? (
              <>
                <Loader2 className="mb-4 h-12 w-12 animate-spin text-primary" />
                <p className="text-lg font-medium">Uploading...</p>
                <Progress value={65} className="mt-4 w-64" />
              </>
            ) : uploadMutation.isSuccess && uploadedDoc ? (
              <>
                <Check className="mb-4 h-12 w-12 text-green-500" />
                <p className="text-lg font-medium">Upload Successful</p>
                <div className="mt-2 flex items-center gap-2">
                  <span className="text-sm text-muted-foreground">
                    Status:
                  </span>
                  <StatusBadge
                    status={uploadedDoc.status}
                    variantMap={{
                      uploaded: 'info',
                      processing: 'warning',
                      extracted: 'success',
                      booked: 'success',
                      failed: 'destructive',
                    }}
                  />
                </div>
                <div className="mt-4 flex gap-2">
                  <Link to="/documents">
                    <Button variant="outline">Back to Archive</Button>
                  </Link>
                  <Button
                    onClick={() => {
                      setUploadedDoc(null);
                      uploadMutation.reset();
                    }}
                  >
                    Upload Another
                  </Button>
                </div>
              </>
            ) : (
              <>
                <Upload className="mb-4 h-12 w-12 text-muted-foreground" />
                <p className="text-lg font-medium">
                  {isDragActive
                    ? 'Drop your file here'
                    : 'Drag & drop a file here, or click to select'}
                </p>
                <p className="mt-2 text-sm text-muted-foreground">
                  PDF, JPEG, PNG, or TIFF up to 10MB
                </p>
              </>
            )}
          </div>

          {/* Rejection Errors */}
          {fileRejections.length > 0 && (
            <div className="mt-4 rounded-md border border-red-200 bg-red-50 p-3 dark:border-red-900 dark:bg-red-950">
              <div className="flex items-center gap-2 text-red-700 dark:text-red-300">
                <X className="h-4 w-4" />
                <span className="text-sm font-medium">File rejected</span>
              </div>
              {fileRejections.map(({ file, errors }) => (
                <div key={file.name} className="mt-1">
                  <p className="text-sm text-red-600 dark:text-red-400">
                    <FileText className="mr-1 inline h-3 w-3" />
                    {file.name}
                  </p>
                  {errors.map((err) => (
                    <p
                      key={err.code}
                      className="ml-5 text-xs text-red-500 dark:text-red-400"
                    >
                      {err.message}
                    </p>
                  ))}
                </div>
              ))}
            </div>
          )}

          {/* Upload Error */}
          {uploadMutation.isError && (
            <div className="mt-4 rounded-md border border-red-200 bg-red-50 p-3 dark:border-red-900 dark:bg-red-950">
              <div className="flex items-center gap-2 text-red-700 dark:text-red-300">
                <X className="h-4 w-4" />
                <span className="text-sm font-medium">
                  Upload failed. Please try again.
                </span>
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
