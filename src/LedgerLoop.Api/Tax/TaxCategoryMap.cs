using LedgerLoop.Api.Domain;

namespace LedgerLoop.Api.Tax;

public static class TaxCategoryMap
{
    private static readonly Dictionary<LineCategory, RateKind> Map = new()
    {
        [LineCategory.StandardGoods] = RateKind.Standard,
        [LineCategory.ProfessionalServices] = RateKind.Standard,
        [LineCategory.DigitalServices] = RateKind.Standard,
        [LineCategory.Shipping] = RateKind.Standard,
        [LineCategory.PrintedBooks] = RateKind.Reduced,
        [LineCategory.MedicalSupplies] = RateKind.Reduced,
        [LineCategory.FoodStaples] = RateKind.SuperReduced
    };

    public static RateKind RateKindFor(LineCategory category) =>
        Map.TryGetValue(category, out var kind) ? kind : RateKind.Standard;
}
