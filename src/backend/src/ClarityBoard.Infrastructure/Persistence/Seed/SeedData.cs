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
            await SeedMissingAccountsAsync(context, logger);

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

    /// <summary>
    /// Seeds SKR03 accounts for any entity that has zero accounts.
    /// This handles entities created before auto-seeding was added to CreateEntityCommand.
    /// </summary>
    private static async Task SeedMissingAccountsAsync(ClarityBoardContext context, ILogger logger)
    {
        var entitiesWithoutAccounts = await context.LegalEntities
            .Where(e => e.IsActive && !context.Accounts.Any(a => a.EntityId == e.Id))
            .Select(e => new { e.Id, e.ChartOfAccounts, e.Name })
            .ToListAsync();

        foreach (var entity in entitiesWithoutAccounts)
        {
            if (entity.ChartOfAccounts == "SKR03")
                await SeedSkr03AccountsAsync(context, entity.Id, logger);
        }
    }

    private static async Task SeedAiPromptsAsync(ClarityBoardContext context, ILogger logger)
    {
        var existingKeys = await context.AiPrompts
            .Select(p => p.PromptKey)
            .ToListAsync();

        var existingKeySet = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var prompts = AiPromptsSeed.All
            .Where(p => !existingKeySet.Contains(p.Key))
            .Select(p => AiPrompt.Create(
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
            isSystemPrompt:      true))
            .ToList();

        if (prompts.Count == 0)
            return;

        context.AiPrompts.AddRange(prompts);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} new AI prompts", prompts.Count);
    }

    private static async Task SeedPermissionsAsync(ClarityBoardContext context, ILogger logger)
    {
        var existing = (await context.Permissions.Select(p => p.Name).ToListAsync()).ToHashSet();
        var toAdd = DefaultRoles.AllPermissions
            .Where(p => !existing.Contains(p.Name))
            .Select(p => Permission.Create(p.Name, p.Module, p.Action))
            .ToList();

        if (toAdd.Count == 0) return;

        context.Permissions.AddRange(toAdd);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} new permissions", toAdd.Count);
    }

    private static async Task SeedRolesAsync(ClarityBoardContext context, ILogger logger)
    {
        var allPermissions = await context.Permissions.ToListAsync();
        var permsByName = allPermissions.ToDictionary(p => p.Name);

        foreach (var roleSeed in DefaultRoles.AllRoles)
        {
            var role = await context.Roles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.Name == roleSeed.Name);

            if (role is null)
            {
                role = Role.Create(roleSeed.Name, roleSeed.Description, isSystem: true);
                foreach (var permName in roleSeed.Permissions)
                    if (permsByName.TryGetValue(permName, out var perm))
                        role.AddPermission(perm);
                context.Roles.Add(role);
            }
            else
            {
                var existingPerms = role.Permissions.Select(p => p.Name).ToHashSet();
                foreach (var permName in roleSeed.Permissions)
                    if (!existingPerms.Contains(permName) && permsByName.TryGetValue(permName, out var perm))
                        role.AddPermission(perm);
            }
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Roles seeded/updated");
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
