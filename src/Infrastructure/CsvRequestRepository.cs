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
        if (!File.Exists(_filePath)) throw new FileNotFoundException($"Missing: {_filePath}");

        var lines = File.ReadAllLines(_filePath);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var cols = lines[i].Split(';');
            var requestId = cols[0].Trim();

            // Idempotent processing: Skip already processed requests
            if (processedIds.Contains(requestId)) continue;

            yield return new Request
            {
                RequestId = requestId,
                CustomerId = cols[1].Trim(),
                TargetTariffId = cols[2].Trim(),
                // Parse the string and convert it straight to standard UTC DateTime
                RequestedAt = DateTimeOffset.Parse(cols[3].Trim(), null, DateTimeStyles.RoundtripKind).UtcDateTime
            };
        }
    }
}