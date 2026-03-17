// ---------------------------------------------------------------------------
// Journal Entries (List)
// ---------------------------------------------------------------------------

export interface JournalEntryListItem {
  id: string;
  entryNumber: number;
  entryDate: string;
  description: string;
  status: string;
  sourceType?: string;
  totalAmount: number;
  lineCount: number;
  createdAt: string;
}

// ---------------------------------------------------------------------------
// Journal Entry (Create Response / General)
// ---------------------------------------------------------------------------

export interface JournalEntry {
  id: string;
  entryNumber: number;
  entryDate: string;
  postingDate: string;
  description: string;
  status: string;
  sourceType?: string;
  createdAt: string;
  lines: JournalEntryLine[];
}

export interface JournalEntryLine {
  id: string;
  lineNumber: number;
  accountId: string;
  accountNumber: string;
  accountName: string;
  accountNameDe?: string;
  accountNameEn?: string;
  accountNameRu?: string;
  debitAmount: number;
  creditAmount: number;
  currency: string;
  vatCode?: string;
  vatAmount?: number;
  costCenter?: string;
  description?: string;
}

// ---------------------------------------------------------------------------
// Journal Entry Detail
// ---------------------------------------------------------------------------

export interface JournalEntryDetail {
  id: string;
  entryNumber: number;
  entryDate: string;
  postingDate: string;
  description: string;
  status: string;
  sourceType?: string;
  sourceRef?: string;
  fiscalPeriodId: string;
  isReversal: boolean;
  reversalOf?: string;
  hash: string;
  createdAt: string;
  createdBy: string;
  lines: JournalEntryDetailLine[];
}

export interface JournalEntryDetailLine {
  id: string;
  lineNumber: number;
  accountId: string;
  accountNumber: string;
  accountName: string;
  accountNameDe?: string;
  accountNameEn?: string;
  accountNameRu?: string;
  debitAmount: number;
  creditAmount: number;
  currency: string;
  vatAmount: number;
  vatCode?: string;
  costCenter?: string;
  description?: string;
}

// ---------------------------------------------------------------------------
// Create Journal Entry
// ---------------------------------------------------------------------------

export interface CreateJournalEntryRequest {
  entityId: string;
  entryDate: string;
  description: string;
  documentId?: string;
  sourceType?: string;
  sourceRef?: string;
  lines: CreateJournalEntryLineRequest[];
}

export interface CreateJournalEntryLineRequest {
  accountId: string;
  debitAmount: number;
  creditAmount: number;
  currency?: string;
  exchangeRate?: number;
  vatCode?: string;
  vatAmount?: number;
  costCenter?: string;
  description?: string;
}

// ---------------------------------------------------------------------------
// Update Journal Entry
// ---------------------------------------------------------------------------

export interface UpdateJournalEntryRequest {
  id: string;
  entityId: string;
  entryDate: string;
  description: string;
  lines: CreateJournalEntryLineRequest[];
}

// ---------------------------------------------------------------------------
// Account
// ---------------------------------------------------------------------------

export interface Account {
  id: string;
  accountNumber: string;
  name: string;
  accountType: string;
  accountClass: number;
  isActive: boolean;
  vatDefault?: string;
  nameDe?: string;
  nameEn?: string;
  nameRu?: string;
}

