using EnergieConsoleApp.Domain;

namespace EnergieConsoleApp.Application;

public class TariffSwitchHandler
{
    private readonly ICustomerRepository _customerRepo;
    private readonly ITariffRepository _tariffRepo;
    private readonly IRequestRepository _requestRepo;
    private readonly IProcessedRequestRepository _processedRepo;
    private readonly TimeZoneInfo _viennaTimeZone;

    public TariffSwitchHandler(
        ICustomerRepository customerRepo,
        ITariffRepository tariffRepo,
        IRequestRepository requestRepo,
        IProcessedRequestRepository processedRepo)
    {
        _customerRepo = customerRepo;
        _tariffRepo = tariffRepo;
        _requestRepo = requestRepo;
        _processedRepo = processedRepo;
        
        try
        {
            _viennaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Vienna");
        }
        catch (TimeZoneNotFoundException)
        {
            throw new InvalidOperationException("Timezone 'Europe/Vienna' could not be found.");
        }
    }

    public void ProcessPendingRequests()
    {
        var customers = _customerRepo.GetCustomers();
        var tariffs = _tariffRepo.GetTariffs();
        var processedIds = _processedRepo.GetProcessedRequestIds();

        var pendingRequests = _requestRepo.GetPendingRequests(processedIds);

        foreach (var request in pendingRequests)
        {
            var result = EvaluateRequest(request, customers, tariffs);
            _processedRepo.SaveProcessedRequest(result);
        }
    }

    private ProcessedRequest EvaluateRequest(Request req, Dictionary<string, Customer> customers, Dictionary<string, Tariff> tariffs)
    {
        var result = new ProcessedRequest { RequestId = req.RequestId, Status = "Rejected" };

        if (!customers.TryGetValue(req.CustomerId, out var customer))
        {
            result.Reason = "Unknown customer";
            return result;
        }

        if (!tariffs.TryGetValue(req.TargetTariffId, out var tariff))
        {
            result.Reason = "Unknown tariff";
            return result;
        }

        if (customer.HasUnpaidInvoice)
        {
            result.Reason = "Unpaid invoice";
            return result;
        }

        int slaHours = customer.SLA.Equals("Premium", StringComparison.OrdinalIgnoreCase) ? 24 : 48;
        bool needsSmartMeter = tariff.RequiresSmartMeter && customer.MeterType.Equals("Classic", StringComparison.OrdinalIgnoreCase);

        if (needsSmartMeter)
        {
            result.FollowUpAction = "Schedule meter upgrade";
            slaHours += 12;
        }

        result.Status = "Approved";
        result.SlaDue = CalculateSlaDue(req.RequestedAt, slaHours);
        
        return result;
    }

    private string CalculateSlaDue(DateTime requestedAtUtc, int hoursToAdd)
    {
        // Convert the UTC input to Local Vienna Time
        DateTime localViennaTime = TimeZoneInfo.ConvertTimeFromUtc(requestedAtUtc, _viennaTimeZone);
        
        // Add hours to the local clock (safe against DST shifts)
        DateTime localDue = localViennaTime.AddHours(hoursToAdd);
        
        // Attach the correct UTC offset for the final target date
        TimeSpan offset = _viennaTimeZone.GetUtcOffset(localDue);
        DateTimeOffset finalSlaDue = new DateTimeOffset(localDue, offset);
        
        return finalSlaDue.ToString("yyyy-MM-ddTHH:mm:sszzz");
    }
}
