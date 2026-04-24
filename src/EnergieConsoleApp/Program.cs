using EnergieConsoleApp.Application;
using EnergieConsoleApp.Infrastructure;

try
{
    Console.WriteLine("Starting Tariff Switch Processor...");

    var dataRoot = Path.Combine(AppContext.BaseDirectory, "lib");

    var inputFolder = Path.Combine(dataRoot, "InputFiles");
    var outputFolder = Path.Combine(dataRoot, "OutputFiles");

    var customerRepo = new CsvCustomerRepository(Path.Combine(inputFolder, "customers.csv"));
    var tariffRepo = new CsvTariffRepository(Path.Combine(inputFolder, "tariffs.csv"));
    var requestRepo = new CsvRequestRepository(Path.Combine(inputFolder, "requests.csv"));
    var processedRepo = new CsvProcessedRequestRepository(Path.Combine(outputFolder, "processed_requests.csv"));

    var handler = new TariffSwitchHandler(customerRepo, tariffRepo, requestRepo, processedRepo);

    handler.ProcessPendingRequests();

    Console.WriteLine("Processing completed successfully. Check processed_requests.csv for results.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"FATAL ERROR: {ex.Message}");
    Environment.Exit(1);
}