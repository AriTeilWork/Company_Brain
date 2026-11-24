# Repository structure

Company Brain separates backend services, the desktop application and project documentation into dedicated top-level directories.

```text
Company_Brain/
├── README.md
├── backend/
│   └── README.md
├── desktop/
│   ├── README.md
│   ├── CompanyBrain.Client.csproj
│   ├── CompanyBrain.Client.sln
│   ├── App.axaml
│   ├── Views/
│   ├── ViewModels/
│   ├── Styles/
│   ├── Assets/
│   └── .vscode/
└── docs/
    ├── repository-structure.md
    ├── desktop-ui-qa.md
    └── screenshots/
```

| Directory | Responsibility |
| --- | --- |
| `backend/` | Backend services, APIs, server-side business logic and their configuration. |
| `desktop/` | Desktop application source code, UI assets and desktop-specific configuration. |
| `docs/` | Shared project documentation, architecture notes and setup or deployment guides. |

## Placement rules

- Keep backend implementation inside `backend/`.
- Keep desktop application implementation inside `desktop/`.
- Document component-specific setup in that component's README.
- Keep documentation that applies to the whole project inside `docs/`, linked from the root README.

The Avalonia desktop project lives directly in `desktop/`. Its project and
solution retain the `CompanyBrain.Client` name; this does not introduce a
separate top-level `client/` directory. Generated outputs (`bin/`, `obj/`,
`.dist/`) and local tooling (`.local/`) are ignored by Git.
