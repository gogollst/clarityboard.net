export type BudgetStatus = 'draft' | 'approved' | 'active' | 'closed';

export interface Budget {
  id: string;
  entityId: string;
  name: string;
  year: number;
  status: BudgetStatus;
  totalAmount: number;
  lines: BudgetLine[];
}

export interface BudgetLine {
  accountNumber: string;
  accountName: string;
  monthlyAmounts: number[];
  totalAmount: number;
}

export interface BudgetRevision {
  id: string;
  budgetId: string;
  note: string;
  createdAt: string;
}

export interface PlanVsActual {
  lines: PlanVsActualLine[];
  totalPlanned: number;
  totalActual: number;
  totalVariance: number;
}

export interface PlanVsActualLine {
  accountNumber: string;
  accountName: string;
  planned: number;
  actual: number;
  variance: number;
  variancePct: number;
}

export interface CreateBudgetRequest {
  entityId: string;
  name: string;
  year: number;
  lines: Omit<BudgetLine, 'accountName'>[];
}

export interface UpdateBudgetRequest {
  id: string;
  name?: string;
  status?: BudgetStatus;
  lines?: Omit<BudgetLine, 'accountName'>[];
}

export interface CreateBudgetRevisionRequest {
  budgetId: string;
  note: string;
}
