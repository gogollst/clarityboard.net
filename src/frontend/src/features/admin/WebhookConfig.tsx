import { useState } from 'react';
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
      },
      {
        onSuccess: () => {
          setIsAddSourceOpen(false);
          setSourceForm({ name: '', sourceType: '' });
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
        title="Webhook Configuration"
        actions={
          <Button onClick={() => setIsAddSourceOpen(true)}>
            <Plus className="mr-1 h-4 w-4" />
            Add Source
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
            No webhook sources configured.
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
                          {config.isActive ? 'Active' : 'Inactive'}
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
                      <h4 className="text-sm font-medium">Mapping Rules</h4>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => openAddRule(config.id)}
                      >
                        <Plus className="mr-1 h-3 w-3" />
                        Add Rule
                      </Button>
                    </div>

                    {config.mappingRules.length === 0 ? (
                      <p className="text-sm text-muted-foreground">
                        No mapping rules configured.
                      </p>
                    ) : (
                      <Table>
                        <TableHeader>
                          <TableRow>
                            <TableHead>Name</TableHead>
                            <TableHead>Event Type</TableHead>
                            <TableHead>Debit Field</TableHead>
                            <TableHead>Credit Field</TableHead>
                            <TableHead>Amount Field</TableHead>
                            <TableHead>Active</TableHead>
                            <TableHead>Actions</TableHead>
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
          <CardTitle>Dead Letter Queue</CardTitle>
        </CardHeader>
        <CardContent>
          {dlLoading ? (
            <Skeleton className="h-40 w-full" />
          ) : deadLetterItems.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              No dead letter events.
            </p>
          ) : (
            <>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Source</TableHead>
                    <TableHead>Event Type</TableHead>
                    <TableHead>Error</TableHead>
                    <TableHead>Created</TableHead>
                    <TableHead>Retries</TableHead>
                    <TableHead>Actions</TableHead>
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
                        {new Date(event.createdAt).toLocaleDateString('de-DE')}
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
                          Retry
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
                    Previous
                  </Button>
                  <span className="text-sm text-muted-foreground">
                    Page {dlPage}
                  </span>
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={dlPage * dlPageSize >= deadLetterTotal}
                    onClick={() => setDlPage((p) => p + 1)}
                  >
                    Next
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
            <DialogTitle>Add Webhook Source</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label>Source Name</Label>
              <Input
                value={sourceForm.name}
                onChange={(e) =>
                  setSourceForm((f) => ({ ...f, name: e.target.value }))
                }
                placeholder="e.g. Stripe Payments"
              />
            </div>
            <div>
              <Label>Source Type</Label>
              <Input
                value={sourceForm.sourceType}
                onChange={(e) =>
                  setSourceForm((f) => ({
                    ...f,
                    sourceType: e.target.value,
                  }))
                }
                placeholder="e.g. stripe, shopify, custom"
              />
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsAddSourceOpen(false)}
            >
              Cancel
            </Button>
            <Button
              onClick={handleCreateSource}
              disabled={createConfig.isPending}
            >
              {createConfig.isPending && (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              )}
              Create
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Add Mapping Rule Dialog */}
      <Dialog open={isAddRuleOpen} onOpenChange={setIsAddRuleOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Add Mapping Rule</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label>Rule Name</Label>
              <Input
                value={ruleForm.name}
                onChange={(e) =>
                  setRuleForm((f) => ({ ...f, name: e.target.value }))
                }
                placeholder="e.g. Payment Received"
              />
            </div>
            <div>
              <Label>Event Type</Label>
              <Input
                value={ruleForm.eventType}
                onChange={(e) =>
                  setRuleForm((f) => ({ ...f, eventType: e.target.value }))
                }
                placeholder="e.g. payment_intent.succeeded"
              />
            </div>
            <div>
              <Label>Debit Account Field</Label>
              <Input
                value={ruleForm.debitAccountField}
                onChange={(e) =>
                  setRuleForm((f) => ({
                    ...f,
                    debitAccountField: e.target.value,
                  }))
                }
                placeholder="e.g. data.metadata.debit_account"
              />
            </div>
            <div>
              <Label>Credit Account Field</Label>
              <Input
                value={ruleForm.creditAccountField}
                onChange={(e) =>
                  setRuleForm((f) => ({
                    ...f,
                    creditAccountField: e.target.value,
                  }))
                }
                placeholder="e.g. data.metadata.credit_account"
              />
            </div>
            <div>
              <Label>Amount Field</Label>
              <Input
                value={ruleForm.amountField}
                onChange={(e) =>
                  setRuleForm((f) => ({
                    ...f,
                    amountField: e.target.value,
                  }))
                }
                placeholder="e.g. data.amount"
              />
            </div>
            <div>
              <Label>Description Field</Label>
              <Input
                value={ruleForm.descriptionField}
                onChange={(e) =>
                  setRuleForm((f) => ({
                    ...f,
                    descriptionField: e.target.value,
                  }))
                }
                placeholder="e.g. data.description"
              />
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsAddRuleOpen(false)}
            >
              Cancel
            </Button>
            <Button
              onClick={handleCreateRule}
              disabled={createRule.isPending}
            >
              {createRule.isPending && (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              )}
              Create Rule
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Edit Mapping Rule Dialog */}
      <Dialog open={isEditRuleOpen} onOpenChange={setIsEditRuleOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Edit Mapping Rule</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label>Rule Name</Label>
              <Input
                value={ruleForm.name}
                onChange={(e) =>
                  setRuleForm((f) => ({ ...f, name: e.target.value }))
                }
              />
            </div>
            <div>
              <Label>Event Type</Label>
              <Input
                value={ruleForm.eventType}
                onChange={(e) =>
                  setRuleForm((f) => ({ ...f, eventType: e.target.value }))
                }
              />
            </div>
            <div>
              <Label>Debit Account Field</Label>
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
              <Label>Credit Account Field</Label>
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
              <Label>Amount Field</Label>
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
              <Label>Description Field</Label>
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
              Cancel
            </Button>
            <Button
              onClick={handleUpdateRule}
              disabled={updateRule.isPending}
            >
              {updateRule.isPending && (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              )}
              Save Changes
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
