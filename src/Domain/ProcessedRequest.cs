namespace EnergieConsoleApp.Domain;

public class ProcessedRequest
{
    public string RequestId { get; init; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string SlaDue { get; set; } = string.Empty;
    public string FollowUpAction { get; set; } = string.Empty;
}