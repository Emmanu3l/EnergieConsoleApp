namespace EnergieConsoleApp.Domain;

public class Tariff
{
    public string TariffId { get; init; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool RequiresSmartMeter { get; init; }
    public decimal MonthlyPrice { get; set; }
}