using System.Globalization;
using EnergieConsoleApp.Application;
using EnergieConsoleApp.Domain;

namespace EnergieConsoleApp.Infrastructure;

public class CsvRequestRepository : IRequestRepository
{
    private readonly string _filePath;

    public CsvRequestRepository(string filePath) => _filePath = filePath;

    public IEnumerable<Request> GetPendingRequests(HashSet<string> processedIds)
    {
        var requests = CsvFile.Read(_filePath, "RequestId;CustomerId;TargetTariffId;RequestedAtISO8601", cols =>
        {
            string[] formats = ["yyyy-MM-dd'T'HH:mm:sszzz", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz",
                "yyyy-MM-dd'T'HH:mm:ss'Z'", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"];
            if (!DateTimeOffset.TryParseExact(cols[3], formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out var requestedAt))
                throw new FormatException("RequestedAtISO8601 must be an ISO 8601 timestamp with Z or an explicit offset.");
            return new Request
            {
                RequestId = CsvFile.Required(cols[0], "RequestId"),
                CustomerId = CsvFile.Required(cols[1], "CustomerId"),
                TargetTariffId = CsvFile.Required(cols[2], "TargetTariffId"),
                RequestedAt = requestedAt.UtcDateTime
            };
        });
        foreach (var request in requests)
            if (!processedIds.Contains(request.RequestId)) yield return request;
    }
}
