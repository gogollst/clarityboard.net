namespace ClarityBoard.Domain.Entities.AI;

/// <summary>
/// Central registry for all AI prompts used in the system.
/// Every AI call must reference a prompt by its unique PromptKey.
/// Hardcoded prompt strings in application code are forbidden.
/// </summary>
public class AiPrompt
{
    public Guid Id { get; private set; }

    /// <summary>Unique identifier used in code, e.g. "document.booking_suggestion".</summary>
    public string PromptKey { get; private set; } = default!;

    /// <summary>Human-readable name shown in admin UI.</summary>
    public string Name { get; private set; } = default!;

    /// <summary>Detailed description: purpose, context, usage.</summary>
    public string Description { get; private set; } = default!;

    /// <summary>Owning module, e.g. "Document", "KPI", "CashFlow".</summary>
    public string Module { get; private set; } = default!;

    /// <summary>Precise description of inputs, outputs and side effects of this prompt.</summary>
    public string FunctionDescription { get; private set; } = default!;

    public string SystemPrompt { get; private set; } = default!;

    /// <summary>User-side template with {{variable_name}} placeholders.</summary>
    public string? UserPromptTemplate { get; private set; }

    public string? ExampleInput { get; private set; }
    public string? ExampleOutput { get; private set; }

    public AiProvider PrimaryProvider { get; private set; }
    public string PrimaryModel { get; private set; } = default!;
    public AiProvider FallbackProvider { get; private set; }
    public string FallbackModel { get; private set; } = default!;

    public decimal Temperature { get; private set; }
    public int MaxTokens { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>true = managed by the system; false = user-edited.</summary>
    public bool IsSystemPrompt { get; private set; }

    /// <summary>Incremented on every update.</summary>
    public int Version { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid? LastEditedByUserId { get; private set; }

    private AiPrompt() { }

    public static AiPrompt Create(
        string promptKey, string name, string description, string module,
        string functionDescription, string systemPrompt, string? userPromptTemplate,
        AiProvider primaryProvider, string primaryModel,
        AiProvider fallbackProvider, string fallbackModel,
        decimal temperature = 0.3m, int maxTokens = 4096,
        bool isSystemPrompt = true,
        string? exampleInput = null, string? exampleOutput = null)
    {
        return new AiPrompt
        {
            Id                  = Guid.NewGuid(),
            PromptKey           = promptKey,
            Name                = name,
            Description         = description,
            Module              = module,
            FunctionDescription = functionDescription,
            SystemPrompt        = systemPrompt,
            UserPromptTemplate  = userPromptTemplate,
            PrimaryProvider     = primaryProvider,
            PrimaryModel        = primaryModel,
            FallbackProvider    = fallbackProvider,
            FallbackModel       = fallbackModel,
            Temperature         = temperature,
            MaxTokens           = maxTokens,
            IsActive            = true,
            IsSystemPrompt      = isSystemPrompt,
            Version             = 1,
            ExampleInput        = exampleInput,
            ExampleOutput       = exampleOutput,
            CreatedAt           = DateTime.UtcNow,
            UpdatedAt           = DateTime.UtcNow,
        };
    }

    public void Update(
        string systemPrompt, string? userPromptTemplate,
        AiProvider primaryProvider, string primaryModel,
        AiProvider fallbackProvider, string fallbackModel,
        decimal temperature, int maxTokens, Guid editedByUserId)
    {
        SystemPrompt        = systemPrompt;
        UserPromptTemplate  = userPromptTemplate;
        PrimaryProvider     = primaryProvider;
        PrimaryModel        = primaryModel;
        FallbackProvider    = fallbackProvider;
        FallbackModel       = fallbackModel;
        Temperature         = temperature;
        MaxTokens           = maxTokens;
        Version             += 1;
        LastEditedByUserId  = editedByUserId;
        IsSystemPrompt      = false;
        UpdatedAt           = DateTime.UtcNow;
    }

    public void Restore(
        string systemPrompt,
        string? userPromptTemplate,
        AiProvider primaryProvider,
        string primaryModel,
        AiProvider fallbackProvider,
        string fallbackModel,
        decimal temperature,
        int maxTokens,
        Guid editedByUserId)
    {
        SystemPrompt       = systemPrompt;
        UserPromptTemplate = userPromptTemplate;
        PrimaryProvider    = primaryProvider;
        PrimaryModel       = primaryModel;
        FallbackProvider   = fallbackProvider;
        FallbackModel      = fallbackModel;
        Temperature        = temperature;
        MaxTokens          = maxTokens;
        Version            += 1;
        LastEditedByUserId = editedByUserId;
        UpdatedAt          = DateTime.UtcNow;
    }

    public void SetActive(bool active) { IsActive = active; UpdatedAt = DateTime.UtcNow; }
}

