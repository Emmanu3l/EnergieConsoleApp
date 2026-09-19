using EnergieConsoleApp.Application;
using EnergieConsoleApp.Infrastructure;

try
{
    Console.WriteLine("Starting Tariff Switch Processor...");

    // Find the repository root regardless of Rider's working directory.
    var repositoryRoot = new DirectoryInfo(AppContext.BaseDirectory);
    while (repositoryRoot != null &&
           !File.Exists(Path.Combine(repositoryRoot.FullName, "EnergieConsoleApp.sln")))
    {
        repositoryRoot = repositoryRoot.Parent;
    }

    // Standalone builds use the lib folder alongside the executable.
    if (args.Length != 0 && (args.Length != 2 || args[0] != "--data-dir" || string.IsNullOrWhiteSpace(args[1])))
        throw new ArgumentException("Usage: EnergieConsoleApp [--data-dir <path>]");
    var dataRoot = args.Length == 2
        ? Path.GetFullPath(args[1])
        : Path.Combine(repositoryRoot?.FullName ?? AppContext.BaseDirectory, "lib");

    var inputFolder = Path.Combine(dataRoot, "InputFiles");
    var outputFolder = Path.Combine(dataRoot, "OutputFiles");
    Directory.CreateDirectory(outputFolder);
    var outputPath = Path.Combine(outputFolder, "processed_requests.csv");

    var customerRepo = new CsvCustomerRepository(Path.Combine(inputFolder, "customers.csv"));
    var tariffRepo = new CsvTariffRepository(Path.Combine(inputFolder, "tariffs.csv"));
    var requestRepo = new CsvRequestRepository(Path.Combine(inputFolder, "requests.csv"));
    var processedRepo = new CsvProcessedRequestRepository(outputPath);

    var handler = new TariffSwitchHandler(customerRepo, tariffRepo, requestRepo, processedRepo);

    handler.ProcessPendingRequests();

    Console.WriteLine($"Processing completed successfully. Results: {outputPath}");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"FATAL ERROR: {ex.Message}");
    Environment.Exit(1);
}
