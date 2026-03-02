using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace ClarityBoard.Application.Features.AI.Commands;

/// <summary>
/// Calls the "system.enhance_prompt" prompt via Anthropic to improve
/// the given system prompt. Returns a preview string (does NOT persist).
/// </summary>
public record EnhancePromptCommand : IRequest<string>
{
    public required string CurrentSystemPrompt { get; init; }
    public string? UserPromptTemplate { get; init; }
    public required string Description { get; init; }
    public required string FunctionDescription { get; init; }
}

public class EnhancePromptCommandValidator : AbstractValidator<EnhancePromptCommand>
{
    public EnhancePromptCommandValidator()
    {
        RuleFor(x => x.CurrentSystemPrompt).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.FunctionDescription).NotEmpty().MaximumLength(1000);
    }
}

public class EnhancePromptCommandHandler : IRequestHandler<EnhancePromptCommand, string>
{
    private readonly IPromptAiService _aiService;

    public EnhancePromptCommandHandler(IPromptAiService aiService)
        => _aiService = aiService;

    public async Task<string> Handle(
        EnhancePromptCommand request, CancellationToken cancellationToken)
    {
        return await _aiService.EnhancePromptAsync(
            request.CurrentSystemPrompt,
            request.UserPromptTemplate,
            request.Description,
            request.FunctionDescription,
            cancellationToken);
    }
}

