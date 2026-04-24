namespace EnergieConsoleApp.Domain;

public class Request
{
    public string RequestId { get; init; } = string.Empty;
    public string CustomerId { get; init; } = string.Empty;
    public string TargetTariffId { get; init; } = string.Empty;
    public DateTime RequestedAt { get; init; }
}