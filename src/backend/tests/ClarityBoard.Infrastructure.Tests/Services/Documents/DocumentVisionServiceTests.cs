using System.Reflection;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Infrastructure.Services.Documents;

namespace ClarityBoard.Infrastructure.Tests.Services.Documents;

public class DocumentVisionServiceTests
{
    [Fact]
    public void ExtractJson_FromMarkdownCodeFence_ReturnsJsonPayload()
    {
        var method = typeof(DocumentVisionService).GetMethod("ExtractJson", BindingFlags.NonPublic | BindingFlags.Static)!;

        var result = (string?)method.Invoke(null, new object?[] { "```json\n{\"full_text\":\"Hallo\"}\n```" });

        Assert.Equal("{\"full_text\":\"Hallo\"}", result);
    }

    [Fact]
    public void ExtractJson_FromWrappedText_ReturnsEmbeddedJsonObject()
    {
        var method = typeof(DocumentVisionService).GetMethod("ExtractJson", BindingFlags.NonPublic | BindingFlags.Static)!;

        var result = (string?)method.Invoke(null, new object?[] { "Antwort:\n{\"full_text\":\"Scan\",\"confidence\":0.8}\nDanke" });

        Assert.Equal("{\"full_text\":\"Scan\",\"confidence\":0.8}", result);
    }

    [Fact]
    public void ResolveEndpoint_WhenBaseUrlAlreadyContainsProviderPath_DoesNotDuplicateSegments()
    {
        var method = typeof(DocumentVisionService).GetMethod("ResolveEndpoint", BindingFlags.NonPublic | BindingFlags.Static)!;

        var result = (string?)method.Invoke(null, new object?[]
        {
            "https://generativelanguage.googleapis.com/v1beta",
            "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions",
            "v1beta/openai/chat/completions"
        });

        Assert.Equal("https://generativelanguage.googleapis.com/v1beta/openai/chat/completions", result);
    }

    [Fact]
    public void BuildUserPrompt_IncludesPageMetadataAndJsonInstruction()
    {
        var method = typeof(DocumentVisionService).GetMethod("BuildUserPrompt", BindingFlags.NonPublic | BindingFlags.Static)!;
        IReadOnlyList<VisionPageInput> pages = [
            new(1, [1, 2, 3], "image/png"),
            new(2, [4, 5, 6], "image/bmp"),
        ];

        var result = (string?)method.Invoke(null, new object?[] { pages });

        Assert.NotNull(result);
        Assert.Contains("page 1 (image/png)", result);
        Assert.Contains("page 2 (image/bmp)", result);
        Assert.Contains("Return only valid JSON", result);
    }
}