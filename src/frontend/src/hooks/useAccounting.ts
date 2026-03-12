import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { api } from '@/lib/api';
import { queryKeys } from '@/lib/queryKeys';
import i18n from '@/i18n';
import type { ApiResponse, PaginatedResponse } from '@/types/api';
import type {
  Account,
  JournalEntry,
  JournalEntryDetail,
  JournalEntryListItem,
  CreateJournalEntryRequest,
  TrialBalance,
  ProfitAndLoss,
  BalanceSheet,
  FiscalPeriod,
  CreateFiscalPeriodRequest,
  UpdateFiscalPeriodStatusRequest,
  VatReconciliation,
  BusinessPartner,
  BusinessPartnerListItem,
  BusinessPartnerSearchItem,
  CreateBusinessPartnerRequest,
  UpdateBusinessPartnerRequest,
} from '@/types/accounting';

// ---------------------------------------------------------------------------
// Accounts
// ---------------------------------------------------------------------------

export function useAccounts(entityId: string | null, accountType?: string, activeOnly = true) {
  return useQuery({
    queryKey: [...queryKeys.accounting.accounts(entityId ?? ''), accountType, activeOnly],
    queryFn: async () => {
      const { data } = await api.get<Account[]>('/accounting/accounts', {
        params: { accountType, activeOnly },
      });
      return data;
    },
    enabled: !!entityId,
  });
}

// ---------------------------------------------------------------------------
// Journal Entries
// ---------------------------------------------------------------------------

interface JournalEntryParams {
  page?: number;
  pageSize?: number;
  year?: number;
  month?: number;
  status?: string;
}

export function useJournalEntries(
  entityId: string | null,
  params?: JournalEntryParams,
) {
  return useQuery({
    queryKey: [...queryKeys.accounting.journalEntries(entityId ?? ''), params],
    queryFn: async () => {
      const { data } = await api.get<
        ApiResponse<PaginatedResponse<JournalEntryListItem>>
      >('/accounting/journal-entries', {
        params: { entityId, ...params },
      });
      return data.data;
    },
    enabled: !!entityId,
  });
}

export function useJournalEntry(id: string | null, entityId: string | null) {
  return useQuery({
    queryKey: queryKeys.accounting.journalEntry(id ?? ''),
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<JournalEntryDetail>>(
        `/accounting/journal-entries/${id}`,
        { params: { entityId } },
      );
      return data.data;
    },
    enabled: !!id && !!entityId,
  });
}

export function useCreateJournalEntry() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: CreateJournalEntryRequest) => {
      const { data } = await api.post<ApiResponse<JournalEntry>>(
        '/accounting/journal-entries',
        request,
      );
      return data.data;
    },
    onSuccess: (_data, variables) => {
      toast.success(i18n.t('accounting:toast.journalEntryCreated'));
      queryClient.invalidateQueries({
        queryKey: queryKeys.accounting.journalEntries(variables.entityId),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.accounting.trialBalance(variables.entityId),
      });
    },
    onError: () => {
      toast.error(i18n.t('accounting:toast.journalEntryCreateError'));
    },
  });
}

export function useReverseJournalEntry() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      id,
      entityId,
      reason,
    }: {
      id: string;
      entityId: string;
      reason: string;
    }) => {
      const { data } = await api.post<ApiResponse<string>>(
        `/accounting/journal-entries/${id}/reverse`,
        { reason },
        { params: { entityId } },
      );
      return { reversalId: data.data, entityId };
    },
    onSuccess: ({ entityId }) => {
      toast.success(i18n.t('accounting:toast.journalEntryReversed'));
      queryClient.invalidateQueries({
        queryKey: queryKeys.accounting.journalEntries(entityId),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.accounting.trialBalance(entityId),
      });
    },
    onError: () => {
      toast.error(i18n.t('accounting:toast.journalEntryReverseError'));
    },
  });
}

// ---------------------------------------------------------------------------
// Financial Reports
// ---------------------------------------------------------------------------

export function useTrialBalance(
  entityId: string | null,
  year?: number,
  month?: number,
) {
  return useQuery({
    queryKey: [...queryKeys.accounting.trialBalance(entityId ?? ''), year, month],
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<TrialBalance>>(
        '/accounting/trial-balance',
        { params: { entityId, year, month } },
      );
      return data.data;
    },
    enabled: !!entityId && !!year && !!month,
  });
}

