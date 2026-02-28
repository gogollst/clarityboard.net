namespace ClarityBoard.Domain.Entities.Identity;

public class Permission
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!; // e.g. accounting:read, accounting:write, kpi:manage
    public string Module { get; private set; } = default!; // accounting, kpi, entity, cashflow, scenario, document, budget, asset, integration, admin
    public string Action { get; private set; } = default!; // read, write, manage, export, approve

    private Permission() { }

    public static Permission Create(string name, string module, string action)
    {
        return new Permission
        {
            Id = Guid.NewGuid(),
            Name = name,
            Module = module,
            Action = action,
        };
    }
}
