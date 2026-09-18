namespace LedgerLoop.Api.Tax;

public static class Money
{
    public const int Scale = 2;

    public static decimal Round(decimal value) => Math.Round(value, Scale, MidpointRounding.AwayFromZero);

    public static decimal Percent(decimal amount, decimal percent) => amount * percent / 100m;
}
