export interface WebhookConfig {
  id: string;
  entityId: string;
  name: string;
  sourceType: string;
  secretKey: string;
  isActive: boolean;
  mappingRules: MappingRule[];
}

export interface MappingRule {
  id: string;
  name: string;
  eventType: string;
  debitAccountField: string;
  creditAccountField: string;
  amountField: string;
  descriptionField: string;
  isActive: boolean;
}

export interface CreateWebhookConfigRequest {
  entityId: string;
  name: string;
  sourceType: string;
  secret?: string;
  headerSignatureKey?: string;
  eventFilter?: string;
}

export interface UpdateWebhookConfigRequest {
  id: string;
  name?: string;
  isActive?: boolean;
}

export interface CreateMappingRuleRequest {
  webhookConfigId: string;
  name: string;
  eventType: string;
  debitAccountField: string;
  creditAccountField: string;
  amountField: string;
  descriptionField: string;
}

export interface UpdateMappingRuleRequest {
  id: string;
  name?: string;
  eventType?: string;
  debitAccountField?: string;
  creditAccountField?: string;
  amountField?: string;
  descriptionField?: string;
  isActive?: boolean;
}

export interface WebhookDeadLetterEvent {
  id: string;
  sourceType: string;
  eventType: string;
  error: string;
  createdAt: string;
  retryCount: number;
}
