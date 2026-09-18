namespace LedgerLoop.Api.Domain;

public enum DocumentKind
{
    Invoice = 0,
    CreditNote = 1
}

public enum InvoiceStatus
{
    Draft = 0,
    Posted = 1
}

public enum PriceMode
{
    Exclusive = 0,
    Inclusive = 1
}

public enum LineCategory
{
    StandardGoods = 0,
    ProfessionalServices = 1,
    DigitalServices = 2,
    PrintedBooks = 3,
    MedicalSupplies = 4,
    FoodStaples = 5,
    Shipping = 6
}

public enum RateKind
{
    Standard = 0,
    Reduced = 1,
    SuperReduced = 2
}
