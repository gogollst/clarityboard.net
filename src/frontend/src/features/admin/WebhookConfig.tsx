import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useEntity } from '@/hooks/useEntity';
import {
  useWebhookConfigs,
  useCreateWebhookConfig,
  useUpdateWebhookConfig,
  useCreateMappingRule,
  useUpdateMappingRule,
  useDeleteMappingRule,
  useDeadLetterQueue,
  useRetryDeadLetter,
} from '@/hooks/useWebhooks';
import type {
  WebhookConfig as WebhookConfigType,
  MappingRule,
} from '@/types/webhook';
import PageHeader from '@/components/shared/PageHeader';
import StatusBadge from '@/components/shared/StatusBadge';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Switch } from '@/components/ui/switch';
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
import { Separator } from '@/components/ui/separator';
import { Skeleton } from '@/components/ui/skeleton';
import { Plus, Loader2, Trash2, Edit, RefreshCw, ChevronRight, ChevronDown } from 'lucide-react';

export function Component() {
  const { t, i18n } = useTranslation('admin');
  const { selectedEntityId } = useEntity();
  const { data: configs, isLoading: configsLoading } =
    useWebhookConfigs(selectedEntityId);
  const createConfig = useCreateWebhookConfig();
  const updateConfig = useUpdateWebhookConfig();
  const createRule = useCreateMappingRule();
  const updateRule = useUpdateMappingRule();
  const deleteRule = useDeleteMappingRule();

  // Dead letter queue
  const [dlPage, setDlPage] = useState(1);
  const dlPageSize = 20;
  const { data: deadLetterData, isLoading: dlLoading } = useDeadLetterQueue(
    selectedEntityId,
    { page: dlPage, pageSize: dlPageSize },
  );
  const retryDeadLetter = useRetryDeadLetter();

  // UI state
  const [expandedConfig, setExpandedConfig] = useState<string | null>(null);
  const [isAddSourceOpen, setIsAddSourceOpen] = useState(false);
  const [isAddRuleOpen, setIsAddRuleOpen] = useState(false);
  const [isEditRuleOpen, setIsEditRuleOpen] = useState(false);
  const [editingRule, setEditingRule] = useState<MappingRule | null>(null);
  const [activeConfigId, setActiveConfigId] = useState<string | null>(null);

  const [sourceForm, setSourceForm] = useState({
    name: '',
    sourceType: '',
    secret: '',
    headerSignatureKey: 'X-Webhook-Signature',
    eventFilter: '',
  });

  const [ruleForm, setRuleForm] = useState({
    name: '',
    eventType: '',
    debitAccountField: '',
    creditAccountField: '',
    amountField: '',
    descriptionField: '',
  });

  const deadLetterItems = Array.isArray(deadLetterData)
    ? deadLetterData
    : deadLetterData?.items ?? [];
  const deadLetterTotal =
    (deadLetterData as { totalCount?: number } | undefined)?.totalCount ?? 0;

  const handleCreateSource = () => {
    if (!selectedEntityId) return;
    createConfig.mutate(
      {
        entityId: selectedEntityId,
        name: sourceForm.name,
        sourceType: sourceForm.sourceType,
        secret: sourceForm.secret || undefined,
        headerSignatureKey: sourceForm.headerSignatureKey || undefined,
        eventFilter: sourceForm.eventFilter || undefined,
      },
      {
        onSuccess: () => {
          setIsAddSourceOpen(false);
          setSourceForm({ name: '', sourceType: '', secret: '', headerSignatureKey: 'X-Webhook-Signature', eventFilter: '' });
        },
      },
    );
  };

  const handleToggleActive = (config: WebhookConfigType) => {
    if (!selectedEntityId) return;
    updateConfig.mutate({
      request: { id: config.id, isActive: !config.isActive },
      entityId: selectedEntityId,
    });
  };

  const handleCreateRule = () => {
    if (!selectedEntityId || !activeConfigId) return;
    createRule.mutate(
      {
        request: {
          webhookConfigId: activeConfigId,
          ...ruleForm,
        },
        entityId: selectedEntityId,
      },
      {
        onSuccess: () => {
          setIsAddRuleOpen(false);
          setRuleForm({
            name: '',
            eventType: '',
            debitAccountField: '',
            creditAccountField: '',
            amountField: '',
            descriptionField: '',
          });
        },
      },
    );
  };

  const handleUpdateRule = () => {
    if (!selectedEntityId || !editingRule) return;
    updateRule.mutate(
      {
        request: {
          id: editingRule.id,
          ...ruleForm,
        },
        entityId: selectedEntityId,
      },
      {
        onSuccess: () => {
          setIsEditRuleOpen(false);
          setEditingRule(null);
        },
      },
    );
  };

  const handleDeleteRule = (ruleId: string) => {
    if (!selectedEntityId) return;
    deleteRule.mutate({ ruleId, entityId: selectedEntityId });
  };

  const handleRetry = (eventId: string) => {
    if (!selectedEntityId) return;
    retryDeadLetter.mutate({ eventId, entityId: selectedEntityId });
  };

  const openAddRule = (configId: string) => {
    setActiveConfigId(configId);
    setRuleForm({
      name: '',
      eventType: '',
      debitAccountField: '',
      creditAccountField: '',
      amountField: '',
      descriptionField: '',
    });
    setIsAddRuleOpen(true);
  };

  const openEditRule = (rule: MappingRule) => {
    setEditingRule(rule);
    setRuleForm({
      name: rule.name,
      eventType: rule.eventType,
      debitAccountField: rule.debitAccountField,
      creditAccountField: rule.creditAccountField,
      amountField: rule.amountField,
      descriptionField: rule.descriptionField,
    });
    setIsEditRuleOpen(true);
  };

  return (
    <div>
      <PageHeader
        title={t('webhooks.title')}
        actions={
          <Button onClick={() => setIsAddSourceOpen(true)}>
            <Plus className="mr-1 h-4 w-4" />
            {t('webhooks.addSource')}
          </Button>
        }
      />

      {/* Webhook Configs */}
      {configsLoading ? (
        <div className="space-y-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="h-20 w-full" />
          ))}
        </div>
      ) : !configs || configs.length === 0 ? (
        <Card>
          <CardContent className="py-12 text-center text-muted-foreground">
            {t('webhooks.noSources')}
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-4">
          {configs.map((config) => {
            const isExpanded = expandedConfig === config.id;
            return (
              <Card key={config.id}>
                <CardHeader
                  className="cursor-pointer"
                  onClick={() =>
                    setExpandedConfig(isExpanded ? null : config.id)
                  }
                >
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      {isExpanded ? (
                        <ChevronDown className="h-4 w-4 text-muted-foreground" />
                      ) : (
                        <ChevronRight className="h-4 w-4 text-muted-foreground" />
                      )}
                      <CardTitle className="text-base">
                        {config.name}
                      </CardTitle>
                      <Badge variant="outline">{config.sourceType}</Badge>
                    </div>
                    <div
                      className="flex items-center gap-3"
                      onClick={(e) => e.stopPropagation()}
                    >
                      <div className="flex items-center gap-2">
                        <span className="text-xs text-muted-foreground">
                          {config.isActive ? t('common:status.active', { ns: 'common' }) : t('common:status.inactive', { ns: 'common' })}
                        </span>
                        <Switch
                          checked={config.isActive}
                          onCheckedChange={() => handleToggleActive(config)}
                        />
                      </div>
                    </div>
                  </div>
                </CardHeader>

                {isExpanded && (
                  <CardContent>
                    <div className="flex items-center justify-between mb-3">
                      <h4 className="text-sm font-medium">{t('webhooks.mappingRules')}</h4>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => openAddRule(config.id)}
                      >
                        <Plus className="mr-1 h-3 w-3" />
                        {t('webhooks.addRule')}
                      </Button>
                    </div>

                    {config.mappingRules.length === 0 ? (
                      <p className="text-sm text-muted-foreground">
                        {t('webhooks.noRules')}
                      </p>
                    ) : (
                      <Table>
                        <TableHeader>
                          <TableRow>
                            <TableHead>{t('webhooks.columns.name')}</TableHead>
                            <TableHead>{t('webhooks.columns.eventType')}</TableHead>
                            <TableHead>{t('webhooks.columns.debitField')}</TableHead>
                            <TableHead>{t('webhooks.columns.creditField')}</TableHead>
                            <TableHead>{t('webhooks.columns.amountField')}</TableHead>
                            <TableHead>{t('webhooks.columns.active')}</TableHead>
                            <TableHead>{t('webhooks.columns.actions')}</TableHead>
                          </TableRow>
                        </TableHeader>
                        <TableBody>
                          {config.mappingRules.map((rule) => (
                            <TableRow key={rule.id}>
                              <TableCell className="font-medium">
                                {rule.name}
                              </TableCell>
                              <TableCell>{rule.eventType}</TableCell>
                              <TableCell className="font-mono text-xs">
                                {rule.debitAccountField}
                              </TableCell>
                              <TableCell className="font-mono text-xs">
                                {rule.creditAccountField}
                              </TableCell>
                              <TableCell className="font-mono text-xs">
                                {rule.amountField}
                              </TableCell>
                              <TableCell>
                                <StatusBadge
                                  status={rule.isActive ? 'Active' : 'Inactive'}
                                  variantMap={{
                                    active: 'success',
                                    inactive: 'default',
                                  }}
                                />
                              </TableCell>
                              <TableCell>
                                <div className="flex items-center gap-1">
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    onClick={() => openEditRule(rule)}
                                  >
                                    <Edit className="h-3 w-3" />
                                  </Button>
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    onClick={() => handleDeleteRule(rule.id)}
                                  >
                                    <Trash2 className="h-3 w-3 text-red-500" />
                                  </Button>
                                </div>
                              </TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    )}
                  </CardContent>
                )}
              </Card>
            );
          })}
        </div>
      )}

      {/* Dead Letter Queue */}
      <Separator className="my-8" />
      <Card>
        <CardHeader>
          <CardTitle>{t('webhooks.deadLetterQueue.title')}</CardTitle>
        </CardHeader>
        <CardContent>
          {dlLoading ? (
            <Skeleton className="h-40 w-full" />
          ) : deadLetterItems.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              {t('webhooks.deadLetterQueue.noEvents')}
            </p>
          ) : (
            <>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t('webhooks.deadLetterQueue.columns.source')}</TableHead>
                    <TableHead>{t('webhooks.deadLetterQueue.columns.eventType')}</TableHead>
                    <TableHead>{t('webhooks.deadLetterQueue.columns.error')}</TableHead>
                    <TableHead>{t('webhooks.deadLetterQueue.columns.created')}</TableHead>
                    <TableHead>{t('webhooks.deadLetterQueue.columns.retries')}</TableHead>
                    <TableHead>{t('webhooks.deadLetterQueue.columns.actions')}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {deadLetterItems.map((event) => (
                    <TableRow key={event.id}>
                      <TableCell>{event.sourceType}</TableCell>
                      <TableCell>{event.eventType}</TableCell>
                      <TableCell className="max-w-[200px] truncate text-xs text-red-600">
                        {event.error}
                      </TableCell>
                      <TableCell>
                        {new Date(event.createdAt).toLocaleDateString(i18n.language)}
                      </TableCell>
                      <TableCell>{event.retryCount}</TableCell>
                      <TableCell>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleRetry(event.id)}
                          disabled={retryDeadLetter.isPending}
                        >
                          <RefreshCw className="mr-1 h-3 w-3" />
                          {t('webhooks.deadLetterQueue.retry')}
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>

              {/* Pagination for dead letter */}
              {deadLetterTotal > dlPageSize && (
                <div className="mt-4 flex items-center justify-end gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={dlPage <= 1}
                    onClick={() => setDlPage((p) => p - 1)}
                  >
                    {t('webhooks.pagination.previous')}
                  </Button>
                  <span className="text-sm text-muted-foreground">
                    {t('webhooks.pagination.page', { page: dlPage })}
                  </span>
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={dlPage * dlPageSize >= deadLetterTotal}
                    onClick={() => setDlPage((p) => p + 1)}
                  >
                    {t('webhooks.pagination.next')}
                  </Button>
                </div>
              )}
            </>
          )}
        </CardContent>
      </Card>

      {/* Add Source Dialog */}
      <Dialog open={isAddSourceOpen} onOpenChange={setIsAddSourceOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('webhooks.dialogs.addSource.title')}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label>{t('webhooks.dialogs.addSource.sourceName')}</Label>
              <Input
                value={sourceForm.name}
                onChange={(e) =>
                  setSourceForm((f) => ({ ...f, name: e.target.value }))
                }
                placeholder={t('webhooks.dialogs.addSource.sourceNamePlaceholder')}
              />
            </div>
            <div>
              <Label>{t('webhooks.dialogs.addSource.sourceType')}</Label>
              <Input
                value={sourceForm.sourceType}
                onChange={(e) =>
                  setSourceForm((f) => ({
                    ...f,
                    sourceType: e.target.value,
                  }))
                }
                placeholder={t('webhooks.dialogs.addSource.sourceTypePlaceholder')}
              />
            </div>
            <div>
              <Label>{t('webhooks.dialogs.addSource.secret')}</Label>
              <Input
                type="password"
                value={sourceForm.secret}
                onChange={(e) =>
                  setSourceForm((f) => ({ ...f, secret: e.target.value }))
                }
                placeholder={t('webhooks.dialogs.addSource.secretPlaceholder')}
              />
            </div>
            <div>
              <Label>{t('webhooks.dialogs.addSource.headerSignatureKey')}</Label>
              <Input
                value={sourceForm.headerSignatureKey}
                onChange={(e) =>
                  setSourceForm((f) => ({ ...f, headerSignatureKey: e.target.value }))
                }
                placeholder="X-Webhook-Signature"
              />
            </div>
            <div>
              <Label>{t('webhooks.dialogs.addSource.eventFilter')}</Label>
              <Input
                value={sourceForm.eventFilter}
                onChange={(e) =>
                  setSourceForm((f) => ({ ...f, eventFilter: e.target.value }))
                }
                placeholder={t('webhooks.dialogs.addSource.eventFilterPlaceholder')}
              />
            </div>
            {sourceForm.sourceType && (
              <div className="rounded-md bg-muted p-3">
                <Label className="text-xs text-muted-foreground">{t('webhooks.dialogs.addSource.endpointUrl')}</Label>
                <code className="mt-1 block text-xs break-all">
                  POST /api/webhooks/{sourceForm.sourceType}/events
                </code>
              </div>
            )}
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsAddSourceOpen(false)}
            >
              {t('common:buttons.cancel', { ns: 'common' })}
            </Button>
            <Button
              onClick={handleCreateSource}
              disabled={createConfig.isPending}
            >
              {createConfig.isPending && (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              )}
              {t('webhooks.dialogs.addSource.create')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Add Mapping Rule Dialog */}
      <Dialog open={isAddRuleOpen} onOpenChange={setIsAddRuleOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('webhooks.dialogs.addRule.title')}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label>{t('webhooks.dialogs.ruleForm.ruleName')}</Label>
              <Input
                value={ruleForm.name}
                onChange={(e) =>
                  setRuleForm((f) => ({ ...f, name: e.target.value }))
                }
                placeholder={t('webhooks.dialogs.ruleForm.ruleNamePlaceholder')}
              />
            </div>
            <div>
              <Label>{t('webhooks.dialogs.ruleForm.eventType')}</Label>
              <Input
                value={ruleForm.eventType}
                onChange={(e) =>
                  setRuleForm((f) => ({ ...f, eventType: e.target.value }))
                }
                placeholder={t('webhooks.dialogs.ruleForm.eventTypePlaceholder')}
              />
            </div>
            <div>
              <Label>{t('webhooks.dialogs.ruleForm.debitAccountField')}</Label>
              <Input
                value={ruleForm.debitAccountField}
                onChange={(e) =>
                  setRuleForm((f) => ({
                    ...f,
                    debitAccountField: e.target.value,
                  }))
                }
                placeholder={t('webhooks.dialogs.ruleForm.debitAccountFieldPlaceholder')}
              />
            </div>
            <div>
              <Label>{t('webhooks.dialogs.ruleForm.creditAccountField')}</Label>
              <Input
                value={ruleForm.creditAccountField}
                onChange={(e) =>
                  setRuleForm((f) => ({
                    ...f,
                    creditAccountField: e.target.value,
                  }))
                }
                placeholder={t('webhooks.dialogs.ruleForm.creditAccountFieldPlaceholder')}
              />
            </div>
            <div>
              <Label>{t('webhooks.dialogs.ruleForm.amountField')}</Label>
              <Input
                value={ruleForm.amountField}
                onChange={(e) =>
                  setRuleForm((f) => ({
                    ...f,
                    amountField: e.target.value,
                  }))
                }
                placeholder={t('webhooks.dialogs.ruleForm.amountFieldPlaceholder')}
              />
            </div>
            <div>
              <Label>{t('webhooks.dialogs.ruleForm.descriptionField')}</Label>
              <Input
                value={ruleForm.descriptionField}
                onChange={(e) =>
                  setRuleForm((f) => ({
                    ...f,
                    descriptionField: e.target.value,
                  }))
                }
                placeholder={t('webhooks.dialogs.ruleForm.descriptionFieldPlaceholder')}
              />
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsAddRuleOpen(false)}
            >
              {t('common:buttons.cancel', { ns: 'common' })}
            </Button>
            <Button
              onClick={handleCreateRule}
              disabled={createRule.isPending}
            >
              {createRule.isPending && (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              )}
              {t('webhooks.dialogs.addRule.createButton')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Edit Mapping Rule Dialog */}
      <Dialog open={isEditRuleOpen} onOpenChange={setIsEditRuleOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('webhooks.dialogs.editRule.title')}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label>{t('webhooks.dialogs.ruleForm.ruleName')}</Label>
              <Input
                value={ruleForm.name}
                onChange={(e) =>
                  setRuleForm((f) => ({ ...f, name: e.target.value }))
                }
              />
            </div>
            <div>
              <Label>{t('webhooks.dialogs.ruleForm.eventType')}</Label>
              <Input
                value={ruleForm.eventType}
                onChange={(e) =>
                  setRuleForm((f) => ({ ...f, eventType: e.target.value }))
                }
              />
            </div>
            <div>
              <Label>{t('webhooks.dialogs.ruleForm.debitAccountField')}</Label>
              <Input
                value={ruleForm.debitAccountField}
                onChange={(e) =>
                  setRuleForm((f) => ({
                    ...f,
                    debitAccountField: e.target.value,
                  }))
                }
              />
            </div>
            <div>
              <Label>{t('webhooks.dialogs.ruleForm.creditAccountField')}</Label>
              <Input
                value={ruleForm.creditAccountField}
                onChange={(e) =>
                  setRuleForm((f) => ({
                    ...f,
                    creditAccountField: e.target.value,
                  }))
                }
              />
            </div>
            <div>
              <Label>{t('webhooks.dialogs.ruleForm.amountField')}</Label>
              <Input
                value={ruleForm.amountField}
                onChange={(e) =>
                  setRuleForm((f) => ({
                    ...f,
                    amountField: e.target.value,
                  }))
                }
              />
            </div>
            <div>
              <Label>{t('webhooks.dialogs.ruleForm.descriptionField')}</Label>
              <Input
                value={ruleForm.descriptionField}
                onChange={(e) =>
                  setRuleForm((f) => ({
                    ...f,
                    descriptionField: e.target.value,
                  }))
                }
              />
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsEditRuleOpen(false)}
            >
              {t('common:buttons.cancel', { ns: 'common' })}
            </Button>
            <Button
              onClick={handleUpdateRule}
              disabled={updateRule.isPending}
            >
              {updateRule.isPending && (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              )}
              {t('webhooks.dialogs.editRule.saveButton')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
