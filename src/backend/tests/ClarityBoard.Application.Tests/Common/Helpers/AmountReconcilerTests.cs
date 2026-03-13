using ClarityBoard.Application.Common.Helpers;

namespace ClarityBoard.Application.Tests.Common.Helpers;

public class AmountReconcilerTests
{
    [Fact]
    public void AllThreeCorrect_ReturnsUnchanged_NoMismatch()
    {
        var result = AmountReconciler.Reconcile(119m, 100m, 19m);

        Assert.Equal(119m, result.GrossAmount);
        Assert.Equal(100m, result.NetAmount);
        Assert.Equal(19m, result.TaxAmount);
        Assert.False(result.PlausibilityMismatch);
        Assert.Null(result.MismatchDetail);
    }

    [Fact]
    public void GrossAndTax_CalculatesNet()
    {
        var result = AmountReconciler.Reconcile(119m, null, 19m);

        Assert.Equal(119m, result.GrossAmount);
        Assert.Equal(100m, result.NetAmount);
        Assert.Equal(19m, result.TaxAmount);
        Assert.False(result.PlausibilityMismatch);
    }

    [Fact]
    public void NetAndTax_CalculatesGross()
    {
        var result = AmountReconciler.Reconcile(null, 100m, 19m);

        Assert.Equal(119m, result.GrossAmount);
        Assert.Equal(100m, result.NetAmount);
        Assert.Equal(19m, result.TaxAmount);
        Assert.False(result.PlausibilityMismatch);
    }

    [Fact]
    public void GrossAndNet_CalculatesTax()
    {
        var result = AmountReconciler.Reconcile(119m, 100m, null);

        Assert.Equal(119m, result.GrossAmount);
        Assert.Equal(100m, result.NetAmount);
        Assert.Equal(19m, result.TaxAmount);
        Assert.False(result.PlausibilityMismatch);
    }

    [Fact]
    public void Contradictory_ReturnsMismatch()
    {
        var result = AmountReconciler.Reconcile(119m, 100m, 25m);

        Assert.Equal(119m, result.GrossAmount);
        Assert.Equal(100m, result.NetAmount);
        Assert.Equal(25m, result.TaxAmount);
        Assert.True(result.PlausibilityMismatch);
        Assert.NotNull(result.MismatchDetail);
        Assert.Contains("119", result.MismatchDetail);
    }

    [Fact]
    public void OnlyGross_LeavesOthersNull()
    {
        var result = AmountReconciler.Reconcile(119m, null, null);

        Assert.Equal(119m, result.GrossAmount);
        Assert.Null(result.NetAmount);
        Assert.Null(result.TaxAmount);
        Assert.False(result.PlausibilityMismatch);
    }

    [Fact]
    public void AllNull_ReturnsAllNull()
    {
        var result = AmountReconciler.Reconcile(null, null, null);

        Assert.Null(result.GrossAmount);
        Assert.Null(result.NetAmount);
        Assert.Null(result.TaxAmount);
        Assert.False(result.PlausibilityMismatch);
    }

    [Fact]
    public void RoundingTolerance_NoMismatch()
    {
        // Diff = 119.00 - (99.99 + 19.00) = 0.01, within ±0.02 tolerance
        var result = AmountReconciler.Reconcile(119.00m, 99.99m, 19.00m);

        Assert.Equal(119.00m, result.GrossAmount);
        Assert.Equal(99.99m, result.NetAmount);
        Assert.Equal(19.00m, result.TaxAmount);
        Assert.False(result.PlausibilityMismatch);
    }
}
