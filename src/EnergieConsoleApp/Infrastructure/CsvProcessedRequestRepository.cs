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

        if (new FileInfo(_filePath).Length == 0) return processed;
        foreach (var id in CsvFile.Read(_filePath, "RequestId;Status;Reason;SLADue;FollowUpAction",
                     cols => CsvFile.Required(cols[0], "RequestId")))
            processed.Add(id);
        return processed;
    }

    public void SaveProcessedRequest(ProcessedRequest result)
    {
        string[] fields = [result.RequestId, result.Status, result.Reason, result.SlaDue, result.FollowUpAction];
        if (fields.Any(value => value.IndexOfAny([';', '"', '\r', '\n']) >= 0))
            throw new FormatException("Output fields cannot contain semicolons, quotes, or newlines.");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_filePath))!);
        bool writeHeader = !File.Exists(_filePath) || new FileInfo(_filePath).Length == 0;
        using var writer = new StreamWriter(_filePath, append: true);
        
        if (writeHeader)
            writer.WriteLine("RequestId;Status;Reason;SLADue;FollowUpAction");

        // Write out using semicolons to match input files
        writer.WriteLine($"{result.RequestId};{result.Status};{result.Reason};{result.SlaDue};{result.FollowUpAction}");
    }
}