export interface AccountDetail extends Account {
  datevAuto?: string;
  costCenterDefault?: string;
  bwaLine?: string;
  isAutoPosting: boolean;
  isSystemAccount: boolean;
  parentId?: string;
  nameDe?: string;
  nameEn?: string;
  nameRu?: string;
  journalEntryCount: number;
  lastBookingDate?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateAccountRequest {
  accountNumber: string;
  name: string;
  accountType: string;
  accountClass: number;
  vatDefault?: string;
  datevAuto?: string;
  costCenterDefault?: string;
  bwaLine?: string;
  isAutoPosting?: boolean;
  sourceLanguage?: string;
}

export interface UpdateAccountRequest {
  id: string;
  name: string;
  vatDefault?: string;
  costCenterDefault?: string;
  bwaLine?: string;
  isAutoPosting?: boolean;
  sourceLanguage?: string;
}

// ---------------------------------------------------------------------------
// Trial Balance
// ---------------------------------------------------------------------------

export interface TrialBalanceLine {
  accountNumber: string;
  accountName: string;
  accountNameDe?: string;
  accountNameEn?: string;
  accountNameRu?: string;
  accountType: string;
  accountClass: number;
  debitTotal: number;
  creditTotal: number;
  balance: number;
}

export interface TrialBalance {
  year: number;
  month: number;
  lines: TrialBalanceLine[];
  totalDebits: number;
  totalCredits: number;
}

// ---------------------------------------------------------------------------
// Profit & Loss
// ---------------------------------------------------------------------------

export interface PnlLineItem {
  label: string;
  amount: number;
  priorAmount?: number;
}

export interface PnlSection {
  name: string;
  items: PnlLineItem[];
  subtotal: number;
  priorSubtotal?: number;
}

export interface ProfitAndLoss {
  year: number;
  month: number;
  compareYear?: number;
  compareMonth?: number;
  sections: PnlSection[];
  netIncome: number;
  priorNetIncome?: number;
}

// ---------------------------------------------------------------------------
// Payroll Posting
// ---------------------------------------------------------------------------

export interface PayrollPostingRequest {
  entityId: string;
  year: number;
  month: number;
}

export interface PayrollPostingError {
  employeeName: string;
  error: string;
}

export interface PayrollPostingResult {
  employeesProcessed: number;
  journalEntriesCreated: number;
  totalGrossSalary: number;
  totalSocialContributions: number;
  errors: PayrollPostingError[];
}

// ---------------------------------------------------------------------------
// Balance Sheet
// ---------------------------------------------------------------------------

export interface BalanceSheetLineItem {
  label: string;
  amount: number;
  priorAmount?: number;
}

export interface BalanceSheetSection {
  name: string;
  items: BalanceSheetLineItem[];
  subtotal: number;
  priorSubtotal?: number;
}

export interface BalanceSheet {
  asOfDate: string;
  priorDate?: string;
  assets: BalanceSheetSection[];
  totalAssets: number;
  priorTotalAssets?: number;
  liabilitiesAndEquity: BalanceSheetSection[];
  totalLiabilitiesAndEquity: number;
  priorTotalLiabilitiesAndEquity?: number;
}

// ---------------------------------------------------------------------------
// VAT Reconciliation
// ---------------------------------------------------------------------------

export interface VatCategoryAmounts {
  netAmount: number;
  vatAmount: number;
}

export interface VatLineDetail {
  accountNumber: string;
  accountName: string;
  vatCode: string;
  vatRate: number;
  vatType: string;
  netAmount: number;
  vatAmount: number;
}

export interface VatReconciliation {
  entityId: string;
  periodStart: string;
  periodEnd: string;
  outputVat19: VatCategoryAmounts;
  outputVat7: VatCategoryAmounts;
  outputVat0: VatCategoryAmounts;
  inputVat: VatCategoryAmounts;
  reverseChargeVat: VatCategoryAmounts;
  intraEuAcquisitions: VatCategoryAmounts;
  totalOutputVat: number;
  totalInputVat: number;
  netPayable: number;
  lineDetails: VatLineDetail[];
}

// ---------------------------------------------------------------------------
// Fiscal Periods
// ---------------------------------------------------------------------------

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

// ---------------------------------------------------------------------------
// Business Partners
// ---------------------------------------------------------------------------

export interface BusinessPartner {
  id: string;
  partnerNumber: string;
  name: string;
  taxId?: string;
  vatNumber?: string;
  street?: string;
  city?: string;
  postalCode?: string;
  country?: string;
  email?: string;
  phone?: string;
  bankName?: string;
  iban?: string;
  bic?: string;
  isCreditor: boolean;
  isDebtor: boolean;
  defaultExpenseAccountId?: string;
  defaultRevenueAccountId?: string;
  contactEmployeeId?: string;
  contactEmployeeName?: string;
  paymentTermDays: number;
  isActive: boolean;
  notes?: string;
  createdAt: string;
}

export interface BusinessPartnerListItem {
  id: string;
  partnerNumber: string;
  name: string;
  isCreditor: boolean;
  isDebtor: boolean;
  isActive: boolean;
  city?: string;
  openDocumentCount: number;
}

export interface BusinessPartnerSearchItem {
  id: string;
  partnerNumber: string;
  name: string;
  taxId?: string;
  iban?: string;
}

export interface CreateBusinessPartnerRequest {
  name: string;
  taxId?: string;
  vatNumber?: string;
  street?: string;
  city?: string;
  postalCode?: string;
  country?: string;
  email?: string;
  phone?: string;
  bankName?: string;
  iban?: string;
  bic?: string;
  isCreditor: boolean;
  isDebtor: boolean;
  defaultExpenseAccountId?: string;
  defaultRevenueAccountId?: string;
  contactEmployeeId?: string;
  paymentTermDays: number;
  notes?: string;
}

export interface UpdateBusinessPartnerRequest {
  id: string;
  name: string;
  taxId?: string;
  vatNumber?: string;
  street?: string;
  city?: string;
  postalCode?: string;
  country?: string;
  email?: string;
  phone?: string;
  bankName?: string;
  iban?: string;
  bic?: string;
  isCreditor: boolean;
  isDebtor: boolean;
  defaultExpenseAccountId?: string;
  defaultRevenueAccountId?: string;
  contactEmployeeId?: string;
  paymentTermDays: number;
  isActive: boolean;
  notes?: string;
}
