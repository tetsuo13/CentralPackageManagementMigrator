# Central Package Management Migrator

[![Continuous integration](https://github.com/tetsuo13/AspNetCore.DataProtection.MySql/actions/workflows/ci.yml/badge.svg)](https://github.com/tetsuo13/AspNetCore.DataProtection.MySql/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![CentralPackageManagementMigrator](https://img.shields.io/nuget/v/CentralPackageManagementMigrator.svg)](https://www.nuget.org/packages/CentralPackageManagementMigrator/)

.NET tool that migrates a solution to use NuGet [central package management](https://learn.microsoft.com/en-us/nuget/consume-packages/Central-Package-Management) (CPM).

## Quick Start

Install the tool globally using the following command:

```
dotnet tool install --global CentralPackageManagementMigrator
```

Then change directory to where the solution file is and run the tool with:

```
centralpackagemanagementmigrator
```

This will examine all project files recursively, modify them to remove package version references, and create a `Directory.packages.props` file in the current directory in which the tool was invoked from.

### Options

```
Description:
  Migrates a codebase to use NuGet central package management (CPM)

Usage:
  centralpackagemanagement-migrator [options]

Options:
  -v, --verbosity <Critical|Debug|Error|Information|None|Trace|Warning>  Verbosity level of the console logging output. [default: Information]
  --version                                                              Show version information
  -?, -h, --help                                                         Show help and usage information
```

