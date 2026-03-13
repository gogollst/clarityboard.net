import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { CheckCircle2, XCircle, RefreshCw, Key, Loader2 } from 'lucide-react';
import { useAiProviders, useUpsertAiProvider, useTestAiProvider } from '@/hooks/useAiManagement';
import { AI_PROVIDERS } from '@/types/ai';
import type { AiProvider, AiProviderConfig } from '@/types/ai';
import {
  Card, CardContent, CardDescription, CardHeader, CardTitle,
} from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter,
} from '@/components/ui/dialog';

const PROVIDER_COLORS: Record<string, string> = {
  Anthropic: 'bg-amber-50 border-amber-200',
  OpenAI:    'bg-emerald-50 border-emerald-200',
  Grok:      'bg-blue-50 border-blue-200',
  Gemini:    'bg-purple-50 border-purple-200',
  ZAI:       'bg-secondary border-border',
  Manus:     'bg-rose-50 border-rose-200',
  DeepL:     'bg-sky-50 border-sky-200',
  AzureDocIntelligence: 'bg-cyan-50 border-cyan-200',
};

function HealthBadge({ isHealthy, lastTestedAt }: Pick<AiProviderConfig, 'isHealthy' | 'lastTestedAt'>) {
  const { t } = useTranslation('ai');
  if (!lastTestedAt) return <Badge variant="secondary">{t('providers.notTested')}</Badge>;
  return isHealthy
    ? <Badge className="bg-emerald-100 text-emerald-800"><CheckCircle2 className="mr-1 h-3 w-3" />{t('providers.healthy')}</Badge>
    : <Badge variant="destructive"><XCircle className="mr-1 h-3 w-3" />{t('providers.unhealthy')}</Badge>;
}

export function Component() {
  const { t, i18n } = useTranslation('ai');
  const { data: providers = [], isLoading } = useAiProviders();
  const upsert = useUpsertAiProvider();
  const test   = useTestAiProvider();

  const [editProvider, setEditProvider] = useState<AiProvider | null>(null);
  const [apiKey, setApiKey]             = useState('');
  const [baseUrl, setBaseUrl]           = useState('');
  const [model, setModel]               = useState('');

  const configuredMap = new Map(providers.map(p => [p.provider, p]));

  const openEdit = (provider: AiProvider) => {
    const cfg = configuredMap.get(provider);
    setEditProvider(provider);
    setApiKey('');
    setBaseUrl(cfg?.baseUrl ?? '');
    setModel(cfg?.modelDefault ?? '');
  };

  const handleSave = () => {
    if (!editProvider || !apiKey.trim()) return;
    upsert.mutate(
      { provider: editProvider, request: { apiKey, baseUrl: baseUrl || undefined, modelDefault: model || undefined } },
      { onSuccess: () => setEditProvider(null) },
    );
  };

  if (isLoading) return <div className="flex justify-center p-8"><Loader2 className="h-6 w-6 animate-spin" /></div>;

  return (
    <div className="space-y-6 p-6">
      <div>
        <h1 className="text-2xl font-semibold">{t('providers.title')}</h1>
        <p className="text-muted-foreground mt-1">{t('providers.description')}</p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {AI_PROVIDERS.map(provider => {
          const cfg = configuredMap.get(provider);
          return (
            <Card key={provider} className={`border-2 ${PROVIDER_COLORS[provider] ?? ''}`}>
              <CardHeader className="pb-3">
                <div className="flex items-center justify-between">
                  <CardTitle className="text-base">{provider}</CardTitle>
                  <HealthBadge isHealthy={cfg?.isHealthy ?? false} lastTestedAt={cfg?.lastTestedAt ?? null} />
                </div>
                <CardDescription className="font-mono text-xs">
                  {cfg ? cfg.keyHint : t('providers.noKeyConfigured')}
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-2">
                {cfg?.lastTestedAt && (
                  <p className="text-muted-foreground text-xs">
                    {t('providers.lastTested', { date: new Date(cfg.lastTestedAt).toLocaleString(i18n.language) })}
                  </p>
                )}
                {cfg?.modelDefault && (
                  <p className="text-xs text-slate-600">{t('providers.defaultModel')} <code>{cfg.modelDefault}</code></p>
                )}
                <div className="flex gap-2 pt-1">
                  <Button size="sm" variant="outline" className="flex-1" onClick={() => openEdit(provider)}>
                    <Key className="mr-1 h-3 w-3" />{cfg ? t('providers.updateKey') : t('providers.addKey')}
                  </Button>
                  {cfg && (
                    <Button size="sm" variant="ghost" disabled={test.isPending} onClick={() => test.mutate(provider)}>
                      <RefreshCw className={`h-3 w-3 ${test.isPending ? 'animate-spin' : ''}`} />
                    </Button>
                  )}
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>

      <Dialog open={!!editProvider} onOpenChange={open => !open && setEditProvider(null)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t('providers.dialog.title', { provider: editProvider })}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-2">
            <div className="space-y-1.5">
              <Label>{t('providers.dialog.apiKeyLabel')}</Label>
              <Input type="password" placeholder={t('providers.dialog.apiKeyPlaceholder')} value={apiKey} onChange={e => setApiKey(e.target.value)} />
              <p className="text-muted-foreground text-xs">{t('providers.dialog.apiKeyHint')}</p>
            </div>
            <div className="space-y-1.5">
              <Label>{t('providers.dialog.baseUrlLabel')} <span className="text-muted-foreground">{t('providers.dialog.baseUrlOptional')}</span></Label>
              <Input placeholder={t('providers.dialog.baseUrlPlaceholder')} value={baseUrl} onChange={e => setBaseUrl(e.target.value)} />
            </div>
            <div className="space-y-1.5">
              <Label>{t('providers.dialog.modelLabel')} <span className="text-muted-foreground">{t('providers.dialog.modelOptional')}</span></Label>
              <Input placeholder={t('providers.dialog.modelPlaceholder')} value={model} onChange={e => setModel(e.target.value)} />
            </div>
          </div>
          <DialogFooter>
            <Button variant="ghost" onClick={() => setEditProvider(null)}>{t('common:buttons.cancel')}</Button>
            <Button disabled={!apiKey.trim() || upsert.isPending} onClick={handleSave}>
              {upsert.isPending ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
              {t('providers.dialog.saveAndTest')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

