# EnergieConsoleApp

Lightweight .NET console app that processes tariff-switch requests from CSV files, applies the business rules, calculates SLA due dates in `Europe/Vienna`, and persists processed results so requests are handled only once.

## Usage

```bash
dotnet run --project EnergieConsoleApp.csproj
dotnet test
```

Place the CSV files in `data/InputFiles/`, and check `data/OutputFiles/processed_requests.csv` after each run. New requests added to `requests.csv` are processed on the next run; already handled `RequestId`s are skipped.

## Notes

- `Standard` SLA = 48 hours.
- `Premium` SLA = 24 hours.
- Smart-meter requests add a follow-up action: `Schedule meter upgrade`.
- Invalid rows are rejected with a reason instead of stopping the run.
- The solution is split into `Application`, `Domain`, and `Infrastructure` to keep the business logic testable.