# Repository structure

Company Brain separates backend services, the desktop application and project documentation into dedicated top-level directories.

```text
Company_Brain/
├── README.md
├── backend/
│   ├── api/
│   ├── core/
│   ├── database/
│   ├── models/
│   ├── schemas/
│   ├── services/
│   └── README.md
├── desktop/
│   └── README.md
└── docs/
    └── repository-structure.md
```

| Directory | Responsibility |
| --- | --- |
| `backend/` | Backend services, APIs, server-side business logic and their configuration. |
| `desktop/` | Desktop application source code, UI assets and desktop-specific configuration. |
| `docs/` | Shared project documentation, architecture notes and setup or deployment guides. |

## Placement rules

- Keep backend implementation inside `backend/`.
- Organize backend API, configuration, database, model, schema and service code
  into their corresponding subdirectories.
- Keep desktop application implementation inside `desktop/`.
- Document component-specific setup in that component's README.
- Keep documentation that applies to the whole project inside `docs/`, linked from the root README.

Each component directory starts with a README so the intended structure is present in Git before implementation begins.
