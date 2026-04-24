using EnergieConsoleApp.Domain;

namespace EnergieConsoleApp.Application;

public interface ICustomerRepository
{
    Dictionary<string, Customer> GetCustomers();
}

public interface ITariffRepository
{
    Dictionary<string, Tariff> GetTariffs();
}

public interface IRequestRepository
{
    IEnumerable<Request> GetPendingRequests(HashSet<string> processedIds);
}

public interface IProcessedRequestRepository
{
    HashSet<string> GetProcessedRequestIds();
    void SaveProcessedRequest(ProcessedRequest result);
}