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
        var dict = new Dictionary<string, Tariff>();
        if (!File.Exists(_filePath)) throw new FileNotFoundException($"Missing: {_filePath}");

        var lines = File.ReadAllLines(_filePath);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var cols = lines[i].Split(';');

            var tariff = new Tariff
            {
                TariffId = cols[0].Trim(),
                Name = cols[1].Trim(),
                RequiresSmartMeter = bool.Parse(cols[2].Trim()),
                MonthlyPrice = decimal.Parse(cols[3].Trim(), CultureInfo.InvariantCulture)
            };
            dict[tariff.TariffId] = tariff;
        }
        return dict;
    }
}