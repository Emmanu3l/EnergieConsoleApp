using EnergieConsoleApp.Application;
using EnergieConsoleApp.Domain;

namespace EnergieConsoleApp.Tests.Application;

public class TariffSwitchHandlerTests
{
    private readonly FakeCustomerRepository _customerRepo;
    private readonly FakeTariffRepository _tariffRepo;
    private readonly FakeRequestRepository _requestRepo;
    private readonly FakeProcessedRepository _processedRepo;
    private readonly TariffSwitchHandler _handler;

    public TariffSwitchHandlerTests()
    {
        // 1. Initialize our in-memory data stores
        _customerRepo = new FakeCustomerRepository();
        _tariffRepo = new FakeTariffRepository();
        _requestRepo = new FakeRequestRepository();
        _processedRepo = new FakeProcessedRepository();

        // 2. Inject them into the real handler
        _handler = new TariffSwitchHandler(_customerRepo, _tariffRepo, _requestRepo, _processedRepo);

        // 3. Seed Master Data
        _customerRepo.Customers = new Dictionary<string, Customer>
        {
            { "C_STANDARD", new Customer { CustomerId = "C_STANDARD", HasUnpaidInvoice = false, SLA = "Standard", MeterType = "Smart" } },
            { "C_PREMIUM", new Customer { CustomerId = "C_PREMIUM", HasUnpaidInvoice = false, SLA = "Premium", MeterType = "Smart" } },
            { "C_CLASSIC", new Customer { CustomerId = "C_CLASSIC", HasUnpaidInvoice = false, SLA = "Standard", MeterType = "Classic" } },
            { "C_UNPAID", new Customer { CustomerId = "C_UNPAID", HasUnpaidInvoice = true, SLA = "Standard", MeterType = "Smart" } }
        };

        _tariffRepo.Tariffs = new Dictionary<string, Tariff>
        {
            { "T_BASIC", new Tariff { TariffId = "T_BASIC", RequiresSmartMeter = false } },
            { "T_SMART", new Tariff { TariffId = "T_SMART", RequiresSmartMeter = true } }
        };
    }

    [Fact]
    public void Scenario1_StandardSLA_ShouldApproveWith48Hours()
    {
        // Arrange
        // Requested in Winter (Standard Time in Vienna: +01:00)
        _requestRepo.PendingRequests = new List<Request> {
            new Request { RequestId = "R1", CustomerId = "C_STANDARD", TargetTariffId = "T_BASIC", RequestedAt = new DateTime(2025, 01, 10, 10, 0, 0, DateTimeKind.Utc) }
        };

        // Act
        _handler.ProcessPendingRequests();

        // Assert
        var result = _processedRepo.SavedResults.Single();
        Assert.Equal("Approved", result.Status);
        Assert.Equal("2025-01-12T11:00:00+01:00", result.SlaDue); // 10:00 UTC = 11:00 Vienna -> +48h = Jan 12, 11:00
        Assert.Empty(result.FollowUpAction);
    }

    [Fact]
    public void Scenario2_PremiumSLA_ShouldApproveWith24Hours()
    {
        // Arrange
        // Requested in Summer (DST in Vienna: +02:00)
        _requestRepo.PendingRequests = new List<Request> {
            new Request { RequestId = "R2", CustomerId = "C_PREMIUM", TargetTariffId = "T_BASIC", RequestedAt = new DateTime(2025, 07, 10, 10, 0, 0, DateTimeKind.Utc) }
        };

        // Act
        _handler.ProcessPendingRequests();

        // Assert
        var result = _processedRepo.SavedResults.Single();
        Assert.Equal("Approved", result.Status);
        Assert.Equal("2025-07-11T12:00:00+02:00", result.SlaDue); // 10:00 UTC = 12:00 Vienna -> +24h = Jul 11, 12:00
    }

    [Fact]
    public void Scenario3_RequiresSmartMeter_CustomerHasClassic_ShouldScheduleUpgradeAndAdd12Hours()
    {
        // Arrange
        _requestRepo.PendingRequests = new List<Request> {
            new Request { RequestId = "R3", CustomerId = "C_CLASSIC", TargetTariffId = "T_SMART", RequestedAt = new DateTime(2025, 01, 10, 10, 0, 0, DateTimeKind.Utc) }
        };

        // Act
        _handler.ProcessPendingRequests();

        // Assert
        var result = _processedRepo.SavedResults.Single();
        Assert.Equal("Approved", result.Status);
        Assert.Equal("Schedule meter upgrade", result.FollowUpAction);
        // Standard (48h) + Smart Meter Penalty (12h) = 60 hours
        Assert.Equal("2025-01-12T23:00:00+01:00", result.SlaDue); 
    }

    [Fact]
    public void Scenario4_UnpaidInvoice_ShouldReject()
    {
        // Arrange
        _requestRepo.PendingRequests = new List<Request> {
            new Request { RequestId = "R4", CustomerId = "C_UNPAID", TargetTariffId = "T_BASIC", RequestedAt = DateTime.UtcNow }
        };

        // Act
        _handler.ProcessPendingRequests();

        // Assert
        var result = _processedRepo.SavedResults.Single();
        Assert.Equal("Rejected", result.Status);
        Assert.Equal("Unpaid invoice", result.Reason);
    }

    [Fact]
    public void Scenario5and6_UnknownCustomerOrTariff_ShouldReject()
    {
        // Arrange
        _requestRepo.PendingRequests = new List<Request> {
            new Request { RequestId = "R5", CustomerId = "UNKNOWN_CUST", TargetTariffId = "T_BASIC", RequestedAt = DateTime.UtcNow },
            new Request { RequestId = "R6", CustomerId = "C_STANDARD", TargetTariffId = "UNKNOWN_TARIFF", RequestedAt = DateTime.UtcNow }
        };

        // Act
        _handler.ProcessPendingRequests();

        // Assert
        Assert.Equal(2, _processedRepo.SavedResults.Count);
        Assert.Equal("Unknown customer", _processedRepo.SavedResults[0].Reason);
        Assert.Equal("Unknown tariff", _processedRepo.SavedResults[1].Reason);
    }
}

// --- Fake In-Memory Repositories for Testing ---

public class FakeCustomerRepository : ICustomerRepository
{
    public Dictionary<string, Customer> Customers { get; set; } = new();
    public Dictionary<string, Customer> GetCustomers() => Customers;
}

public class FakeTariffRepository : ITariffRepository
{
    public Dictionary<string, Tariff> Tariffs { get; set; } = new();
    public Dictionary<string, Tariff> GetTariffs() => Tariffs;
}

public class FakeRequestRepository : IRequestRepository
{
    public List<Request> PendingRequests { get; set; } = new();
    public IEnumerable<Request> GetPendingRequests(HashSet<string> processedIds) => PendingRequests;
}

public class FakeProcessedRepository : IProcessedRequestRepository
{
    public List<ProcessedRequest> SavedResults { get; } = new();
    
    public HashSet<string> GetProcessedRequestIds() => new HashSet<string>();

    public void SaveProcessedRequest(ProcessedRequest result)
    {
        SavedResults.Add(result);
    }
}
