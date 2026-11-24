# Company Brain desktop application

Desktop client built with Avalonia UI 12.2.0, C#, XAML and CommunityToolkit.Mvvm.
It targets .NET 11 and uses Fluent 2, the Inter font and a custom light theme
based on the four supplied Company Brain references.

## Visual preview

- Devices: sample device list, selection, details and related actions.
- Customers: sample customer, support agreements and registered device link.
- Support guides: sample results list and loan-device policy.
- Ask: editable sample question, static sample answer and source links.

Sidebar navigation and related screen links work locally. Search, agreement
PDFs, ticket details and AI answers are placeholders. Activating a placeholder
shows a dismissible notice; no backend or external service is contacted.

The top-right controls minimize, maximize/restore and close the window. Drag
the empty top strip to move the window, or double-click it to maximize/restore.

## Build and run

From this directory in PowerShell:

```powershell
.\dotnet.ps1 restore
.\dotnet.ps1 build --no-restore
.\dotnet.ps1 run --no-build
```

The wrapper uses the SDK downloaded to `../.local/dotnet-sdk` when available,
and otherwise uses an installed .NET SDK. `global.json` selects stable .NET 11
SDKs. The local SDK and package caches are ignored by Git. On another machine,
install .NET 11 SDK first; then standard `dotnet restore`, `dotnet build` and
`dotnet run` also work. On Linux/macOS, use the standard .NET commands.

## Structure

- `App.axaml`: theme and view templates.
- `Views/MainWindow.axaml`: sidebar, window controls and screen container.
- `Views/*View.axaml`: four placeholder screens.
- `Styles/CompanyBrain.axaml`: shared colors, typography and control states.
- `Styles/Icons.axaml`: geometry from MIT-licensed icon libraries.
- `Assets/Icons/`: original Tabler/Heroicons SVG sources and licenses.
- `ViewModels/MainViewModel.cs`: local screen state and sample data.
- `Models/`: reserved for application models.
- `Program.cs`: desktop application startup.

## VS Code

Open the `desktop` directory as your VS Code folder. The required extensions are
listed in `.vscode/extensions.json`: C#, C# Dev Kit and Avalonia for VSCode.

For C# Dev Kit project loading and the Avalonia XAML previewer on Windows,
install .NET 11 SDK system-wide using Microsoft's installer, then reload VS Code.
The local SDK is sufficient for `dotnet.ps1`, but editor extensions can launch
their own processes using the system .NET installation.

- `Ctrl+Shift+B`: build the client.
- `F5`: build and debug using the `Company Brain` configuration.
- `Terminal > Run Task > Company Brain: run`: build and run without debugging.

`CompanyBrain.Client.sln` uses a relative project path. `global.json` selects
.NET 11. The debugger uses the system .NET installation. The build task uses
`dotnet.ps1`, with its local-SDK preference and installed-SDK fallback.

Backend code belongs in [`../backend/`](../backend/). Shared documentation belongs
in [`../docs/`](../docs/).

This is a visual prototype; backend integration is not configured yet.
Only the free framework packages and CommunityToolkit are referenced. Optional
Avalonia diagnostics tooling was removed from the generated template.

## References and assets

- [Avalonia](https://avaloniaui.net/avalonia)
- [.NET 11 SDK](https://dotnet.microsoft.com/download/dotnet/11.0)
- [Tabler Icons](https://github.com/tabler/tabler-icons) and
  [Heroicons](https://github.com/tailwindlabs/heroicons): MIT-licensed icon sources;
  the original SVGs and license notices are included in `Assets/Icons/`.
