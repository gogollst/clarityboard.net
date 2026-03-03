namespace ClarityBoard.Infrastructure.Persistence.Seed;

public static class DefaultRoles
{
    public record RoleSeed(string Name, string Description, string[] Permissions);

    public record PermissionSeed(string Name, string Module, string Action);

    public static readonly PermissionSeed[] AllPermissions = [
        // Accounting
        new("accounting.view", "accounting", "view"),
        new("accounting.create", "accounting", "create"),
        new("accounting.edit", "accounting", "edit"),
        new("accounting.delete", "accounting", "delete"),
        new("accounting.close_period", "accounting", "close_period"),
        new("accounting.reopen_period", "accounting", "reopen_period"),
        new("accounting.export", "accounting", "export"),
        new("accounting.reverse", "accounting", "reverse"),

        // KPI
        new("kpi.view", "kpi", "view"),
        new("kpi.configure", "kpi", "configure"),
        new("kpi.alerts.manage", "kpi", "alerts_manage"),
        new("kpi.alerts.acknowledge", "kpi", "alerts_acknowledge"),

        // Cash Flow
        new("cashflow.view", "cashflow", "view"),
        new("cashflow.create", "cashflow", "create"),
        new("cashflow.edit", "cashflow", "edit"),
        new("cashflow.forecast", "cashflow", "forecast"),

        // Documents
        new("documents.view", "documents", "view"),
        new("documents.upload", "documents", "upload"),
        new("documents.approve", "documents", "approve"),
        new("documents.delete", "documents", "delete"),

        // Budget
        new("budget.view", "budget", "view"),
        new("budget.create", "budget", "create"),
        new("budget.edit", "budget", "edit"),
        new("budget.approve", "budget", "approve"),
        new("budget.lock", "budget", "lock"),

        // Assets
        new("assets.view", "assets", "view"),
        new("assets.create", "assets", "create"),
        new("assets.edit", "assets", "edit"),
        new("assets.dispose", "assets", "dispose"),

        // Scenarios
        new("scenarios.view", "scenarios", "view"),
        new("scenarios.create", "scenarios", "create"),
        new("scenarios.edit", "scenarios", "edit"),
        new("scenarios.run", "scenarios", "run"),

        // Webhooks / Integration
        new("webhooks.view", "webhooks", "view"),
        new("webhooks.configure", "webhooks", "configure"),
        new("webhooks.retry", "webhooks", "retry"),

        // DATEV
        new("datev.view", "datev", "view"),
        new("datev.export", "datev", "export"),
        new("datev.download", "datev", "download"),

        // Entity
        new("entity.view", "entity", "view"),
        new("entity.create", "entity", "create"),
        new("entity.edit", "entity", "edit"),
        new("entity.delete", "entity", "delete"),

        // Admin
        new("admin.users.view", "admin", "users_view"),
        new("admin.users.create", "admin", "users_create"),
        new("admin.users.edit", "admin", "users_edit"),
        new("admin.users.delete", "admin", "users_delete"),
        new("admin.roles.manage", "admin", "roles_manage"),
        new("admin.audit.view", "admin", "audit_view"),
        new("admin.settings.manage", "admin", "settings_manage"),
        new("admin.mail.manage", "admin", "mail_manage"),
    ];

    public static readonly RoleSeed[] AllRoles = [
        new("Admin", "Full system administrator with all permissions", AllPermissions.Select(p => p.Name).ToArray()),

        new("Finance", "Finance team - accounting, DATEV, budget, cash flow", [
            "accounting.view", "accounting.create", "accounting.edit", "accounting.close_period", "accounting.export", "accounting.reverse",
            "kpi.view", "kpi.alerts.acknowledge",
            "cashflow.view", "cashflow.create", "cashflow.edit", "cashflow.forecast",
            "documents.view", "documents.upload", "documents.approve",
            "budget.view", "budget.create", "budget.edit", "budget.approve", "budget.lock",
            "assets.view", "assets.create", "assets.edit", "assets.dispose",
            "datev.view", "datev.export", "datev.download",
            "entity.view",
        ]),

        new("Executive", "Executive view - dashboards, KPIs, scenarios, reports", [
            "accounting.view",
            "kpi.view", "kpi.configure", "kpi.alerts.manage", "kpi.alerts.acknowledge",
            "cashflow.view", "cashflow.forecast",
            "documents.view",
            "budget.view", "budget.approve",
            "assets.view",
            "scenarios.view", "scenarios.create", "scenarios.edit", "scenarios.run",
            "datev.view",
            "entity.view",
        ]),

        new("Sales", "Sales team - sales KPIs, pipeline, forecasting", [
            "kpi.view",
            "cashflow.view",
            "documents.view", "documents.upload",
            "scenarios.view",
            "entity.view",
        ]),

        new("HR", "HR team - HR KPIs, workforce data", [
            "kpi.view",
            "budget.view",
            "documents.view", "documents.upload",
            "entity.view",
        ]),
    ];
}
