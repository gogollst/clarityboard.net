export interface KpiDefinition {
  id: string;
  domain: 'financial' | 'sales' | 'marketing' | 'hr' | 'general';
  name: string;
  description: string;
  unit: 'percentage' | 'currency' | 'ratio' | 'count' | 'days';
  direction: 'higher_better' | 'lower_better' | 'target';
}

export interface KpiSnapshot {
  entityId: string;
  kpiId: string;
  snapshotDate: string;
  value: number;
  previousValue: number | null;
  changePct: number | null;
  targetValue: number | null;
  components: Record<string, number>;
  isProvisional: boolean;
  calculatedAt: string;
}

export interface KpiUpdateMessage {
  kpiId: string;
  entityId: string;
  value: number;
  previousValue: number | null;
  changePct: number | null;
  snapshotDate: string;
  updatedAt: string;
}

export interface DashboardDto {
  entityId: string;
  role: string;
  kpis: KpiSnapshot[];
  alerts: AlertDto[];
  lastUpdated: string;
}

export interface AlertDto {
  id: string;
  severity: 'critical' | 'warning' | 'info';
  title: string;
  message: string;
  kpiId: string | null;
  currentValue: number | null;
  thresholdValue: number | null;
  triggeredAt: string;
  status: 'active' | 'acknowledged' | 'resolved';
}
