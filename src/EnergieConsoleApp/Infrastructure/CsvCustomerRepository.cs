using EnergieConsoleApp.Application;
using EnergieConsoleApp.Domain;

namespace EnergieConsoleApp.Infrastructure;

public class CsvCustomerRepository : ICustomerRepository
{
    private readonly string _filePath;

    public CsvCustomerRepository(string filePath) => _filePath = filePath;

    public Dictionary<string, Customer> GetCustomers()
    {
        var result = new Dictionary<string, Customer>();
        // Parse eagerly so invalid master data fails before any requests are saved.
        foreach (var item in CsvFile.Read(_filePath, "CustomerId;Name;HasUnpaidInvoice;SLA;MeterType", cols =>
        {
            var item = new Customer
            {
                CustomerId = CsvFile.Required(cols[0], "CustomerId"),
                Name = CsvFile.Required(cols[1], "Name"),
                HasUnpaidInvoice = bool.Parse(cols[2]),
                SLA = CsvFile.Choice(cols[3], "SLA", "Standard", "Premium"),
                MeterType = CsvFile.Choice(cols[4], "MeterType", "Classic", "Smart")
            };
            if (result.ContainsKey(item.CustomerId))
                throw new FormatException($"Duplicate CustomerId '{item.CustomerId}'.");
            return item;
        }))
            result.Add(item.CustomerId, item);
        return result;
    }
}
