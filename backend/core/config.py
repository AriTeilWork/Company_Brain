"""Backend configuration loaded from environment variables.

The same code runs locally, in Docker Compose and on the application LXC.
Only the environment changes:

- Local / Docker Compose: values come from the repository root ``.env``
  (see ``.env.example``) or from variables set by Compose.
- LXC deployment: systemd loads ``/etc/company-brain/backend.env``.

Real environment variables always take precedence over the ``.env`` file.
"""

from functools import lru_cache
from pathlib import Path

from pydantic import PostgresDsn, SecretStr, TypeAdapter, field_validator
from pydantic_settings import BaseSettings, SettingsConfigDict

# backend/core/config.py -> repository root
_REPO_ROOT_ENV = Path(__file__).resolve().parents[2] / ".env"

_postgres_dsn = TypeAdapter(PostgresDsn)


class Settings(BaseSettings):
    """Typed backend settings."""

    model_config = SettingsConfigDict(
        env_file=_REPO_ROOT_ENV,
        env_file_encoding="utf-8",
        extra="ignore",  # .env also holds POSTGRES_* values for the database container
        hide_input_in_errors=True,  # never echo DATABASE_URL (password) in errors
    )

    database_url: SecretStr
    """PostgreSQL connection URL (env: ``DATABASE_URL``).

    Format: ``postgresql://<user>:<password>@<host>:<port>/<database>``.
    Stored as a secret so the password is never shown in logs or reprs.
    """

    @field_validator("database_url")
    @classmethod
    def _validate_database_url(cls, value: SecretStr) -> SecretStr:
        try:
            _postgres_dsn.validate_python(value.get_secret_value())
        except ValueError:
            # Do not include the value itself: it contains the password.
            raise ValueError(
                "DATABASE_URL must be a PostgreSQL URL like "
                "postgresql://user:password@host:5432/database"
            ) from None
        return value


@lru_cache
def get_settings() -> Settings:
    """Load settings once on first use.

    Settings are loaded lazily, so the API and ``/docs`` start even when no
    database is configured. A missing or invalid ``DATABASE_URL`` raises a
    ``pydantic.ValidationError`` when database settings are first requested.
    """
    return Settings()
