using FluentAssertions;
using LedgerLoop.Api.Tax;

namespace LedgerLoop.UnitTests;

public class MoneyTests
{
    [Theory]
    [InlineData(2.005, 2.01)]
    [InlineData(2.004, 2.00)]
    [InlineData(1.155, 1.16)]
    [InlineData(-1.155, -1.16)]
    [InlineData(-2.005, -2.01)]
    public void Rounds_half_amounts_away_from_zero(decimal value, decimal expected) =>
        Money.Round(value).Should().Be(expected);

    [Fact]
    public void Percent_keeps_full_precision_until_rounded() =>
        Money.Percent(55.00m, 2.1m).Should().Be(1.155m);
}
