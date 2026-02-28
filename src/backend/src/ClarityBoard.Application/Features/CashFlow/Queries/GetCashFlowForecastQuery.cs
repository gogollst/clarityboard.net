using ClarityBoard.Application.Features.CashFlow.DTOs;
using ClarityBoard.Application.Features.CashFlow.Services;
using MediatR;

namespace ClarityBoard.Application.Features.CashFlow.Queries;

public record GetCashFlowForecastQuery : IRequest<CashFlowForecastDto>
{
    public Guid EntityId { get; init; }
    public DateOnly? StartDate { get; init; }
}

public class GetCashFlowForecastQueryHandler : IRequestHandler<GetCashFlowForecastQuery, CashFlowForecastDto>
{
    private readonly ICashFlowForecastService _forecastService;

    public GetCashFlowForecastQueryHandler(ICashFlowForecastService forecastService)
    {
        _forecastService = forecastService;
    }

    public async Task<CashFlowForecastDto> Handle(
        GetCashFlowForecastQuery request, CancellationToken cancellationToken)
    {
        var startDate = request.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return await _forecastService.GenerateForecastAsync(request.EntityId, startDate, cancellationToken);
    }
}
