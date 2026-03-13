using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.AI.DTOs;
using ClarityBoard.Domain.Entities.AI;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.AI.Commands;

public record TestAiProviderCommand : IRequest<ProviderTestResultDto>
{
    public AiProvider Provider { get; init; }
}

public class TestAiProviderCommandHandler
    : IRequestHandler<TestAiProviderCommand, ProviderTestResultDto>
{
    private readonly IAppDbContext _db;
    private readonly IPromptAiService _aiService;
    private readonly ITranslationService _translationService;
    private readonly IAzureDocIntelligenceService _azureDocIntelligence;

    public TestAiProviderCommandHandler(
        IAppDbContext db,
        IPromptAiService aiService,
        ITranslationService translationService,
        IAzureDocIntelligenceService azureDocIntelligence)
    {
        _db        = db;
        _aiService = aiService;
        _translationService = translationService;
        _azureDocIntelligence = azureDocIntelligence;
    }

    public async Task<ProviderTestResultDto> Handle(
        TestAiProviderCommand request, CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        string? error = null;
        bool healthy;

        try
        {
            if (request.Provider == AiProvider.DeepL)
            {
                var result = await _translationService.TranslateAsync("Test", "en", ["de"], cancellationToken);
                healthy = result.Count > 0;
            }
            else if (request.Provider == AiProvider.AzureDocIntelligence)
            {
                healthy = await _azureDocIntelligence.TestConnectivityAsync(cancellationToken);
            }
            else
            {
                healthy = await _aiService.TestProviderAsync(request.Provider, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            healthy = false;
            error   = ex.Message;
        }

        sw.Stop();

        // Persist health status
        var config = await _db.AiProviderConfigs
            .FirstOrDefaultAsync(p => p.Provider == request.Provider && p.IsActive, cancellationToken);

        if (config is not null)
        {
            config.SetHealthStatus(healthy);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new ProviderTestResultDto
        {
            Provider     = request.Provider,
            IsHealthy    = healthy,
            DurationMs   = (int)sw.ElapsedMilliseconds,
            ErrorMessage = error,
        };
    }
}

