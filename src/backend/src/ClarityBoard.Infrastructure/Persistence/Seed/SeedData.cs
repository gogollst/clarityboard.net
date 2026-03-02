using ClarityBoard.Domain.Entities.AI;
using ClarityBoard.Domain.Entities.Accounting;
using ClarityBoard.Domain.Entities.Entity;
using ClarityBoard.Domain.Entities.Identity;
using ClarityBoard.Domain.Entities.KPI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Persistence.Seed;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ClarityBoardContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ClarityBoardContext>>();

        try
        {
            await SeedPermissionsAsync(context, logger);
            await SeedRolesAsync(context, logger);
            await SeedKpiDefinitionsAsync(context, logger);
            await SeedAiPromptsAsync(context, logger);
            await SeedDevAdminAsync(context, logger);

            logger.LogInformation("Seed data initialization completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during seed data initialization");
            throw;
        }
    }

    /// <summary>
    /// Seeds SKR03 accounts for a specific legal entity.
    /// Call this when a new entity is created with ChartOfAccounts = "SKR03".
    /// </summary>
    public static async Task SeedSkr03AccountsAsync(
        ClarityBoardContext context,
        Guid entityId,
        ILogger logger)
    {
        var existingCount = await context.Accounts
            .CountAsync(a => a.EntityId == entityId);

        if (existingCount > 0)
        {
            logger.LogInformation("Entity {EntityId} already has {Count} accounts, skipping SKR03 seed", entityId, existingCount);
            return;
        }

        var accounts = Skr03Accounts.All.Select(a =>
            Account.Create(
                entityId,
                a.Number,
                a.Name,
                a.AccountType,
                a.AccountClass,
                vatDefault: a.VatDefault)).ToList();

        context.Accounts.AddRange(accounts);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} SKR03 accounts for entity {EntityId}", accounts.Count, entityId);
    }

    /// <summary>
    /// Seeds a development admin user + test legal entity (only if no users exist).
    /// Email: admin@clarityboard.net / Password: Admin123!
    /// </summary>
    private static async Task SeedDevAdminAsync(ClarityBoardContext context, ILogger logger)
    {
        if (await context.Users.AnyAsync())
            return;

        // Create test legal entity
        var entity = LegalEntity.Create(
            name: "ClarityBoard Demo GmbH",
            legalForm: "GmbH",
            street: "Musterstraße 1",
            city: "Berlin",
            postalCode: "10115");
        context.LegalEntities.Add(entity);
        await context.SaveChangesAsync();

        // Seed SKR03 accounts for this entity
        await SeedSkr03AccountsAsync(context, entity.Id, logger);

        // Create admin user (BCrypt hash of "Admin123!")
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
        var user = User.Create(
            email: "admin@clarityboard.net",
            passwordHash: passwordHash,
            firstName: "Admin",
            lastName: "User");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Assign Admin role for the test entity
        var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
        var userRole = UserRole.Create(user.Id, adminRole.Id, entity.Id, user.Id);
        context.UserRoles.Add(userRole);
        await context.SaveChangesAsync();

        logger.LogInformation(
            "Seeded dev admin user: admin@clarityboard.net (entity: {EntityName}, role: Admin)",
            entity.Name);
    }

    private static async Task SeedAiPromptsAsync(ClarityBoardContext context, ILogger logger)
    {
        if (await context.AiPrompts.AnyAsync())
            return;

        var prompts = AiPromptsSeed.All.Select(p => AiPrompt.Create(
            promptKey:           p.Key,
            name:                p.Name,
            description:         p.Description,
            module:              p.Module,
            functionDescription: p.FunctionDescription,
            systemPrompt:        p.SystemPrompt,
            userPromptTemplate:  p.UserTemplate,
            primaryProvider:     p.Primary,
            primaryModel:        p.PrimaryModel,
            fallbackProvider:    p.Fallback,
            fallbackModel:       p.FallbackModel,
            temperature:         p.Temp,
            maxTokens:           p.MaxTok,
            isSystemPrompt:      true)).ToList();

        context.AiPrompts.AddRange(prompts);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} AI prompts", prompts.Count);
    }

    private static async Task SeedPermissionsAsync(ClarityBoardContext context, ILogger logger)
    {
        if (await context.Permissions.AnyAsync())
            return;

        var permissions = DefaultRoles.AllPermissions.Select(p =>
            Permission.Create(p.Name, p.Module, p.Action)).ToList();

        context.Permissions.AddRange(permissions);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} permissions", permissions.Count);
    }

    private static async Task SeedRolesAsync(ClarityBoardContext context, ILogger logger)
    {
        if (await context.Roles.AnyAsync())
            return;

        var allPermissions = await context.Permissions.ToListAsync();

        foreach (var roleSeed in DefaultRoles.AllRoles)
        {
            var role = Role.Create(roleSeed.Name, roleSeed.Description, isSystem: true);
            var rolePermissions = allPermissions
                .Where(p => roleSeed.Permissions.Contains(p.Name))
                .ToList();

            foreach (var perm in rolePermissions)
                role.AddPermission(perm);

            context.Roles.Add(role);
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} roles", DefaultRoles.AllRoles.Length);
    }

    private static async Task SeedKpiDefinitionsAsync(ClarityBoardContext context, ILogger logger)
    {
        if (await context.KpiDefinitions.AnyAsync())
            return;

        var definitions = KpiDefinitionsSeed.All.Select(kpi =>
            KpiDefinition.Create(
                kpi.Id, kpi.Domain, kpi.Name, kpi.Formula, kpi.Unit, kpi.Direction,
                kpi.Category, kpi.CalculationClass, kpi.DisplayOrder)).ToList();

        context.KpiDefinitions.AddRange(definitions);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} KPI definitions", definitions.Count);
    }
}
