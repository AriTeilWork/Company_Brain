"""Add versioned support guide storage."""

import sqlalchemy as sa
from alembic import op

revision: str = "0044_support_guide_revisions"
down_revision: str | None = "0001"
branch_labels: str | None = None
depends_on: str | None = None


def upgrade() -> None:
    op.create_table(
        "support_guide_revisions",
        sa.Column("guide_id", sa.String(), nullable=False),
        sa.Column("version", sa.String(), nullable=False),
        sa.Column("title", sa.Text(), nullable=False),
        sa.Column("status", sa.String(), nullable=False),
        sa.Column("effective_from", sa.Date(), nullable=True),
        sa.Column("effective_until", sa.Date(), nullable=True),
        sa.Column("markdown_body", sa.Text(), nullable=False),
        sa.Column("source_path", sa.Text(), nullable=False),
        sa.PrimaryKeyConstraint("guide_id", "version"),
        sa.CheckConstraint(
            "status IN ('published', 'archived', 'draft')",
            name="ck_support_guide_status",
        ),
        sa.CheckConstraint(
            "status <> 'published' OR effective_from IS NOT NULL",
            name="ck_support_guide_published_start",
        ),
        sa.CheckConstraint(
            "effective_from IS NULL OR effective_until IS NULL "
            "OR effective_until >= effective_from",
            name="ck_support_guide_effective_range",
        ),
    )


def downgrade() -> None:
    op.drop_table("support_guide_revisions")