export function useProfitAndLoss(
  entityId: string | null,
  year?: number,
  month?: number,
  compareYear?: number,
  compareMonth?: number,
) {
  return useQuery({
    queryKey: [
      ...queryKeys.accounting.profitAndLoss(entityId ?? ''),
      year,
      month,
      compareYear,
      compareMonth,
    ],
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<ProfitAndLoss>>(
        '/accounting/pnl',
        { params: { entityId, year, month, compareYear, compareMonth } },
      );
      return data.data;
    },
    enabled: !!entityId && !!year && !!month,
  });
}

export function useBalanceSheet(
  entityId: string | null,
  year?: number,
  month?: number,
  compareYear?: number,
  compareMonth?: number,
) {
  return useQuery({
    queryKey: [
      ...queryKeys.accounting.balanceSheet(entityId ?? ''),
      year,
      month,
      compareYear,
      compareMonth,
    ],
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<BalanceSheet>>(
        '/accounting/balance-sheet',
        { params: { entityId, year, month, compareYear, compareMonth } },
      );
      return data.data;
    },
    enabled: !!entityId && !!year && !!month,
  });
}

// ---------------------------------------------------------------------------
// Fiscal Periods
// ---------------------------------------------------------------------------

export function useFiscalPeriods(entityId: string | null) {
  return useQuery({
    queryKey: queryKeys.accounting.fiscalPeriods(entityId ?? ''),
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<FiscalPeriod[]>>(
        '/accounting/fiscal-periods',
        { params: { entityId } },
      );
      return data.data;
    },
    enabled: !!entityId,
  });
}

export function useCreateFiscalPeriod() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: CreateFiscalPeriodRequest) => {
      const { data } = await api.post<ApiResponse<FiscalPeriod>>(
        '/accounting/fiscal-periods',
        request,
      );
      return data.data;
    },
    onSuccess: (_data, variables) => {
      toast.success(i18n.t('accounting:toast.fiscalPeriodCreated'));
      queryClient.invalidateQueries({
        queryKey: queryKeys.accounting.fiscalPeriods(variables.entityId),
      });
    },
    onError: () => {
      toast.error(i18n.t('accounting:toast.fiscalPeriodCreateError'));
    },
  });
}

export function useUpdateFiscalPeriodStatus() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      id,
      status,
      entityId,
    }: UpdateFiscalPeriodStatusRequest & { entityId: string }) => {
      const { data } = await api.put<ApiResponse<FiscalPeriod>>(
        `/accounting/fiscal-periods/${id}/status`,
        { status },
      );
      return { period: data.data, entityId };
    },
    onSuccess: ({ entityId }) => {
      toast.success(i18n.t('accounting:toast.fiscalPeriodClosed'));
      queryClient.invalidateQueries({
        queryKey: queryKeys.accounting.fiscalPeriods(entityId),
      });
    },
    onError: () => {
      toast.error(i18n.t('accounting:toast.fiscalPeriodCloseError'));
    },
  });
}

// ---------------------------------------------------------------------------
// VAT Reconciliation
// ---------------------------------------------------------------------------

export function useVatReconciliation(
  entityId: string | null,
  periodStart?: string,
  periodEnd?: string,
) {
  return useQuery({
    queryKey: [
      ...queryKeys.accounting.vatReconciliation(entityId ?? ''),
      periodStart,
      periodEnd,
    ],
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<VatReconciliation>>(
        '/accounting/vat-reconciliation',
        { params: { entityId, periodStart, periodEnd } },
      );
      return data.data;
    },
    enabled: !!entityId && !!periodStart && !!periodEnd,
  });
}

// ---------------------------------------------------------------------------
// Post Journal Entry (GoBD hash chaining)
// ---------------------------------------------------------------------------

export function usePostJournalEntry() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, entityId }: { id: string; entityId: string }) => {
      const { data } = await api.post<ApiResponse<{ entryNumber: number; hash: string }>>(
        `/accounting/journal-entries/${id}/post`,
      );
      return { result: data.data, entityId };
    },
    onSuccess: ({ entityId }) => {
      toast.success(i18n.t('accounting:toast.journalEntryPosted'));
      queryClient.invalidateQueries({ queryKey: queryKeys.accounting.journalEntries(entityId) });
    },
    onError: () => {
      toast.error(i18n.t('accounting:toast.journalEntryPostError'));
    },
  });
}

// ---------------------------------------------------------------------------
// Cost Centers
// ---------------------------------------------------------------------------

export interface CostCenter {
  id: string;
  code: string;
  shortName: string;
  description?: string;
  type: string;
  hrEmployeeId?: string;
  hrDepartmentId?: string;
  isActive: boolean;
}

export interface CreateCostCenterRequest {
  code: string;
  shortName: string;
  description?: string;
  type?: string;
  hrEmployeeId?: string;
  hrDepartmentId?: string;
  parentId?: string;
}

