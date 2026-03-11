import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, Sparkles, Save, RotateCcw, Loader2, History, ChevronDown, ChevronUp } from 'lucide-react';
import {
  useAiPromptDetail, useUpdateAiPrompt, useEnhancePrompt, useRestorePromptVersion,
} from '@/hooks/useAiManagement';
import { AI_PROVIDERS } from '@/types/ai';
import type { AiPromptDetail, UpdateAiPromptRequest } from '@/types/ai';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Badge } from '@/components/ui/badge';
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select';
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from '@/components/ui/dialog';
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table';

type FormState = {
  systemPrompt: string;
  userPromptTemplate: string;
  primaryProvider: string;
  primaryModel: string;
  fallbackProvider: string;
  fallbackModel: string;
  temperature: number;
  maxTokens: number;
  changeSummary: string;
};

function toForm(p: AiPromptDetail): FormState {
  return {
    systemPrompt:       p.systemPrompt,
    userPromptTemplate: p.userPromptTemplate ?? '',
    primaryProvider:    p.primaryProvider,
    primaryModel:       p.primaryModel,
    fallbackProvider:   p.fallbackProvider,
    fallbackModel:      p.fallbackModel,
    temperature:        p.temperature,
    maxTokens:          p.maxTokens,
    changeSummary:      '',
  };
}

