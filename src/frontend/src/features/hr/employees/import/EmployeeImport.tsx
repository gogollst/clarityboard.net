import { useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useDropzone } from 'react-dropzone';
import { useEntity } from '@/hooks/useEntity';
import { useBulkImportEmployees } from '@/hooks/useHr';
import { parseExcel } from './parseExcel';
import { validateRows } from './validateRows';
import { generateTemplate } from './generateTemplate';
import type {
  EmployeeImportRow,
  ImportRowValidation,
  BulkImportEmployeesResponse,
} from '@/types/hr';
import PageHeader from '@/components/shared/PageHeader';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip';
import {
  ArrowLeft,
  Download,
  Upload,
  CheckCircle2,
  XCircle,
  AlertCircle,
  FileSpreadsheet,
} from 'lucide-react';

type Step = 'upload' | 'preview' | 'result';

export function Component() {
  const { t } = useTranslation('hr');
  const navigate = useNavigate();
  const { selectedEntityId } = useEntity();
  const importMutation = useBulkImportEmployees();

  const [step, setStep] = useState<Step>('upload');
  const [rows, setRows] = useState<EmployeeImportRow[]>([]);
  const [validations, setValidations] = useState<ImportRowValidation[]>([]);
  const [result, setResult] = useState<BulkImportEmployeesResponse | null>(null);
  const [parseError, setParseError] = useState<string | null>(null);

  const validRows = rows.filter((_, i) => validations[i]?.errors.length === 0);
  const invalidCount = rows.length - validRows.length;

  const onDrop = useCallback(
    async (acceptedFiles: File[]) => {
      const file = acceptedFiles[0];
      if (!file) return;

      setParseError(null);
      try {
        const parsed = await parseExcel(file);
        if (parsed.length === 0) {
          setParseError(t('employees.import.noDataFound'));
          return;
        }
        const vals = validateRows(parsed, t);
        setRows(parsed);
        setValidations(vals);
        setStep('preview');
      } catch {
        setParseError(t('employees.import.parseError'));
      }
    },
    [t],
  );

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: {
      'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet': ['.xlsx'],
    },
    maxSize: 5 * 1024 * 1024,
    multiple: false,
  });

  async function handleImport() {
    if (!selectedEntityId) return;

    const employees = validRows.map(({ rowNumber, ...rest }) => rest);
    const res = await importMutation.mutateAsync({
      entityId: selectedEntityId,
      employees,
    });
    setResult(res);
    setStep('result');
  }

  function handleReset() {
    setStep('upload');
    setRows([]);
    setValidations([]);
    setResult(null);
    setParseError(null);
  }

  return (
    <div>
      <PageHeader
        title={t('employees.import.title')}
        actions={
          <Button variant="ghost" onClick={() => navigate('/hr/employees')}>
            <ArrowLeft className="mr-1 h-4 w-4" />
            {t('employees.import.back')}
          </Button>
        }
      />

      {step === 'upload' && (
        <div className="space-y-4">
          <Button variant="outline" onClick={() => generateTemplate(t)}>
            <Download className="mr-1 h-4 w-4" />
            {t('employees.import.downloadTemplate')}
          </Button>

          <div
            {...getRootProps()}
            className={`flex flex-col items-center justify-center rounded-lg border-2 border-dashed p-12 transition-colors cursor-pointer ${
              isDragActive
                ? 'border-primary bg-primary/5'
                : 'border-muted-foreground/25 hover:border-primary/50'
            }`}
          >
            <input {...getInputProps()} />
            <FileSpreadsheet className="mb-3 h-10 w-10 text-muted-foreground" />
            <p className="text-sm text-muted-foreground">
              {isDragActive
                ? t('employees.import.dropHere')
                : t('employees.import.dragOrClick')}
            </p>
            <p className="mt-1 text-xs text-muted-foreground">
              {t('employees.import.maxSize')}
            </p>
          </div>

          {parseError && (
            <div className="flex items-center gap-2 text-sm text-destructive">
              <AlertCircle className="h-4 w-4" />
              {parseError}
            </div>
          )}
        </div>
      )}

      {step === 'preview' && (
        <div className="space-y-4">
          {/* Summary bar */}
          <div className="flex items-center gap-4 rounded-lg bg-muted/50 px-4 py-3">
            <span className="text-sm font-medium">
              {t('employees.import.summaryTotal', { count: rows.length })}
            </span>
            <Badge className="bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-300">
              {t('employees.import.summaryValid', { count: validRows.length })}
            </Badge>
            {invalidCount > 0 && (
              <Badge className="bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300">
                {t('employees.import.summaryInvalid', { count: invalidCount })}
              </Badge>
            )}
          </div>

          {/* Preview table */}
          <div className="overflow-x-auto rounded-lg border">
            <table className="w-full text-sm">
              <thead className="bg-muted/50">
                <tr>
                  <th className="px-3 py-2 text-left font-medium">#</th>
                  <th className="px-3 py-2 text-left font-medium">
                    {t('employees.fields.employeeNumber')}
                  </th>
                  <th className="px-3 py-2 text-left font-medium">
                    {t('employees.fields.firstName')}
                  </th>
                  <th className="px-3 py-2 text-left font-medium">
                    {t('employees.fields.lastName')}
                  </th>
                  <th className="px-3 py-2 text-left font-medium">
                    {t('employees.fields.employeeType')}
                  </th>
                  <th className="px-3 py-2 text-left font-medium">
                    {t('employees.fields.hireDate')}
                  </th>
                  <th className="px-3 py-2 text-left font-medium">
                    {t('employees.import.contractFields.grossAmount')}
                  </th>
                  <th className="px-3 py-2 text-left font-medium">
                    {t('employees.import.contractFields.salaryType')}
                  </th>
                  <th className="px-3 py-2 text-left font-medium">
                    {t('employees.import.statusColumn')}
                  </th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row, i) => {
                  const v = validations[i];
                  const hasErrors = v && v.errors.length > 0;
                  const hasWarnings = v && v.warnings.length > 0;
                  return (
                    <tr
                      key={row.rowNumber}
                      className={hasErrors ? 'bg-red-50 dark:bg-red-950/20' : ''}
                    >
                      <td className="px-3 py-2 text-muted-foreground">{row.rowNumber}</td>
                      <td className="px-3 py-2">{row.employeeNumber}</td>
                      <td className="px-3 py-2">{row.firstName}</td>
                      <td className="px-3 py-2">{row.lastName}</td>
                      <td className="px-3 py-2">{row.employeeType}</td>
                      <td className="px-3 py-2">{row.hireDate}</td>
                      <td className="px-3 py-2">{row.grossAmount || '–'}</td>
                      <td className="px-3 py-2">{row.salaryType || '–'}</td>
                      <td className="px-3 py-2">
                        {hasErrors ? (
                          <TooltipProvider>
                            <Tooltip>
                              <TooltipTrigger>
                                <XCircle className="h-4 w-4 text-destructive" />
                              </TooltipTrigger>
                              <TooltipContent className="max-w-xs">
                                <ul className="list-disc pl-4 text-xs">
                                  {v.errors.map((err, j) => (
                                    <li key={j}>{err}</li>
                                  ))}
                                </ul>
                              </TooltipContent>
                            </Tooltip>
                          </TooltipProvider>
                        ) : hasWarnings ? (
                          <TooltipProvider>
                            <Tooltip>
                              <TooltipTrigger>
                                <AlertCircle className="h-4 w-4 text-amber-500" />
                              </TooltipTrigger>
                              <TooltipContent className="max-w-xs">
                                <ul className="list-disc pl-4 text-xs">
                                  {v.warnings.map((w, j) => (
                                    <li key={j}>{w}</li>
                                  ))}
                                </ul>
                              </TooltipContent>
                            </Tooltip>
                          </TooltipProvider>
                        ) : (
                          <CheckCircle2 className="h-4 w-4 text-emerald-600" />
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {/* Actions */}
          <div className="flex gap-2">
            <Button variant="outline" onClick={handleReset}>
              {t('employees.import.back')}
            </Button>
            <Button
              onClick={handleImport}
              disabled={validRows.length === 0 || importMutation.isPending}
            >
              <Upload className="mr-1 h-4 w-4" />
              {importMutation.isPending
                ? t('employees.import.importing')
                : t('employees.import.importButton', { count: validRows.length })}
            </Button>
          </div>
        </div>
      )}

      {step === 'result' && result && (
        <div className="space-y-4">
          {/* Result summary */}
          <div className="rounded-lg bg-muted/50 px-4 py-3">
            <p className="text-sm font-medium">
              {t('employees.import.resultSummary', {
                success: result.successCount,
                total: result.totalRows,
              })}
            </p>
          </div>

          {/* Result table */}
          <div className="overflow-x-auto rounded-lg border">
            <table className="w-full text-sm">
              <thead className="bg-muted/50">
                <tr>
                  <th className="px-3 py-2 text-left font-medium">#</th>
                  <th className="px-3 py-2 text-left font-medium">
                    {t('employees.fields.employeeNumber')}
                  </th>
                  <th className="px-3 py-2 text-left font-medium">
                    {t('employees.columns.name')}
                  </th>
                  <th className="px-3 py-2 text-left font-medium">
                    {t('employees.import.statusColumn')}
                  </th>
                  <th className="px-3 py-2 text-left font-medium">
                    {t('employees.import.errorColumn')}
                  </th>
                </tr>
              </thead>
              <tbody>
                {result.results.map((r) => {
                  const row = validRows[r.rowIndex];
                  return (
                    <tr
                      key={r.rowIndex}
                      className={r.success ? '' : 'bg-red-50 dark:bg-red-950/20'}
                    >
                      <td className="px-3 py-2 text-muted-foreground">
                        {row?.rowNumber ?? r.rowIndex + 1}
                      </td>
                      <td className="px-3 py-2">{row?.employeeNumber ?? '—'}</td>
                      <td className="px-3 py-2">
                        {row ? `${row.firstName} ${row.lastName}` : '—'}
                      </td>
                      <td className="px-3 py-2">
                        {r.success ? (
                          <CheckCircle2 className="h-4 w-4 text-emerald-600" />
                        ) : (
                          <XCircle className="h-4 w-4 text-destructive" />
                        )}
                      </td>
                      <td className="px-3 py-2 text-xs text-muted-foreground">
                        {r.error ?? ''}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {/* Actions */}
          <div className="flex gap-2">
            <Button variant="outline" onClick={handleReset}>
              {t('employees.import.importMore')}
            </Button>
            <Button onClick={() => navigate('/hr/employees')}>
              {t('employees.import.goToList')}
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
