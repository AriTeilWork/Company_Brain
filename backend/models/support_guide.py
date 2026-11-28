from datetime import date

from sqlalchemy import CheckConstraint, Date, String, Text
from sqlalchemy.orm import Mapped, mapped_column

from database.base import Base


class SupportGuideRevision(Base):
    __tablename__ = "support_guide_revisions"
    __table_args__ = (
        CheckConstraint(
            "status IN ('published', 'archived', 'draft')",
            name="ck_support_guide_status",
        ),
        CheckConstraint(
            "status <> 'published' OR effective_from IS NOT NULL",
            name="ck_support_guide_published_start",
        ),
        CheckConstraint(
            "effective_from IS NULL OR effective_until IS NULL "
            "OR effective_until >= effective_from",
            name="ck_support_guide_effective_range",
        ),
    )

    guide_id: Mapped[str] = mapped_column(String, primary_key=True)
    version: Mapped[str] = mapped_column(String, primary_key=True)
    title: Mapped[str] = mapped_column(Text, nullable=False)
    status: Mapped[str] = mapped_column(String, nullable=False)
    effective_from: Mapped[date | None] = mapped_column(Date, nullable=True)
    effective_until: Mapped[date | None] = mapped_column(Date, nullable=True)
    markdown_body: Mapped[str] = mapped_column(Text, nullable=False)
    source_path: Mapped[str] = mapped_column(Text, nullable=False)
