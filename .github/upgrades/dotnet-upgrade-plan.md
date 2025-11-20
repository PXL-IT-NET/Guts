# .NET 8.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that an .NET 8.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 8.0 upgrade.
3. Upgrade `Guts.Client.Core\Guts.Client.Core.csproj`
4. Upgrade `Guts.Client.WPF\Guts.Client.WPF.csproj`
5. Upgrade `Dummy.Tests\Dummy.Tests.csproj`
6. Upgrade `Guts.Client.Core.Tests\Guts.Client.Core.Tests.csproj`

## Settings

This section contains settings and data used by execution steps.

### Excluded projects

Table below contains projects that do belong to the dependency graph for selected projects and should not be included in the upgrade.

| Project name                                   | Description                 |
|:-----------------------------------------------|:---------------------------:|


### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                                | Current Version | New Version | Description                                   |
|:--------------------------------------------|:---------------:|:-----------:|:----------------------------------------------|
| Microsoft.Extensions.Configuration.Json     |     8.0.0       |   8.0.1     | Replace with newer patch release (recommended)|
| Microsoft.Extensions.Logging.Debug          |     8.0.0       |   8.0.1     | Replace with newer patch release (recommended)|

### Project upgrade details
This section contains details about each project upgrade and modifications that need to be done in the project.

#### Guts.Client.Core modifications

Project properties changes:
  - Target framework should be changed from `net6.0` to `net8.0`

NuGet packages changes:
  - `Microsoft.Extensions.Configuration.Json` should be updated from `8.0.0` to `8.0.1` (*recommended patch update*)
  - `Microsoft.Extensions.Logging.Debug` should be updated from `8.0.0` to `8.0.1` (*recommended patch update*)

Other changes:
  - Review for any API breaking changes related to .NET 8 and package updates; adjust code if compilation errors appear.

#### Guts.Client.WPF modifications

Project properties changes:
  - Target framework should be changed from `net6.0-windows` to `net8.0-windows`

Other changes:
  - Verify WPF runtime APIs and package compatibility for .NET 8; update any platform-specific references if needed.

#### Dummy.Tests modifications

Project properties changes:
  - Target framework should be changed from `net6.0` to `net8.0`

Other changes:
  - Ensure test framework and test runner are compatible with .NET 8.

#### Guts.Client.Core.Tests modifications

Project properties changes:
  - Target framework should be changed from `net6.0` to `net8.0`

Other changes:
  - Ensure NUnit version used is compatible with .NET 8 and adjust test project packages if needed.

