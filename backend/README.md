# Backend

The backend is organized by responsibility:

| Directory | Responsibility |
| --- | --- |
| [`api/`](api/) | HTTP routes and API boundary logic. |
| [`core/`](core/) | Application configuration and shared core concerns. |
| [`database/`](database/) | Database connection and persistence setup. |
| [`models/`](models/) | Domain and persistence models. |
| [`schemas/`](schemas/) | Request, response and validation schemas. |
| [`services/`](services/) | Business logic used by API handlers. |

Keep desktop application code in [`../desktop/`](../desktop/) and project
documentation in [`../docs/`](../docs/). Backend runtime and framework selection
will be added when backend implementation begins.