export function useCostCenters(entityId: string | null, activeOnly = true) {
  return useQuery({
    queryKey: queryKeys.accounting.costCenters(entityId ?? ''),
    queryFn: async () => {
      const { data } = await api.get<CostCenter[]>('/accounting/cost-centers', {
        params: { entityId, activeOnly },
      });
      return data;
    },
    enabled: !!entityId,
  });
}

export function useCreateCostCenter() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ entityId, ...body }: CreateCostCenterRequest & { entityId: string }) => {
      const { data } = await api.post<string>('/accounting/cost-centers', body);
      return { id: data, entityId };
    },
    onSuccess: ({ entityId }) => {
      toast.success(i18n.t('accounting:toast.costCenterCreated'));
      queryClient.invalidateQueries({ queryKey: queryKeys.accounting.costCenters(entityId) });
    },
    onError: () => {
      toast.error(i18n.t('accounting:toast.costCenterCreateError'));
    },
  });
}

// ---------------------------------------------------------------------------
// DATEV Exports
// ---------------------------------------------------------------------------

export interface DatevExportRecord {
  id: string;
  fiscalPeriodId: string;
  exportType: string;
  status: string;
  fileCount?: number;
  recordCount?: number;
  errorDetails?: string;
  createdAt: string;
  completedAt?: string;
}

export function useDatevExports(entityId: string | null) {
  return useQuery({
    queryKey: queryKeys.accounting.datevExports(entityId ?? ''),
    queryFn: async () => {
      const { data } = await api.get<DatevExportRecord[]>('/accounting/datev/exports', {
        params: { entityId },
      });
      return data;
    },
    enabled: !!entityId,
  });
}

export function useGenerateDatevExport() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      entityId,
      fiscalPeriodId,
      exportType = 'Buchungsstapel',
    }: {
      entityId: string;
      fiscalPeriodId: string;
      exportType?: string;
    }) => {
      const { data } = await api.post<string>('/accounting/datev/exports', {
        fiscalPeriodId,
        exportType,
      });
      return { id: data, entityId };
    },
    onSuccess: ({ entityId }) => {
      toast.success(i18n.t('accounting:toast.datevExportGenerated'));
      queryClient.invalidateQueries({ queryKey: queryKeys.accounting.datevExports(entityId) });
    },
    onError: () => {
      toast.error(i18n.t('accounting:toast.datevExportError'));
    },
  });
}

// ---------------------------------------------------------------------------
// Accounting Scenarios
// ---------------------------------------------------------------------------

export interface AccountingScenario {
  id: string;
  name: string;
  description?: string;
  scenarioType: string;
  year: number;
  isLocked: boolean;
  isBaseline: boolean;
  createdAt: string;
}

export function useAccountingScenarios(entityId: string | null, year?: number) {
  return useQuery({
    queryKey: [...queryKeys.accounting.accountingScenarios(entityId ?? ''), year],
    queryFn: async () => {
      const { data } = await api.get<AccountingScenario[]>('/accounting/scenarios', {
        params: { entityId, year },
      });
      return data;
    },
    enabled: !!entityId,
  });
}

export function useCreateAccountingScenario() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      entityId,
      name,
      description,
      scenarioType = 'Budget',
      year,
    }: {
      entityId: string;
      name: string;
      description?: string;
      scenarioType?: string;
      year: number;
    }) => {
      const { data } = await api.post<string>('/accounting/scenarios', {
        name,
        description,
        scenarioType,
        year,
      });
      return { id: data, entityId };
    },
    onSuccess: ({ entityId }) => {
      toast.success(i18n.t('accounting:toast.scenarioCreated'));
      queryClient.invalidateQueries({ queryKey: queryKeys.accounting.accountingScenarios(entityId) });
    },
    onError: () => {
      toast.error(i18n.t('accounting:toast.scenarioCreateError'));
    },
  });
}

// ---------------------------------------------------------------------------
// Travel Costs Sync
// ---------------------------------------------------------------------------

export interface SyncTravelCostsRequest {
  entityId: string;
  fromDate: string;
  toDate: string;
}

export interface SyncTravelCostsResult {
  syncedCount: number;
  skippedCount: number;
  journalEntryIds: string[];
  errors: string[];
}

export function useSyncTravelCosts() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: SyncTravelCostsRequest) => {
      const { data } = await api.post<SyncTravelCostsResult>(
        '/accounting/travel-costs/sync',
        request,
      );
      return { result: data, entityId: request.entityId };
    },
    onSuccess: ({ result, entityId }) => {
      toast.success(
        i18n.t('accounting:toast.travelSynced', {
          count: result.syncedCount,
          skipped: result.skippedCount,
        }),
      );
      queryClient.invalidateQueries({ queryKey: queryKeys.accounting.journalEntries(entityId) });
    },
    onError: () => {
      toast.error(i18n.t('accounting:toast.travelSyncError'));
    },
  });
}

