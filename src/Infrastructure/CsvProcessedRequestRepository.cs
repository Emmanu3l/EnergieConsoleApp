using EnergieConsoleApp.Application;
using EnergieConsoleApp.Domain;

namespace EnergieConsoleApp.Infrastructure;

public class CsvProcessedRequestRepository : IProcessedRequestRepository
{
    private readonly string _filePath;

    public CsvProcessedRequestRepository(string filePath) => _filePath = filePath;

    public HashSet<string> GetProcessedRequestIds()
    {
        var processed = new HashSet<string>();
        if (!File.Exists(_filePath)) return processed;

        var lines = File.ReadAllLines(_filePath);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            // Handle both delimiters just in case previous runs used commas
            var cols = lines[i].Contains(';') ? lines[i].Split(';') : lines[i].Split(',');
            processed.Add(cols[0].Trim());
        }
        return processed;
    }

    public void SaveProcessedRequest(ProcessedRequest result)
    {
        bool writeHeader = !File.Exists(_filePath) || new FileInfo(_filePath).Length == 0;
        using var writer = new StreamWriter(_filePath, append: true);
        
        if (writeHeader)
            writer.WriteLine("RequestId;Status;Reason;SLADue;FollowUpAction");

        // Write out using semicolons to match input files
        writer.WriteLine($"{result.RequestId};{result.Status};{result.Reason};{result.SlaDue};{result.FollowUpAction}");
    }
}