import { useState } from 'react';
import { CheckCircle2, XCircle, AlertTriangle, Loader2, BarChart3 } from 'lucide-react';
import { useAiCallLogs, useAiCallLogStats } from '@/hooks/useAiManagement';
import { AI_PROVIDERS } from '@/types/ai';
import type { AiCallLogFilters, AiProvider } from '@/types/ai';
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';

function StatCard({ label, value, sub }: { label: string; value: string | number; sub?: string }) {
  return (
    <Card>
      <CardContent className="p-4">
        <p className="text-muted-foreground text-xs font-medium uppercase tracking-wide">{label}</p>
        <p className="text-2xl font-semibold mt-1">{value}</p>
        {sub && <p className="text-muted-foreground text-xs mt-0.5">{sub}</p>}
      </CardContent>
    </Card>
  );
}

export function Component() {
  const [filters, setFilters] = useState<AiCallLogFilters>({ page: 1, pageSize: 50 });

  const { data: logs, isLoading } = useAiCallLogs(filters);
  const { data: stats }           = useAiCallLogStats();

  const set = (k: keyof AiCallLogFilters, v: unknown) =>
    setFilters(prev => ({ ...prev, [k]: v, page: 1 }));

  const totalPages = logs ? Math.ceil(logs.totalCount / (filters.pageSize ?? 50)) : 1;

  return (
    <div className="space-y-6 p-6">
      <div className="flex items-center gap-2">
        <BarChart3 className="h-5 w-5" />
        <div>
          <h1 className="text-2xl font-semibold">AI Call Logs</h1>
          <p className="text-muted-foreground mt-0.5 text-sm">Audit trail of all AI calls made through the prompt execution engine.</p>
        </div>
      </div>

      {/* Stats */}
      {stats && (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4 xl:grid-cols-7">
          <StatCard label="Total Calls" value={stats.totalCalls.toLocaleString()} />
          <StatCard label="Success Rate" value={`${stats.successRate}%`} />
          <StatCard label="Avg. Duration" value={`${stats.avgDurationMs} ms`} />
          <StatCard label="Fallbacks" value={stats.fallbackCount} />
          <StatCard label="Input Tokens" value={stats.totalInputTokens.toLocaleString()} />
          <StatCard label="Output Tokens" value={stats.totalOutputTokens.toLocaleString()} />
          <StatCard label="Total Tokens" value={(stats.totalInputTokens + stats.totalOutputTokens).toLocaleString()} />
        </div>
      )}

      {/* Filters */}
      <div className="flex flex-wrap gap-2">
        <Input
          className="w-52"
          placeholder="Filter by prompt key…"
          value={filters.promptKey ?? ''}
          onChange={e => set('promptKey', e.target.value || undefined)}
        />
        <Select value={filters.provider ?? 'all'} onValueChange={v => set('provider', v === 'all' ? undefined : v as AiProvider)}>
          <SelectTrigger className="w-36"><SelectValue placeholder="All Providers" /></SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Providers</SelectItem>
            {AI_PROVIDERS.map(p => <SelectItem key={p} value={p}>{p}</SelectItem>)}
          </SelectContent>
        </Select>
        <Select
          value={filters.isSuccess === undefined ? 'all' : filters.isSuccess ? 'success' : 'error'}
          onValueChange={v => set('isSuccess', v === 'all' ? undefined : v === 'success')}
        >
          <SelectTrigger className="w-32"><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All</SelectItem>
            <SelectItem value="success">Success</SelectItem>
            <SelectItem value="error">Error</SelectItem>
          </SelectContent>
        </Select>
        <Input type="date" className="w-40" value={filters.from ?? ''}
          onChange={e => set('from', e.target.value || undefined)} />
        <Input type="date" className="w-40" value={filters.to ?? ''}
          onChange={e => set('to', e.target.value || undefined)} />
      </div>

      {/* Table */}
      {isLoading ? (
        <div className="flex justify-center py-12"><Loader2 className="h-6 w-6 animate-spin" /></div>
      ) : (
        <>
          <div className="rounded-lg border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-8" />
                  <TableHead>Prompt Key</TableHead>
                  <TableHead>Provider</TableHead>
                  <TableHead>Fallback</TableHead>
                  <TableHead>Input Tok.</TableHead>
                  <TableHead>Output Tok.</TableHead>
                  <TableHead>Duration</TableHead>
                  <TableHead>Date</TableHead>
                  <TableHead>Error</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {(logs?.items ?? []).map(log => (
                  <TableRow key={log.id} className={log.isSuccess ? '' : 'bg-red-50/40'}>
                    <TableCell>
                      {log.isSuccess
                        ? <CheckCircle2 className="h-4 w-4 text-emerald-600" />
                        : <XCircle className="h-4 w-4 text-red-500" />}
                    </TableCell>
                    <TableCell className="font-mono text-xs">{log.promptKey}</TableCell>
                    <TableCell>
                      <Badge variant="outline" className="text-xs">{log.usedProvider}</Badge>
                    </TableCell>
                    <TableCell>
                      {log.usedFallback && <AlertTriangle className="h-4 w-4 text-amber-500" />}
                    </TableCell>
                    <TableCell className="text-muted-foreground text-xs">{log.inputTokens.toLocaleString()}</TableCell>
                    <TableCell className="text-muted-foreground text-xs">{log.outputTokens.toLocaleString()}</TableCell>
                    <TableCell className="text-muted-foreground text-xs">{log.durationMs} ms</TableCell>
                    <TableCell className="text-muted-foreground text-xs">
                      {new Date(log.createdAt).toLocaleString('de-DE')}
                    </TableCell>
                    <TableCell className="max-w-xs truncate text-xs text-red-600">{log.errorMessage}</TableCell>
                  </TableRow>
                ))}
                {(logs?.items ?? []).length === 0 && (
                  <TableRow>
                    <TableCell colSpan={9} className="text-muted-foreground py-12 text-center">No logs found.</TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </div>

          {/* Pagination */}
          <div className="flex items-center justify-between">
            <p className="text-muted-foreground text-sm">
              {logs?.totalCount ?? 0} entries
            </p>
            <div className="flex gap-2">
              <Button size="sm" variant="outline" disabled={(filters.page ?? 1) <= 1}
                onClick={() => set('page', (filters.page ?? 1) - 1)}>Previous</Button>
              <span className="text-muted-foreground flex items-center px-2 text-sm">
                Page {filters.page ?? 1} / {totalPages}
              </span>
              <Button size="sm" variant="outline" disabled={(filters.page ?? 1) >= totalPages}
                onClick={() => set('page', (filters.page ?? 1) + 1)}>Next</Button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

