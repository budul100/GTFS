# ![GTFS icon](https://raw.githubusercontent.com/budul100/GTFS/develop/icon.png) GTFS

[![NuGet](https://img.shields.io/nuget/v/budul.GTFS.svg)](https://www.nuget.org/packages/budul100.GTFS)
[![NuGet Downloads](https://img.shields.io/nuget/dt/budul.GTFS.svg)](https://www.nuget.org/packages/budul100.GTFS)

A .NET library for reading and writing [General Transit Feed Specification (GTFS)](https://gtfs.org/) feeds.

This is a fork of [Softwareentwicklung-Schittkowski-GmbH/GTFS](https://github.com/Softwareentwicklung-Schittkowski-GmbH/GTFS), which itself is a fork of [itinero/GTFS](https://github.com/itinero/GTFS). The goal of this fork is to modernize the technology stack and extend GTFS spec coverage, particularly for features missing from earlier versions.

## Features

- Read and write GTFS feeds from directories and ZIP archives
- Full support for core GTFS entities: agencies, stops, routes, trips, stop times, calendars, fares, shapes, frequencies, transfers, pathways, levels
- Strict and non-strict parsing modes
- Extensible reader and writer via subclassing
- In-memory feed model with index-based stop time lookups
- Feed validation and filtering utilities
- Compatible with .NET 8+

## Installation

```bash
dotnet add package GTFS
```

Or via the NuGet Package Manager:

```
Install-Package GTFS
```

## Quick Start

**Reading a feed from a directory or ZIP archive:**

```csharp
var reader = new GTFSReader<GTFSFeed>();
var feed = reader.Read("path/to/gtfs");   // directory or .zip
```

**Reading in strict mode** (throws on any spec violation):

```csharp
var reader = new GTFSReader<GTFSFeed>(strict: true);
var feed = reader.Read("path/to/gtfs");
```

**Accessing entities:**

```csharp
foreach (var route in feed.Routes)
{
    Console.WriteLine($"{route.Id}: {route.ShortName} – {route.LongName}");
}

var stopTimes = feed.StopTimes.GetForTrip("trip_id_here");
```

**Writing a feed:**

```csharp
var writer = new GTFSWriter<GTFSFeed>();
using var target = new GTFSDirectoryTarget("path/to/output");
writer.Write(feed, target);
```

**Configuring logging** (Microsoft.Extensions.Logging):

```csharp
Logger.UseLoggerFactory(loggerFactory);
```

## GTFS Spec Coverage

### Supported files

| File                  | Status                                     |
| --------------------- | ------------------------------------------ |
| `agency.txt`          | Supported                                  |
| `stops.txt`           | Supported                                  |
| `routes.txt`          | Supported                                  |
| `trips.txt`           | Supported                                  |
| `stop_times.txt`      | Supported                                  |
| `calendar.txt`        | Supported                                  |
| `calendar_dates.txt`  | Supported                                  |
| `fare_attributes.txt` | Supported                                  |
| `fare_rules.txt`      | Supported                                  |
| `shapes.txt`          | Supported                                  |
| `frequencies.txt`     | Supported                                  |
| `transfers.txt`       | Supported, including transfer_type 4 and 5 |
| `feed_info.txt`       | Supported                                  |
| `pathways.txt`        | Supported                                  |
| `levels.txt`          | Supported                                  |
| `booking_rules.txt`   | Not yet supported                          |
| `location_groups.txt` | Not yet supported                          |
| `locations.geojson`   | Not yet supported                          |

### Notable additions in this fork

- `TransferType.InSeat` (4) and `TransferType.InSeatNotAllowed` (5)
- `ContinuousPickup` and `ContinuousDropOff` on both `Route` and `StopTime`
- Index-based trip lookup in `StopTimeListCollection` (O(1) instead of O(n))
- `Microsoft.Extensions.Logging` integration, replacing the static log delegate
- Correct int values on `ExceptionType`, `PaymentMethodType` and `DirectionType` enums
- `DropOffType` members renamed to correct drop-off terminology (`NoDropOff`, `PhoneForDropOff`, `DriverForDropOff`); old names marked `[Obsolete]`
- `nullable enable` project-wide
- Strict/non-strict separation in time-of-day parsing; parse errors no longer silently produce `00:00:00`

## Breaking Changes

This fork introduces breaking changes relative to itinero/GTFS. If you are migrating:

- `DropOffType.NoPickup`, `PhoneForPickup` and `DriverForPickup` are `[Obsolete]`. Replace with `NoDropOff`, `PhoneForDropOff` and `DriverForDropOff`.
- `IEntityCollection.Get(string id)` now returns `TEntity?` instead of `TEntity`.
- `ReadTimeOfDay` no longer silently returns `00:00:00` on parse errors; in strict mode it throws, in non-strict mode it returns `null`.
- The static `Logger.LogAction` delegate is `[Obsolete]`. Use `Logger.UseLoggerFactory` instead.
- `TraceEventType` is `[Obsolete]`. Use `Microsoft.Extensions.Logging.LogLevel` instead.

## Requirements

- .NET 8 or later
- `Microsoft.Extensions.Logging.Abstractions` 8.0.0+

## Building

```bash
git clone https://github.com/budul100/GTFS
cd GTFS
dotnet build
dotnet test
```

## Contributing

Pull requests are welcome. For larger changes, please open an issue first to discuss the intended approach.

When adding support for new GTFS entities or fields, please include:

- The entity class or property with `[FieldName]` and `[Required]` attributes where applicable
- Reader support (`ParseField*` method and integration into the relevant `Parse*` method)
- Writer support (`WriteField*` method and integration into the relevant `Write` method)
- At least one test covering the happy path

## License

MIT License. See [LICENSE](LICENSE) for details.

Original work copyright (c) 2014 Ben Abelshausen.