from collections.abc import Sequence

from app.schemas.requests import JobSkillRequest
from app.services.skill_extraction_service import SkillExtractionService

REQUIRED_WEIGHT = 1.0
PREFERRED_WEIGHT = 0.5


class SkillMatchingService:
    def __init__(self, extractor: SkillExtractionService) -> None:
        self._extractor = extractor

    def calculate_coverage(
        self,
        resume_skills: Sequence[str],
        job_skills: Sequence[JobSkillRequest | dict[str, str]],
        *,
        resume_context: str | None = None,
        job_context: str | None = None,
    ) -> dict[str, object]:
        normalized_resume = {
            self._extractor.normalize_skill(skill, resume_context)
            for skill in resume_skills
        }

        required: dict[str, str] = {}
        preferred: dict[str, str] = {}
        for job_skill in job_skills:
            if isinstance(job_skill, dict):
                name = job_skill["name"]
                importance = job_skill.get("importance", "Required")
            else:
                name = job_skill.name
                importance = job_skill.importance
            normalized_name = self._extractor.normalize_skill(name, job_context)
            target = required if importance.casefold() == "required" else preferred
            target.setdefault(normalized_name, normalized_name)

        # A requirement remains required if it is listed with both importance levels.
        preferred = {
            name: value for name, value in preferred.items() if name not in required
        }
        matched_required = sorted(set(required) & normalized_resume, key=str.casefold)
        missing_required = sorted(set(required) - normalized_resume, key=str.casefold)
        matched_preferred = sorted(set(preferred) & normalized_resume, key=str.casefold)
        missing_preferred = sorted(set(preferred) - normalized_resume, key=str.casefold)

        required_coverage = (
            len(matched_required) / len(required) if required else 1.0
        )
        denominator = REQUIRED_WEIGHT * len(required) + PREFERRED_WEIGHT * len(preferred)
        overall_coverage = (
            (
                REQUIRED_WEIGHT * len(matched_required)
                + PREFERRED_WEIGHT * len(matched_preferred)
            )
            / denominator
            if denominator
            else 0.0
        )

        return {
            "matched_required_skills": matched_required,
            "missing_required_skills": missing_required,
            "matched_preferred_skills": matched_preferred,
            "missing_preferred_skills": missing_preferred,
            "required_skill_coverage_score": required_coverage,
            "overall_skill_coverage_score": overall_coverage,
            "matched_skills": matched_required + matched_preferred,
        }
