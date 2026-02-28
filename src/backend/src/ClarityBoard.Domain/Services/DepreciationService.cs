using ClarityBoard.Domain.Entities.Asset;

namespace ClarityBoard.Domain.Services;

/// <summary>
/// Calculates depreciation schedules using straight-line and declining-balance methods.
/// Supports pro-rata depreciation for the first year based on the in-service date.
/// </summary>
public sealed class DepreciationService : IDepreciationService
{
    public IReadOnlyList<DepreciationSchedule> CalculateSchedule(FixedAsset asset)
    {
        return asset.DepreciationMethod switch
        {
            "straight_line" => CalculateStraightLine(asset),
            "declining_balance" => CalculateDecliningBalance(asset),
            _ => throw new InvalidOperationException(
                $"Unsupported depreciation method: '{asset.DepreciationMethod}'.")
        };
    }

    /// <summary>
    /// Straight-line depreciation: (AcquisitionCost - ResidualValue) / UsefulLifeMonths per month.
    /// First year is pro-rata based on InServiceDate.
    /// </summary>
    private static IReadOnlyList<DepreciationSchedule> CalculateStraightLine(FixedAsset asset)
    {
        var schedules = new List<DepreciationSchedule>();

        var depreciableAmount = asset.AcquisitionCost - asset.ResidualValue;
        var monthlyAmount = depreciableAmount / asset.UsefulLifeMonths;

        var startDate = asset.InServiceDate ?? asset.AcquisitionDate;
        var accumulated = 0m;
        var remainingMonths = asset.UsefulLifeMonths;

        // Pro-rata first month: if not starting on the 1st, calculate partial month
        var currentDate = new DateOnly(startDate.Year, startDate.Month, 1);

        // If the asset starts mid-month, calculate a partial first month
        if (startDate.Day > 1)
        {
            var daysInMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);
            var remainingDays = daysInMonth - startDate.Day + 1;
            var firstMonthAmount = monthlyAmount * remainingDays / daysInMonth;
            firstMonthAmount = Math.Round(firstMonthAmount, 2);

            accumulated += firstMonthAmount;
            var bookValue = asset.AcquisitionCost - accumulated;

            schedules.Add(DepreciationSchedule.Create(
                asset.Id, currentDate, firstMonthAmount, accumulated, bookValue));

            currentDate = currentDate.AddMonths(1);
            remainingMonths--;
        }

        // Full months
        for (var i = 0; i < remainingMonths; i++)
        {
            var amount = monthlyAmount;

            // Ensure we don't depreciate below residual value
            if (accumulated + amount > depreciableAmount)
                amount = depreciableAmount - accumulated;

            if (amount <= 0)
                break;

            amount = Math.Round(amount, 2);
            accumulated += amount;
            var bookValue = asset.AcquisitionCost - accumulated;

            // Ensure book value doesn't go below residual
            if (bookValue < asset.ResidualValue)
            {
                amount -= (asset.ResidualValue - bookValue);
                amount = Math.Round(amount, 2);
                accumulated = depreciableAmount;
                bookValue = asset.ResidualValue;
            }

            schedules.Add(DepreciationSchedule.Create(
                asset.Id, currentDate, amount, accumulated, bookValue));

            currentDate = currentDate.AddMonths(1);
        }

        return schedules;
    }

    /// <summary>
    /// Declining balance depreciation: (2 / UsefulLifeMonths * 12) * remaining book value per month.
    /// Switches to straight-line when straight-line produces a higher depreciation amount.
    /// </summary>
    private static IReadOnlyList<DepreciationSchedule> CalculateDecliningBalance(FixedAsset asset)
    {
        var schedules = new List<DepreciationSchedule>();

        var usefulLifeYears = asset.UsefulLifeMonths / 12.0m;
        var decliningRate = 2m / usefulLifeYears / 12m; // Monthly declining rate

        var startDate = asset.InServiceDate ?? asset.AcquisitionDate;
        var currentDate = new DateOnly(startDate.Year, startDate.Month, 1);
        var accumulated = 0m;
        var bookValue = asset.AcquisitionCost;

        for (var month = 0; month < asset.UsefulLifeMonths; month++)
        {
            if (bookValue <= asset.ResidualValue)
                break;

            // Declining balance amount
            var decliningAmount = bookValue * decliningRate;

            // Straight-line amount for remaining life
            var remainingMonths = asset.UsefulLifeMonths - month;
            var straightLineAmount = remainingMonths > 0
                ? (bookValue - asset.ResidualValue) / remainingMonths
                : 0m;

            // Use the higher of declining or straight-line (switch point)
            var amount = Math.Max(decliningAmount, straightLineAmount);

            // Don't go below residual value
            if (bookValue - amount < asset.ResidualValue)
                amount = bookValue - asset.ResidualValue;

            amount = Math.Round(amount, 2);

            if (amount <= 0)
                break;

            accumulated += amount;
            bookValue -= amount;

            schedules.Add(DepreciationSchedule.Create(
                asset.Id, currentDate, amount, accumulated, bookValue));

            currentDate = currentDate.AddMonths(1);
        }

        return schedules;
    }
}