// ---------------------------------------------------------------------------
// Business Partners
// ---------------------------------------------------------------------------

interface BusinessPartnerParams {
  page?: number;
  pageSize?: number;
  isCreditor?: boolean;
  isDebtor?: boolean;
  isActive?: boolean;
  search?: string;
}

export function useBusinessPartners(
  entityId: string | null,
  params?: BusinessPartnerParams,
) {
  return useQuery({
    queryKey: [...queryKeys.accounting.businessPartners(entityId ?? ''), params],
    queryFn: async () => {
      const { data } = await api.get<
        ApiResponse<PaginatedResponse<BusinessPartnerListItem>>
      >('/accounting/business-partners', {
        params: { entityId, ...params },
      });
      return data.data;
    },
    enabled: !!entityId,
  });
}

export function useBusinessPartner(id: string | null, entityId: string | null) {
  return useQuery({
    queryKey: queryKeys.accounting.businessPartner(id ?? ''),
    queryFn: async () => {
      const { data } = await api.get<ApiResponse<BusinessPartner>>(
        `/accounting/business-partners/${id}`,
        { params: { entityId } },
      );
      return data.data;
    },
    enabled: !!id && !!entityId,
  });
}

export function useSearchBusinessPartners(entityId: string | null, query: string) {
  return useQuery({
    queryKey: [...queryKeys.accounting.businessPartnerSearch(entityId ?? ''), query],
    queryFn: async () => {
      const { data } = await api.get<BusinessPartnerSearchItem[]>(
        '/accounting/business-partners/search',
        { params: { entityId, q: query } },
      );
      return data;
    },
    enabled: !!entityId && query.length >= 2,
  });
}

export function useCreateBusinessPartner() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ entityId, ...body }: CreateBusinessPartnerRequest & { entityId: string }) => {
      const { data } = await api.post<ApiResponse<string>>(
        '/accounting/business-partners',
        body,
      );
      return { id: data.data, entityId };
    },
    onSuccess: ({ entityId }) => {
      toast.success(i18n.t('accounting:businessPartners.toast.created'));
      queryClient.invalidateQueries({
        queryKey: queryKeys.accounting.businessPartners(entityId),
      });
    },
    onError: () => {
      toast.error(i18n.t('accounting:businessPartners.toast.createError'));
    },
  });
}

export function useUpdateBusinessPartner() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ entityId, ...body }: UpdateBusinessPartnerRequest & { entityId: string }) => {
      const { data } = await api.put<ApiResponse<void>>(
        `/accounting/business-partners/${body.id}`,
        body,
      );
      return { entityId, id: body.id, data };
    },
    onSuccess: ({ entityId, id }) => {
      toast.success(i18n.t('accounting:businessPartners.toast.updated'));
      queryClient.invalidateQueries({
        queryKey: queryKeys.accounting.businessPartners(entityId),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.accounting.businessPartner(id),
      });
    },
    onError: () => {
      toast.error(i18n.t('accounting:businessPartners.toast.updateError'));
    },
  });
}

export function useDeactivateBusinessPartner() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, entityId }: { id: string; entityId: string }) => {
      await api.post(`/accounting/business-partners/${id}/deactivate`);
      return { entityId, id };
    },
    onSuccess: ({ entityId, id }) => {
      toast.success(i18n.t('accounting:businessPartners.toast.deactivated'));
      queryClient.invalidateQueries({
        queryKey: queryKeys.accounting.businessPartners(entityId),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.accounting.businessPartner(id),
      });
    },
    onError: () => {
      toast.error(i18n.t('accounting:businessPartners.toast.deactivateError'));
    },
  });
}

export function useAssignDocumentPartner() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      documentId,
      businessPartnerId,
      entityId,
    }: {
      documentId: string;
      businessPartnerId: string;
      entityId: string;
    }) => {
      await api.post(`/accounting/documents/${documentId}/assign-partner`, {
        businessPartnerId,
      });
      return { entityId, documentId };
    },
    onSuccess: ({ entityId, documentId }) => {
      toast.success(i18n.t('accounting:businessPartners.toast.partnerAssigned'));
      queryClient.invalidateQueries({ queryKey: queryKeys.documents.detail(documentId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.documents.list(entityId) });
    },
    onError: () => {
      toast.error(i18n.t('accounting:businessPartners.toast.assignError'));
    },
  });
}
