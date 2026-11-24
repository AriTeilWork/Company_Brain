# Company_Brain
Company Brain is an internal IT support application for Ember Island IT Services, designed to help technicians manage customer and laptop records, review support agreements and tickets, and access troubleshooting guides. The backend is deployed across three Proxmox LXC containers.

## Repository structure

```text
Company_Brain/
├── backend/    # Backend services, APIs and server-side logic
├── desktop/    # Desktop application code and assets
└── docs/       # Project documentation
```

See [Repository structure](docs/repository-structure.md) for directory responsibilities.

## Desktop UI preview

The Avalonia desktop mockup lives in [`desktop/`](desktop/). It includes Devices,
Customers, Support guides and Ask screens with static sample data.

Install the .NET 11 SDK, then run from the repository root:

```sh
dotnet run --project desktop/CompanyBrain.Client.csproj
```

See [desktop setup and VS Code instructions](desktop/README.md) and
[UI verification and screenshots](docs/desktop-ui-qa.md).
