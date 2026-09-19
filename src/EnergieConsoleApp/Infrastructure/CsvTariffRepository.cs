using System.Globalization;
using EnergieConsoleApp.Application;
using EnergieConsoleApp.Domain;

namespace EnergieConsoleApp.Infrastructure;

public class CsvTariffRepository : ITariffRepository
{
    private readonly string _filePath;

    public CsvTariffRepository(string filePath) => _filePath = filePath;

    public Dictionary<string, Tariff> GetTariffs()
    {
        var result = new Dictionary<string, Tariff>();
        // Parse eagerly so invalid master data fails before any requests are saved.
        foreach (var item in CsvFile.Read(_filePath, "TariffId;Name;RequiresSmartMeter;BaseMonthlyGross", cols =>
        {
            var item = new Tariff
            {
                TariffId = CsvFile.Required(cols[0], "TariffId"),
                Name = CsvFile.Required(cols[1], "Name"),
                RequiresSmartMeter = bool.Parse(cols[2]),
                MonthlyPrice = decimal.Parse(cols[3], CultureInfo.InvariantCulture)
            };
            if (result.ContainsKey(item.TariffId))
                throw new FormatException($"Duplicate TariffId '{item.TariffId}'.");
            return item;
        }))
            result.Add(item.TariffId, item);
        return result;
    }
}
