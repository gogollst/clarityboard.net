export const queryKeys = {
  kpi: {
    all: ['kpi'] as const,
    dashboard: (entityId: string) => ['kpi', 'dashboard', entityId] as const,
    history: (entityId: string, kpiId: string) =>
      ['kpi', 'history', entityId, kpiId] as const,
    definitions: () => ['kpi', 'definitions'] as const,
    alerts: (entityId: string) => ['kpi', 'alerts', entityId] as const,
    alertEvents: (alertId: string) =>
      ['kpi', 'alert-events', alertId] as const,
    drillDown: (entityId: string, kpiId: string) =>
      ['kpi', 'drill-down', entityId, kpiId] as const,
    workingCapital: (entityId: string) =>
      ['kpi', 'working-capital', entityId] as const,
  },

  accounting: {
    all: ['accounting'] as const,
    journalEntries: (entityId: string) =>
      ['accounting', 'journal-entries', entityId] as const,
    journalEntry: (id: string) =>
      ['accounting', 'journal-entry', id] as const,
    trialBalance: (entityId: string) =>
      ['accounting', 'trial-balance', entityId] as const,
    profitAndLoss: (entityId: string) =>
      ['accounting', 'profit-loss', entityId] as const,
    balanceSheet: (entityId: string) =>
      ['accounting', 'balance-sheet', entityId] as const,
    fiscalPeriods: (entityId: string) =>
      ['accounting', 'fiscal-periods', entityId] as const,
    vatReconciliation: (entityId: string) =>
      ['accounting', 'vat-reconciliation', entityId] as const,
  },

  cashflow: {
    all: ['cashflow'] as const,
    overview: (entityId: string) =>
      ['cashflow', 'overview', entityId] as const,
    forecast: (entityId: string) =>
      ['cashflow', 'forecast', entityId] as const,
    workingCapital: (entityId: string) =>
      ['cashflow', 'working-capital', entityId] as const,
    entries: (entityId: string) =>
      ['cashflow', 'entries', entityId] as const,
  },

  documents: {
    all: ['documents'] as const,
    list: (entityId: string) => ['documents', 'list', entityId] as const,
    detail: (id: string) => ['documents', 'detail', id] as const,
    downloadUrl: (id: string) =>
      ['documents', 'download-url', id] as const,
  },

  datev: {
    all: ['datev'] as const,
    exports: (entityId: string) => ['datev', 'exports', entityId] as const,
    downloadUrl: (exportId: string) =>
      ['datev', 'download-url', exportId] as const,
  },

  scenarios: {
    all: ['scenarios'] as const,
    list: (entityId: string) => ['scenarios', 'list', entityId] as const,
    detail: (id: string) => ['scenarios', 'detail', id] as const,
  },

  budget: {
    all: ['budget'] as const,
    list: (entityId: string) => ['budget', 'list', entityId] as const,
    detail: (id: string) => ['budget', 'detail', id] as const,
    planVsActual: (budgetId: string) =>
      ['budget', 'plan-vs-actual', budgetId] as const,
  },

  assets: {
    all: ['assets'] as const,
    list: (entityId: string) => ['assets', 'list', entityId] as const,
    detail: (id: string) => ['assets', 'detail', id] as const,
    anlagenspiegel: (entityId: string, year: number) =>
      ['assets', 'anlagenspiegel', entityId, year] as const,
  },

  webhooks: {
    all: ['webhooks'] as const,
    configs: (entityId: string) =>
      ['webhooks', 'configs', entityId] as const,
    deadLetter: (entityId: string) =>
      ['webhooks', 'dead-letter', entityId] as const,
  },

  entities: {
    all: ['entities'] as const,
    list: () => ['entities', 'list'] as const,
  },

  admin: {
    all: ['admin'] as const,
    users: () => ['admin', 'users'] as const,
    roles: () => ['admin', 'roles'] as const,
    auditLogs: () => ['admin', 'audit-logs'] as const,
  },

  settings: {
    all: ['settings'] as const,
    profile: () => ['settings', 'profile'] as const,
  },
} as const;
