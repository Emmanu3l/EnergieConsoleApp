# EnergieConsoleApp

A .NET 10 console application that evaluates customer tariff-switch requests from CSV files and writes approval or rejection results with SLA deadlines in the `Europe/Vienna` time zone.

## Build and run

Requires the **.NET 10 SDK** and system time-zone data for `Europe/Vienna`. Run these commands from the repository root:

```sh
dotnet build EnergieConsoleApp.sln
dotnet run --project src/EnergieConsoleApp/EnergieConsoleApp.csproj
dotnet test tests/EnergieConsoleApp.Tests/EnergieConsoleApp.Tests.csproj
```

The sample input contains six requests. On a fresh run, the application writes four approvals and two rejections to `lib/OutputFiles/processed_requests.csv`. It prints the full output path when processing completes.

Subsequent runs skip request IDs already recorded in that file. To recompute the sample results, delete that generated file and run again. Existing results are not recalculated when business rules or input data change.

## Structure

```text
EnergieConsoleApp.sln
lib/
├── InputFiles/
│   ├── customers.csv
│   ├── tariffs.csv
│   └── requests.csv
└── OutputFiles/                    # Created at runtime; generated CSVs are ignored
src/EnergieConsoleApp/
├── Domain/                        # Customer, tariff, request, and result models
├── Application/                   # Business rules and repository interfaces
├── Infrastructure/                # CSV parsing and persistence
└── Program.cs                     # Data paths and dependency composition
tests/EnergieConsoleApp.Tests/
├── Application/                   # Business-rule tests using in-memory fakes
└── Infrastructure/                # CSV validation and file-based integration tests
```

The application uses layers and repository interfaces to separate business rules from file access. Constructor injection lets the handler use either CSV repositories or in-memory test fakes. A single application project keeps this small exercise straightforward; layer boundaries are conventions rather than separate assembly dependencies.

## Business rules

Requests are checked in this order:

1. Unknown customer: reject with `Unknown customer`.
2. Unknown target tariff: reject with `Unknown tariff`.
3. Customer has an unpaid invoice: reject with `Unpaid invoice`.
4. Otherwise approve. Standard customers receive a 48-hour SLA; Premium customers receive 24 hours.
5. If the target tariff requires a smart meter and the customer has a Classic meter, add 12 hours and set `FollowUpAction` to `Schedule meter upgrade`.

Rejected requests have no SLA deadline or follow-up action. They are recorded and skipped on subsequent runs, just like approvals.

SLA hours mean **elapsed hours**. The application normalizes the request timestamp to UTC, adds the duration, then converts the deadline to Vienna time. This preserves the duration across spring and autumn daylight-saving transitions. Deadlines use ISO 8601 with the applicable UTC offset.

## Data location

By default, the application searches upward from its executable directory for `EnergieConsoleApp.sln` and uses the `lib/` folder beside it. This works with the usual Rider and `dotnet run` build locations regardless of the working directory. Outside the repository, it uses `lib/` alongside the executable. Only input files are copied into build output; processing results are runtime data.

Override the location explicitly when needed:

```sh
dotnet run --project src/EnergieConsoleApp/EnergieConsoleApp.csproj -- --data-dir /absolute/path/to/data
```

The chosen directory must contain `InputFiles/customers.csv`, `InputFiles/tariffs.csv`, and `InputFiles/requests.csv`. Results go into `OutputFiles/processed_requests.csv` under the same directory. Relative paths supplied to `--data-dir` are resolved against the current working directory. The output directory is created automatically.

## CSV format

Files use UTF-8, a required header, and semicolon-separated fields. Headers must match the examples below. Surrounding field whitespace is trimmed, and blank data lines are ignored. Quoted fields, embedded semicolons, and multiline fields are not supported.

Customer and tariff IDs must be unique within their master-data files. IDs are case-sensitive. Required input fields cannot be blank. Booleans use `true` or `false`; SLA values are `Standard` or `Premium`, and meter types are `Classic` or `Smart` (these values are case-insensitive). Prices use an invariant-culture decimal point.

`customers.csv`:

```csv
CustomerId;Name;HasUnpaidInvoice;SLA;MeterType
C001;Anna Maier;false;Premium;Smart
```

`tariffs.csv`:

```csv
TariffId;Name;RequiresSmartMeter;BaseMonthlyGross
T-ECO;ÖkoStrom;true;29.90
```

`requests.csv`:

```csv
RequestId;CustomerId;TargetTariffId;RequestedAtISO8601
R1001;C001;T-ECO;2025-03-30T01:15:00+01:00
```

Request timestamps must include seconds and either `Z` or an explicit `±HH:mm` offset; up to seven fractional-second digits are accepted. Offset-free timestamps are rejected to avoid depending on the machine's local time zone.

Corresponding output:

```csv
RequestId;Status;Reason;SLADue;FollowUpAction
R1001;Approved;;2025-03-31T02:15:00+02:00;
```

Malformed CSV data produces an error with the file path and line number. File-access failures and validation errors stop processing with a nonzero exit code. Invalid master data is detected before processing requests. Requests are read sequentially, so results saved before a later malformed request remain in the output file.

## Repeat execution and limitations

The output file is the record of processed request IDs. Duplicate request IDs within one input file are skipped after the first result is saved, and later runs skip IDs already recorded on disk. The first successfully saved occurrence wins, even if a later row with the same ID contains different data.

This is intended for one process at a time. CSV appends provide neither transactional crash recovery nor coordination between concurrent runs, so this is not an exactly-once guarantee. A truncated output row is reported as invalid data and needs inspection before processing can resume. Deleting the output file discards processing history.

The parser deliberately supports the restricted format above rather than general-purpose CSV. Larger or less controlled inputs would justify a CSV library, transactional storage, and a defined recovery strategy.

## Tests

The suite covers:

- Standard and Premium SLAs, meter-upgrade extensions, and rejection rules.
- Elapsed-hour SLA calculations across both daylight-saving transitions.
- Duplicate requests within one run and across repeated runs.
- Real CSV input/output with temporary files and output-directory creation.
- Invalid headers, column counts, values, required fields, quoting, duplicate customer IDs, and offset-free timestamps.
- Missing, empty, and truncated processed-results files.
