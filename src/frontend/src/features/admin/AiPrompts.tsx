import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Search, Loader2, ChevronRight, CheckCircle2, XCircle, Bot } from 'lucide-react';
import { useAiPrompts } from '@/hooks/useAiManagement';
import type { AiProvider, AiPromptListItem } from '@/types/ai';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select';
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table';
import { Button } from '@/components/ui/button';

const MODULES = ['All', 'System', 'Document', 'CashFlow', 'KPI', 'Scenario', 'Budget', 'Accounting'];

const PROVIDER_BADGE_CLASS: Record<AiProvider, string> = {
  Anthropic: 'bg-amber-100 text-amber-800',
  OpenAI:    'bg-emerald-100 text-emerald-800',
  Grok:      'bg-blue-100 text-blue-800',
  Gemini:    'bg-purple-100 text-purple-800',
  ZAI:       'bg-secondary text-secondary-foreground',
  Manus:     'bg-rose-100 text-rose-800',
};

function ProviderBadge({ provider }: { provider: AiProvider }) {
  return (
    <Badge className={`text-xs font-medium ${PROVIDER_BADGE_CLASS[provider] ?? ''}`}>
      {provider}
    </Badge>
  );
}

function StatusIcon({ isActive }: { isActive: boolean }) {
  return isActive
    ? <CheckCircle2 className="h-4 w-4 text-emerald-600" />
    : <XCircle className="h-4 w-4 text-slate-400" />;
}

export function Component() {
  const navigate                = useNavigate();
  const [module, setModule]     = useState<string>('All');
  const [search, setSearch]     = useState('');

  const moduleParam = module === 'All' ? undefined : module;
  const { data: prompts = [], isLoading } = useAiPrompts(moduleParam);

  const filtered = search
    ? prompts.filter(p =>
        p.promptKey.toLowerCase().includes(search.toLowerCase()) ||
        p.name.toLowerCase().includes(search.toLowerCase()),
      )
    : prompts;

  return (
    <div className="space-y-6 p-6">
      <div>
        <h1 className="text-2xl font-semibold flex items-center gap-2">
          <Bot className="h-6 w-6" /> Prompt Management
        </h1>
        <p className="text-muted-foreground mt-1">
          All AI prompts are managed here. Every AI call in the system must reference a prompt by key.
        </p>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap gap-3">
        <div className="relative w-64">
          <Search className="text-muted-foreground absolute left-2.5 top-2.5 h-4 w-4" />
          <Input
            className="pl-8"
            placeholder="Search key or name…"
            value={search}
            onChange={e => setSearch(e.target.value)}
          />
        </div>
        <Select value={module} onValueChange={setModule}>
          <SelectTrigger className="w-40">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {MODULES.map(m => <SelectItem key={m} value={m}>{m}</SelectItem>)}
          </SelectContent>
        </Select>
      </div>

      {/* Table */}
      {isLoading ? (
        <div className="flex justify-center py-12"><Loader2 className="h-6 w-6 animate-spin" /></div>
      ) : (
        <div className="rounded-lg border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-8">Status</TableHead>
                <TableHead>Prompt Key</TableHead>
                <TableHead>Name</TableHead>
                <TableHead>Module</TableHead>
                <TableHead>Primary</TableHead>
                <TableHead>Fallback</TableHead>
                <TableHead>Version</TableHead>
                <TableHead>Updated</TableHead>
                <TableHead className="w-8" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {filtered.map((prompt: AiPromptListItem) => (
                <TableRow
                  key={prompt.id}
                  className="cursor-pointer hover:bg-secondary/60"
                  onClick={() => navigate(`/admin/ai/prompts/${prompt.promptKey}`)}
                >
                  <TableCell><StatusIcon isActive={prompt.isActive} /></TableCell>
                  <TableCell className="font-mono text-xs">{prompt.promptKey}</TableCell>
                  <TableCell className="font-medium">{prompt.name}</TableCell>
                  <TableCell>
                    <Badge variant="outline">{prompt.module}</Badge>
                  </TableCell>
                  <TableCell><ProviderBadge provider={prompt.primaryProvider} /></TableCell>
                  <TableCell><ProviderBadge provider={prompt.fallbackProvider} /></TableCell>
                  <TableCell className="text-muted-foreground text-xs">v{prompt.version}</TableCell>
                  <TableCell className="text-muted-foreground text-xs">
                    {new Date(prompt.updatedAt).toLocaleDateString('de-DE')}
                  </TableCell>
                  <TableCell>
                    <Button variant="ghost" size="icon" className="h-7 w-7">
                      <ChevronRight className="h-4 w-4" />
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
              {filtered.length === 0 && (
                <TableRow>
                  <TableCell colSpan={9} className="text-muted-foreground py-12 text-center">
                    No prompts found.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  );
}

