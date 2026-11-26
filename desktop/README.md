# Company Brain desktop

Avalonia UI 12.2.0, .NET 11, C# and CommunityToolkit.Mvvm.
The screens use sample data; their API integrations are tracked in #48–51.

## Build and run

From `desktop/` in PowerShell:

```powershell
.\dotnet.ps1 restore
.\dotnet.ps1 build CompanyBrain.Client.sln --no-restore
.\dotnet.ps1 run --project CompanyBrain.Client.csproj --no-build
```

`dotnet.ps1` prefers the SDK in `../.local/dotnet-sdk`, then the installed SDK.
`global.json` selects .NET 11. On Linux/macOS, use `dotnet` instead of the wrapper.
Backend setup: [backend/README.md](../backend/README.md).

## API

Default: `http://127.0.0.1:8000/api/v1/`. To use the gateway, set its public
address and port before launching:

```powershell
$env:COMPANY_BRAIN_API_BASE_URL = 'http://<gateway-address>:<public-web-port>/api/v1/'
.\dotnet.ps1 run --project CompanyBrain.Client.csproj
```

Use the same environment variable in an IDE or packaged app. Unset it to
restore the default. The URL includes the API prefix; a trailing slash is added.

`App` owns one `ApiClient` for the application lifetime and passes it to
`MainViewModel`. Reuse it in feature view models.

Await `GetAsync<T>(path, cancellationToken)` with relative paths such as
`customers?limit=20&offset=0`. Escape IDs/query values with `Uri.EscapeDataString`.
DTOs use snake_case JSON; lists use `ListResponse<T>`.

`ApiException` exposes `Kind`, `StatusCode` and JSON `Detail`. Caller cancellation
propagates as `OperationCanceledException`; timeout is 30 seconds.

## Tests

HTTP, view-model and headless UI tests run without a server or visible window:

```powershell
.\dotnet.ps1 test CompanyBrain.Client.sln --filter 'TestCategory!=Integration'
```

For UI tests only, use `--filter 'TestCategory=UI'`.

Collect coverage for the desktop assembly (including its HTTP layer):

```powershell
.\dotnet.ps1 test CompanyBrain.Client.sln --filter 'TestCategory!=Integration' --settings Tests/coverage.runsettings --collect 'Code Coverage' --results-directory ../.local/results/coverage
```

Cobertura reports stay outside Git. UI tests check controls and bindings.

For the backend smoke check, start FastAPI first, then run:

```powershell
$env:COMPANY_BRAIN_SMOKE_BASE_URL = 'http://127.0.0.1:8000/'
.\dotnet.ps1 test CompanyBrain.Client.sln --filter 'TestCategory=Integration'
```

The smoke check reads `GET /`; it is skipped when its URL is unset.

## VS Code

Open `desktop/` and install the extensions in `.vscode/extensions.json`.
Install .NET 11 system-wide for C# Dev Kit and the Avalonia previewer.
Use `Ctrl+Shift+B` to build, `F5` to debug, or the `Company Brain: run` task.

## Assets

The UI uses Fluent 2 and Inter. MIT-licensed [Tabler](https://github.com/tabler/tabler-icons)
and [Heroicons](https://github.com/tailwindlabs/heroicons) sources and notices
are in `Assets/Icons/`.
