// Static configuration for the executive dashboard.
// Maps domains to their primary KPI IDs and navigation routes.

export const HEADLINE_KPIS = [
  { kpiId: 'financial.revenue', labelKey: 'executive:headlines.revenue', route: '/kpis/financial' },
  { kpiId: 'financial.ebitda', labelKey: 'executive:headlines.ebitda', route: '/kpis/financial' },
  { kpiId: 'financial.free_cash_flow', labelKey: 'executive:headlines.freeCashFlow', route: '/kpis/financial' },
  { kpiId: 'hr.headcount', labelKey: 'executive:headlines.headcount', route: '/kpis/hr' },
] as const;

export type Period = 'mtd' | 'qtd' | 'ytd';

export interface DomainConfig {
  domain: string;
  labelKey: string;
  route: string | null;
  kpiIds: string[];
  chartKpiId: string | null;
  comingSoon?: boolean;
}

export const DOMAIN_CONFIGS: DomainConfig[] = [
  {
    domain: 'financial',
    labelKey: 'dashboard:domains.financial',
    route: '/kpis/financial',
    kpiIds: ['financial.revenue', 'financial.ebitda_margin', 'financial.net_margin', 'financial.operating_cash_flow'],
    chartKpiId: 'financial.revenue',
  },
  {
    domain: 'sales',
    labelKey: 'dashboard:domains.sales',
    route: '/kpis/sales',
    kpiIds: ['sales.mrr', 'sales.pipeline_value', 'sales.win_rate', 'sales.churn_rate'],
    chartKpiId: 'sales.mrr',
  },
  {
    domain: 'marketing',
    labelKey: 'dashboard:domains.marketing',
    route: '/kpis/marketing',
    kpiIds: ['marketing.cpl', 'marketing.marketing_roi', 'marketing.lead_conversion_rate', 'marketing.website_conversion'],
    chartKpiId: 'marketing.lead_conversion_rate',
  },
  {
    domain: 'hr',
    labelKey: 'dashboard:domains.hr',
    route: '/kpis/hr',
    kpiIds: ['hr.headcount', 'hr.turnover_rate', 'hr.time_to_hire', 'hr.absence_rate'],
    chartKpiId: 'hr.headcount',
  },
  {
    domain: 'operations',
    labelKey: 'executive:domains.operations',
    route: null,
    kpiIds: ['ops.document_processing_rate', 'ops.sla_compliance', 'ops.process_throughput'],
    chartKpiId: null,
    comingSoon: true,
  },
];

export function getAllSparklineKpiIds(): string[] {
  const headlineIds = HEADLINE_KPIS.map((h) => h.kpiId);
  const domainChartIds = DOMAIN_CONFIGS
    .filter((d) => d.chartKpiId && !d.comingSoon)
    .map((d) => d.chartKpiId!);
  return [...new Set([...headlineIds, ...domainChartIds])];
}

export function getDomainRoute(kpiId: string): string {
  const domain = kpiId.split('.')[0];
  const config = DOMAIN_CONFIGS.find((d) => d.domain === domain);
  return config?.route ?? '/kpis/financial';
}

export function getPeriodDates(period: Period): { from: string; to: string } {
  const now = new Date();
  const y = now.getFullYear();
  const m = now.getMonth();
  const today = formatDate(now);

  switch (period) {
    case 'mtd':
      return { from: formatDate(new Date(y, m, 1)), to: today };
    case 'qtd': {
      const quarterStart = new Date(y, Math.floor(m / 3) * 3, 1);
      return { from: formatDate(quarterStart), to: today };
    }
    case 'ytd':
      return { from: `${y}-01-01`, to: today };
  }
}

export function getPriorPeriodDates(period: Period): { from: string; to: string } {
  const { from, to } = getPeriodDates(period);
  return {
    from: shiftYearBack(from),
    to: shiftYearBack(to),
  };
}

function formatDate(d: Date): string {
  return d.toISOString().slice(0, 10);
}

function shiftYearBack(dateStr: string): string {
  const [y, m, d] = dateStr.split('-');
  return `${Number(y) - 1}-${m}-${d}`;
}
