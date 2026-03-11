using ClarityBoard.Domain.Entities.AI;

namespace ClarityBoard.Domain.Tests.Entities.AI;

public class AiPromptRestoreTests
{
    [Fact]
    public void Restore_ShouldRestoreModelsAndSettings()
    {
        var prompt = AiPrompt.Create(
            "document_extraction",
            "Document Extraction",
            "desc",
            "Document",
            "fn",
            "system-v1",
            "user-v1",
            AiProvider.Anthropic,
            "claude-sonnet-4-20250514",
            AiProvider.Gemini,
            "gemini-2.5-flash",
            0.1m,
            2048,
            true);

        prompt.Restore(
            "system-v2",
            "user-v2",
            AiProvider.OpenAI,
            "gpt-4o",
            AiProvider.Manus,
            "manus-large",
            0.6m,
            4096,
            Guid.NewGuid());

        Assert.Equal("system-v2", prompt.SystemPrompt);
        Assert.Equal("user-v2", prompt.UserPromptTemplate);
        Assert.Equal(AiProvider.OpenAI, prompt.PrimaryProvider);
        Assert.Equal("gpt-4o", prompt.PrimaryModel);
        Assert.Equal(AiProvider.Manus, prompt.FallbackProvider);
        Assert.Equal("manus-large", prompt.FallbackModel);
        Assert.Equal(0.6m, prompt.Temperature);
        Assert.Equal(4096, prompt.MaxTokens);
        Assert.Equal(2, prompt.Version);
    }
}