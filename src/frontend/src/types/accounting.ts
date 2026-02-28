export interface JournalEntry {
  id: string;
  entityId: string;
  entryDate: string;
  reference: string;
  description: string;
  sourceType: string;
  lines: JournalEntryLine[];
  createdAt: string;
  createdBy: string;
}

export interface JournalEntryLine {
  accountNumber: string;
  accountName: string;
  debit: number;
  credit: number;
  description?: string;
  costCenter?: string;
  vatRate?: number;
}

export interface CreateJournalEntryRequest {
  entityId: string;
  entryDate: string;
  reference: string;
  description: string;
  lines: Omit<JournalEntryLine, 'accountName'>[];
}

export interface TrialBalance {
  asOfDate: string;
  accounts: TrialBalanceAccount[];
}

export interface TrialBalanceAccount {
  accountNumber: string;
  accountName: string;
  accountClass: string;
  debitBalance: number;
  creditBalance: number;
}

export interface ProfitAndLoss {
  startDate: string;
  endDate: string;
  revenue: number;
  cogs: number;
  grossProfit: number;
  operatingExpenses: OperatingExpenseCategory[];
  ebit: number;
  interest: number;
  taxes: number;
  netIncome: number;
}

export interface OperatingExpenseCategory {
  name: string;
  amount: number;
}

export interface BalanceSheet {
  asOfDate: string;
  totalAssets: number;
  totalLiabilities: number;
  equity: number;
  assets: BalanceSheetSection[];
  liabilities: BalanceSheetSection[];
}

export interface BalanceSheetSection {
  name: string;
  amount: number;
  items: BalanceSheetItem[];
}

export interface BalanceSheetItem {
  name: string;
  amount: number;
}

export interface FiscalPeriod {
  id: string;
  entityId: string;
  year: number;
  month: number;
  status: FiscalPeriodStatus;
}

export type FiscalPeriodStatus = 'open' | 'soft_close' | 'exported' | 'hard_closed';

export interface CreateFiscalPeriodRequest {
  entityId: string;
  year: number;
  month: number;
}

export interface UpdateFiscalPeriodStatusRequest {
  id: string;
  status: FiscalPeriodStatus;
}

export interface VatReconciliation {
  outputVat: VatCategory[];
  inputVat: VatCategory[];
  netPayable: number;
}

export interface VatCategory {
  description: string;
  taxableAmount: number;
  vatAmount: number;
  rate: number;
}