export function Component() {
  const { t, i18n } = useTranslation('ai');
  const [historyOpen, setHistoryOpen] = useState(false);
  const { promptKey } = useParams<{ promptKey: string }>();
  const navigate       = useNavigate();

  const { data: prompt, isLoading } = useAiPromptDetail(promptKey!);
  const update  = useUpdateAiPrompt();
  const enhance = useEnhancePrompt();
  const restore = useRestorePromptVersion();

  const [form, setForm]             = useState<FormState | null>(null);

  const [enhancePreview, setEnhancePreview] = useState<string | null>(null);
  const [showSaveDialog, setShowSaveDialog] = useState(false);

  useEffect(() => {
    if (prompt) setForm(toForm(prompt));
  }, [prompt]);

  if (isLoading || !form || !prompt)
    return <div className="flex justify-center p-8"><Loader2 className="h-6 w-6 animate-spin" /></div>;

  const set = (k: keyof FormState, v: string | number) =>
    setForm(prev => prev ? { ...prev, [k]: v } : prev);

  const handleEnhance = async () => {
    if (!form || !prompt) return;
    const result = await enhance.mutateAsync({
      promptKey: prompt.promptKey,
      request: {
        currentSystemPrompt: form.systemPrompt,
        userPromptTemplate:  form.userPromptTemplate || undefined,
        description:         prompt.description,
        functionDescription: prompt.functionDescription,
      },
    });
    setEnhancePreview(result);
  };

  const acceptEnhanced = () => {
    if (enhancePreview) { set('systemPrompt', enhancePreview); setEnhancePreview(null); }
  };

  const handleSave = () => {
    if (!form) return;
    const req: UpdateAiPromptRequest = {
      systemPrompt:       form.systemPrompt,
      userPromptTemplate: form.userPromptTemplate || undefined,
      primaryProvider:    form.primaryProvider as AiPromptDetail['primaryProvider'],
      primaryModel:       form.primaryModel,
      fallbackProvider:   form.fallbackProvider as AiPromptDetail['fallbackProvider'],
      fallbackModel:      form.fallbackModel,
      temperature:        form.temperature,
      maxTokens:          form.maxTokens,
      changeSummary:      form.changeSummary,
    };
    update.mutate({ promptKey: prompt.promptKey, request: req }, {
      onSuccess: () => { setShowSaveDialog(false); set('changeSummary', ''); },
    });
  };

  return (
    <div className="space-y-6 p-6 max-w-4xl">
      {/* Header */}
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="icon" onClick={() => navigate('/admin/ai/prompts')}>
          <ArrowLeft className="h-4 w-4" />
        </Button>
        <div className="flex-1">
          <div className="flex items-center gap-2">
            <h1 className="text-xl font-semibold">{prompt.name}</h1>
            <Badge variant="outline" className="font-mono text-xs">{prompt.promptKey}</Badge>
            <Badge variant={prompt.isActive ? 'default' : 'secondary'}>
              {prompt.isActive ? t('prompts.detail.active') : t('prompts.detail.inactive')}
            </Badge>
            {prompt.isSystemPrompt && <Badge variant="secondary">{t('prompts.detail.systemBadge')}</Badge>}
          </div>
          <p className="text-muted-foreground text-sm mt-0.5">{prompt.description}</p>
        </div>
        <div className="flex gap-2">
          <Button onClick={() => setShowSaveDialog(true)} disabled={update.isPending}>
            <Save className="mr-2 h-4 w-4" />{t('common:buttons.save')}
          </Button>
        </div>
      </div>

      {/* Provider config */}
      <div className="grid grid-cols-2 gap-4 rounded-lg border p-4">
        <div className="space-y-1.5">
          <Label>{t('prompts.detail.primaryProvider')}</Label>
          <Select value={form.primaryProvider} onValueChange={v => set('primaryProvider', v)}>
            <SelectTrigger><SelectValue /></SelectTrigger>
            <SelectContent>{AI_PROVIDERS.map(p => <SelectItem key={p} value={p}>{p}</SelectItem>)}</SelectContent>
          </Select>
        </div>
        <div className="space-y-1.5">
          <Label>{t('prompts.detail.primaryModel')}</Label>
          <Input value={form.primaryModel} onChange={e => set('primaryModel', e.target.value)} placeholder={t('prompts.detail.primaryModelPlaceholder')} />
        </div>
        <div className="space-y-1.5">
          <Label>{t('prompts.detail.fallbackProvider')}</Label>
          <Select value={form.fallbackProvider} onValueChange={v => set('fallbackProvider', v)}>
            <SelectTrigger><SelectValue /></SelectTrigger>
            <SelectContent>{AI_PROVIDERS.map(p => <SelectItem key={p} value={p}>{p}</SelectItem>)}</SelectContent>
          </Select>
        </div>
        <div className="space-y-1.5">
          <Label>{t('prompts.detail.fallbackModel')}</Label>
          <Input value={form.fallbackModel} onChange={e => set('fallbackModel', e.target.value)} placeholder={t('prompts.detail.fallbackModelPlaceholder')} />
        </div>
        <div className="space-y-1.5">
          <Label>{t('prompts.detail.temperature')} <span className="text-muted-foreground">({form.temperature})</span></Label>
          <input type="range" min={0} max={1} step={0.05}
            value={form.temperature}
            onChange={e => set('temperature', parseFloat(e.target.value))}
            className="w-full accent-primary" />
        </div>
        <div className="space-y-1.5">
          <Label>{t('prompts.detail.maxTokens')}</Label>
          <Input type="number" value={form.maxTokens} onChange={e => set('maxTokens', parseInt(e.target.value, 10))} min={1} max={100000} />
        </div>
      </div>

      {/* System Prompt */}
      <div className="space-y-1.5">
        <div className="flex items-center justify-between">
          <Label className="text-sm font-medium">{t('prompts.detail.systemPrompt')}</Label>
          <Button size="sm" variant="outline" disabled={enhance.isPending} onClick={handleEnhance}>
            {enhance.isPending
              ? <Loader2 className="mr-2 h-3 w-3 animate-spin" />
              : <Sparkles className="mr-2 h-3 w-3 text-amber-500" />}
            ✨ {t('prompts.detail.enhancePrompt')}
          </Button>
        </div>
        <Textarea className="font-mono text-xs min-h-[220px]" value={form.systemPrompt}
          onChange={e => set('systemPrompt', e.target.value)} />
      </div>

      {/* User Prompt Template */}
      <div className="space-y-1.5">
        <Label>{t('prompts.detail.userPromptTemplate')} <span className="text-muted-foreground text-xs">{t('prompts.detail.userPromptTemplateHint')}</span></Label>
        <Textarea className="font-mono text-xs min-h-[120px]" value={form.userPromptTemplate}
          onChange={e => set('userPromptTemplate', e.target.value)}
          placeholder={t('prompts.detail.userPromptTemplatePlaceholder')} />
      </div>

      {/* Version History */}
      <div className="rounded-lg border">
        <button
          className="flex w-full items-center gap-2 px-4 py-3 text-sm font-medium"
          onClick={() => setHistoryOpen(o => !o)}
        >
          <History className="h-4 w-4" />
          {t('prompts.detail.versionHistory')} ({prompt.versions.length})
          {historyOpen ? <ChevronUp className="ml-auto h-4 w-4" /> : <ChevronDown className="ml-auto h-4 w-4" />}
        </button>
        {historyOpen && (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('prompts.history.columns.version')}</TableHead>
                  <TableHead>{t('prompts.history.columns.provider')}</TableHead>
                  <TableHead>{t('prompts.history.columns.changeSummary')}</TableHead>
                  <TableHead>{t('prompts.history.columns.date')}</TableHead>
                  <TableHead />
                </TableRow>
              </TableHeader>
              <TableBody>
                {prompt.versions.map(v => (
                  <TableRow key={v.id}>
                    <TableCell className="font-mono text-xs">v{v.version}</TableCell>
                    <TableCell>
                      <div className="space-y-1 text-xs">
                        <div>{v.primaryProvider} · <span className="font-mono">{v.primaryModel}</span></div>
                        <div className="text-muted-foreground">→ {v.fallbackProvider} · <span className="font-mono">{v.fallbackModel}</span></div>
                        <div className="text-muted-foreground">T {v.temperature} · Max {v.maxTokens}</div>
                      </div>
                    </TableCell>
                    <TableCell className="text-muted-foreground max-w-xs truncate text-sm">{v.changeSummary}</TableCell>
                    <TableCell className="text-muted-foreground text-xs">{new Date(v.createdAt).toLocaleString(i18n.language)}</TableCell>
                    <TableCell>
                      <Button size="sm" variant="ghost" disabled={restore.isPending}
                        onClick={() => restore.mutate({ promptKey: prompt.promptKey, version: v.version })}>
                        <RotateCcw className="mr-1 h-3 w-3" />{t('common:buttons.restore')}
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
        )}
      </div>

      {/* Enhance Preview Dialog */}
      <Dialog open={!!enhancePreview} onOpenChange={open => !open && setEnhancePreview(null)}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Sparkles className="h-4 w-4 text-amber-500" />{t('prompts.detail.enhancePreview.title')}
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-3">
            <p className="text-muted-foreground text-sm">{t('prompts.detail.enhancePreview.description')}</p>
            <div className="grid gap-3 md:grid-cols-2">
              <div>
                <p className="text-muted-foreground mb-1 text-xs font-medium">{t('prompts.detail.enhancePreview.current')}</p>
                <pre className="max-h-64 overflow-auto whitespace-pre-wrap rounded border border-border bg-secondary p-3 text-xs">{form.systemPrompt}</pre>
              </div>
              <div>
                <p className="mb-1 text-xs font-medium text-amber-700">{t('prompts.detail.enhancePreview.enhanced')}</p>
                <pre className="max-h-64 overflow-auto whitespace-pre-wrap rounded border border-amber-200 bg-amber-50 p-3 text-xs">{enhancePreview}</pre>
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button variant="ghost" onClick={() => setEnhancePreview(null)}>{t('prompts.detail.enhancePreview.discard')}</Button>
            <Button onClick={acceptEnhanced}><Sparkles className="mr-2 h-3.5 w-3.5" />{t('prompts.detail.enhancePreview.apply')}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Save with ChangeSummary Dialog */}
      <Dialog open={showSaveDialog} onOpenChange={setShowSaveDialog}>
        <DialogContent>
          <DialogHeader><DialogTitle>{t('prompts.detail.saveDialog.title')}</DialogTitle></DialogHeader>
          <div className="space-y-2 py-2">
            <Label>{t('prompts.detail.saveDialog.changeSummaryLabel')}</Label>
            <Textarea placeholder={t('prompts.detail.saveDialog.changeSummaryPlaceholder')} value={form.changeSummary}
              onChange={e => set('changeSummary', e.target.value)} rows={3} />
            <p className="text-muted-foreground text-xs">{t('prompts.detail.saveDialog.changeSummaryHint')}</p>
          </div>
          <DialogFooter>
            <Button variant="ghost" onClick={() => setShowSaveDialog(false)}>{t('common:buttons.cancel')}</Button>
            <Button disabled={!form.changeSummary.trim() || update.isPending} onClick={handleSave}>
              {update.isPending ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
              {t('prompts.detail.saveDialog.saveVersion')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

