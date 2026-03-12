using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.AI.DTOs;
using ClarityBoard.Domain.Entities.AI;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.AI.Commands;

public record UpsertAiProviderCommand : IRequest<AiProviderConfigDto>
{
    public AiProvider Provider { get; init; }
    public required string ApiKey { get; init; }
    public string? BaseUrl { get; init; }
    public string? ModelDefault { get; init; }
}

public class UpsertAiProviderCommandValidator : AbstractValidator<UpsertAiProviderCommand>
{
    public UpsertAiProviderCommandValidator()
    {
        RuleFor(x => x.ApiKey).NotEmpty().MinimumLength(8)
            .WithMessage("API key must be at least 8 characters.");
    }
}

public class UpsertAiProviderCommandHandler
    : IRequestHandler<UpsertAiProviderCommand, AiProviderConfigDto>
{
    private readonly IAppDbContext _db;
    private readonly IEncryptionService _encryption;
    private readonly ICurrentUser _currentUser;
    private readonly IPromptAiService _aiService;
    private readonly ITranslationService _translationService;

    public UpsertAiProviderCommandHandler(
        IAppDbContext db,
        IEncryptionService encryption,
        ICurrentUser currentUser,
        IPromptAiService aiService,
        ITranslationService translationService)
    {
        _db         = db;
        _encryption = encryption;
        _currentUser = currentUser;
        _aiService  = aiService;
        _translationService = translationService;
    }

    public async Task<AiProviderConfigDto> Handle(
        UpsertAiProviderCommand request, CancellationToken cancellationToken)
    {
        var keyHint     = request.ApiKey.Length >= 4
            ? request.ApiKey[^4..]
            : request.ApiKey;
        var encrypted = _encryption.Encrypt(request.ApiKey);

        var existing = await _db.AiProviderConfigs
            .FirstOrDefaultAsync(p => p.Provider == request.Provider && p.IsActive, cancellationToken);

        if (existing is null)
        {
            existing = AiProviderConfig.Create(
                request.Provider, encrypted, keyHint,
                _currentUser.UserId, request.BaseUrl, request.ModelDefault);
            _db.AiProviderConfigs.Add(existing);
        }
        else
        {
            existing.UpdateKey(encrypted, keyHint);
            existing.UpdateSettings(request.BaseUrl, request.ModelDefault);
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Run connectivity test after saving
        bool isHealthy;
        if (request.Provider == AiProvider.DeepL)
        {
            // DeepL health check: translate a test word
            var result = await _translationService.TranslateAsync("Test", "en", ["de"], cancellationToken);
            isHealthy = result.Count > 0;
        }
        else
        {
            isHealthy = await _aiService.TestProviderAsync(request.Provider, cancellationToken);
        }

        existing.SetHealthStatus(isHealthy);
        await _db.SaveChangesAsync(cancellationToken);

        return new AiProviderConfigDto
        {
            Id           = existing.Id,
            Provider     = existing.Provider,
            ProviderName = existing.Provider.ToString(),
            KeyHint      = $"****...{existing.KeyHint}",
            IsActive     = existing.IsActive,
            IsHealthy    = existing.IsHealthy,
            LastTestedAt = existing.LastTestedAt,
            BaseUrl      = existing.BaseUrl,
            ModelDefault = existing.ModelDefault,
            CreatedAt    = existing.CreatedAt,
        };
    }
}

