namespace EnergieConsoleApp.Domain;

public class Customer
{
    public string CustomerId { get; init; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool HasUnpaidInvoice { get; init; }
    public string SLA { get; init; } = string.Empty;
    public string MeterType { get; init; } = string.Empty;
}