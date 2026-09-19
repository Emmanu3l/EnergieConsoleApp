using EnergieConsoleApp.Application;
using EnergieConsoleApp.Infrastructure;

namespace EnergieConsoleApp.Tests.Infrastructure;

public sealed class CsvRepositoryTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "EnergieTests", Guid.NewGuid().ToString("N"));

    private string Write(string name, string contents)
    {
        Directory.CreateDirectory(_root);
        string path = Path.Combine(_root, name);
        File.WriteAllText(path, contents);
        return path;
    }

    [Fact]
    public void RealCsvProcessingSkipsDuplicatesAndPersistsAcrossRuns()
    {
        var customers = Write("customers.csv", "CustomerId;Name;HasUnpaidInvoice;SLA;MeterType\nC1;Anna;false;Standard;Smart\n");
        var tariffs = Write("tariffs.csv", "TariffId;Name;RequiresSmartMeter;BaseMonthlyGross\nT1;Basic;false;24.50\n");
        var requests = Write("requests.csv", "RequestId;CustomerId;TargetTariffId;RequestedAtISO8601\nR1;C1;T1;2025-03-29T13:00:00+01:00\nR1;C1;T1;2025-03-29T13:00:00+01:00\n");
        var output = Path.Combine(_root, "output", "processed.csv");
        void Run() => new TariffSwitchHandler(new CsvCustomerRepository(customers),
            new CsvTariffRepository(tariffs), new CsvRequestRepository(requests),
            new CsvProcessedRequestRepository(output)).ProcessPendingRequests();

        Run();
        var firstRun = File.ReadAllText(output);
        Run();
        Assert.Equal(firstRun, File.ReadAllText(output));
        Assert.Equal(2, File.ReadAllLines(output).Length);
        Assert.Contains("R1;Approved;;2025-03-31T14:00:00+02:00;", firstRun);
    }

    [Theory]
    [InlineData("C1;Anna;false;Standard")]
    [InlineData("C1;Anna;perhaps;Standard;Smart")]
    [InlineData("C1;Anna;false;Gold;Smart")]
    [InlineData("C1;Anna;false;Standard;Unknown")]
    [InlineData(";Anna;false;Standard;Smart")]
    [InlineData("C1;\"Anna\";false;Standard;Smart")]
    public void InvalidCustomerReportsFileAndLine(string row)
    {
        var path = Write("customers.csv", "CustomerId;Name;HasUnpaidInvoice;SLA;MeterType\n" + row);
        var error = Assert.Throws<FormatException>(() => new CsvCustomerRepository(path).GetCustomers());
        Assert.Contains(path, error.Message);
        Assert.Contains("line 2", error.Message);
    }

    [Fact]
    public void DuplicateMasterDataReportsLine()
    {
        var path = Write("customers.csv", "CustomerId;Name;HasUnpaidInvoice;SLA;MeterType\nC1;Anna;false;Standard;Smart\nC1;Bob;false;Standard;Smart");
        var error = Assert.Throws<FormatException>(() => new CsvCustomerRepository(path).GetCustomers());
        Assert.Contains("line 3", error.Message);
        Assert.Contains("Duplicate CustomerId", error.Message);
    }

    [Theory]
    [InlineData("2025-03-29T13:00:00")]
    [InlineData("not-a-date")]
    public void TimestampRequiresExplicitOffset(string timestamp)
    {
        var path = Write("requests.csv", "RequestId;CustomerId;TargetTariffId;RequestedAtISO8601\nR1;C1;T1;" + timestamp);
        var error = Assert.Throws<FormatException>(() => new CsvRequestRepository(path).GetPendingRequests([]).ToArray());
        Assert.Contains("line 2", error.Message);
        Assert.Contains("explicit offset", error.Message);
    }

    [Fact]
    public void InvalidTariffPriceReportsFileAndLine()
    {
        var path = Write("tariffs.csv", "TariffId;Name;RequiresSmartMeter;BaseMonthlyGross\nT1;Basic;false;invalid");
        var error = Assert.Throws<FormatException>(() => new CsvTariffRepository(path).GetTariffs());
        Assert.Contains(path, error.Message);
        Assert.Contains("line 2", error.Message);
    }

    [Fact]
    public void InvalidHeaderIsRejected()
    {
        var path = Write("customers.csv", "wrong;header\n");
        var error = Assert.Throws<FormatException>(() => new CsvCustomerRepository(path).GetCustomers());
        Assert.Contains("line 1", error.Message);
    }

    [Fact]
    public void MissingAndEmptyOutputHaveNoProcessedIds()
    {
        var path = Path.Combine(_root, "processed.csv");
        Assert.Empty(new CsvProcessedRequestRepository(path).GetProcessedRequestIds());
        Write("processed.csv", "");
        Assert.Empty(new CsvProcessedRequestRepository(path).GetProcessedRequestIds());
    }

    [Fact]
    public void TruncatedOutputIsRejectedInsteadOfSkippingRequest()
    {
        var path = Write("processed.csv", "RequestId;Status;Reason;SLADue;FollowUpAction\nR1;Approved");
        var error = Assert.Throws<FormatException>(() => new CsvProcessedRequestRepository(path).GetProcessedRequestIds());
        Assert.Contains("line 2", error.Message);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
