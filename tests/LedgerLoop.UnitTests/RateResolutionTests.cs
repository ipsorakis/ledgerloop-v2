using FluentAssertions;
using LedgerLoop.Api.Domain;
using LedgerLoop.Api.Tax;

namespace LedgerLoop.UnitTests;

public class RateResolutionTests
{
    private readonly RateResolver _resolver = new(StaticRateTable.Instance);

    [Fact]
    public void Resolves_the_current_standard_rate() =>
        _resolver.Resolve("FR", LineCategory.ProfessionalServices, new DateOnly(2024, 5, 1)).Should().Be(20.0m);

    [Fact]
    public void Resolves_the_super_reduced_rate_for_food_staples() =>
        _resolver.Resolve("FR", LineCategory.FoodStaples, new DateOnly(2024, 5, 1)).Should().Be(2.1m);

    [Fact]
    public void Uses_the_reduced_rate_that_applied_to_older_periods() =>
        _resolver.Resolve("FR", LineCategory.PrintedBooks, new DateOnly(2022, 5, 9)).Should().Be(7.0m);

    [Fact]
    public void Uses_the_current_reduced_rate_from_the_revision_date_onwards() =>
        _resolver.Resolve("FR", LineCategory.PrintedBooks, RateHistory.ReducedRateRevision).Should().Be(10.0m);

    [Fact]
    public void Fails_loudly_for_a_country_without_a_schedule()
    {
        var act = () => _resolver.Resolve("PL", LineCategory.StandardGoods, new DateOnly(2024, 1, 1));
        act.Should().Throw<InvalidOperationException>();
    }
}
