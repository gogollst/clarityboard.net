export interface CashFlowOverview {
  operatingCashFlow: number;
  investingCashFlow: number;
  financingCashFlow: number;
  netCashFlow: number;
  items: CashFlowItem[];
}

export interface CashFlowItem {
  category: string;
  description: string;
  amount: number;
}

export interface CashFlowForecast {
  weeks: ForecastWeek[];
  totalInflow: number;
  totalOutflow: number;
}

export interface ForecastWeek {
  weekStart: string;
  weekEnd: string;
  inflow: number;
  outflow: number;
  netFlow: number;
  cumulativeBalance: number;
}

export interface WorkingCapital {
  dso: number;
  dio: number;
  dpo: number;
  ccc: number;
  agingBuckets: AgingBucket[];
}

export interface AgingBucket {
  range: string;
  amount: number;
  count: number;
}

export interface CashFlowEntry {
  id: string;
  entityId: string;
  description: string;
  amount: number;
  entryDate: string;
  category: string;
  certainty: string;
}

export interface CreateCashFlowEntryRequest {
  entityId: string;
  description: string;
  amount: number;
  entryDate: string;
  category: string;
  certainty: string;
}
