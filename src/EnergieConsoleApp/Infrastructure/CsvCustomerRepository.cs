using EnergieConsoleApp.Application;
using EnergieConsoleApp.Domain;

namespace EnergieConsoleApp.Infrastructure;

public class CsvCustomerRepository : ICustomerRepository
{
    private readonly string _filePath;

    public CsvCustomerRepository(string filePath) => _filePath = filePath;

    public Dictionary<string, Customer> GetCustomers()
    {
        var dict = new Dictionary<string, Customer>();
        if (!File.Exists(_filePath)) throw new FileNotFoundException($"Missing: {_filePath}");

        var lines = File.ReadAllLines(_filePath);
        for (int i = 1; i < lines.Length; i++) // Skip header
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var cols = lines[i].Split(';'); // Using semicolon

            var customer = new Customer
            {
                CustomerId = cols[0].Trim(),
                Name = cols[1].Trim(),
                HasUnpaidInvoice = bool.Parse(cols[2].Trim()),
                SLA = cols[3].Trim(),
                MeterType = cols[4].Trim()
            };
            dict[customer.CustomerId] = customer;
        }
        return dict;
    }
}