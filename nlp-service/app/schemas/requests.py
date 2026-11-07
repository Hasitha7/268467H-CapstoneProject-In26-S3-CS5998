from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, field_validator

from app.config import Settings
from app.utils.text_utils import normalize_text

MAX_TEXT_LENGTH = Settings().max_text_length
MAX_SKILLS = 1_000


class RequestModel(BaseModel):
    model_config = ConfigDict(extra="forbid")


def _validate_text(value: str) -> str:
    try:
        return normalize_text(value)
    except ValueError as exc:
        raise ValueError(str(exc)) from exc


class ExtractSkillsRequest(RequestModel):
    text: str = Field(min_length=1, max_length=MAX_TEXT_LENGTH)

    @field_validator("text")
    @classmethod
    def validate_text(cls, value: str) -> str:
        return _validate_text(value)


class SimilarityRequest(RequestModel):
    resume_text: str = Field(min_length=1, max_length=MAX_TEXT_LENGTH)
    job_description: str = Field(min_length=1, max_length=MAX_TEXT_LENGTH)

    @field_validator("resume_text", "job_description")
    @classmethod
    def validate_text(cls, value: str) -> str:
        return _validate_text(value)


class LegacySimilarityRequest(RequestModel):
    text1: str = Field(min_length=1, max_length=MAX_TEXT_LENGTH)
    text2: str = Field(min_length=1, max_length=MAX_TEXT_LENGTH)

    @field_validator("text1", "text2")
    @classmethod
    def validate_text(cls, value: str) -> str:
        return _validate_text(value)


class JobSkillRequest(RequestModel):
    name: str = Field(min_length=1, max_length=150)
    importance: Literal["Required", "Preferred"] = "Required"

    @field_validator("name")
    @classmethod
    def validate_name(cls, value: str) -> str:
        value = " ".join(value.split())
        if not value:
            raise ValueError("Skill name must not be empty.")
        return value

    @field_validator("importance", mode="before")
    @classmethod
    def normalize_importance(cls, value: str) -> str:
        if isinstance(value, str):
            normalized = value.strip().casefold()
            if normalized in {"required", "preferred"}:
                return normalized.title()
        return value


class SkillCoverageRequest(RequestModel):
    resume_skills: list[str] = Field(max_length=MAX_SKILLS)
    job_skills: list[JobSkillRequest] = Field(max_length=MAX_SKILLS)

    @field_validator("resume_skills")
    @classmethod
    def validate_resume_skills(cls, values: list[str]) -> list[str]:
        cleaned = [" ".join(value.split()) for value in values]
        if any(not value for value in cleaned):
            raise ValueError("Resume skill names must not be empty.")
        return cleaned


class AnalysisRequest(SimilarityRequest):
    resume_skills: list[str] = Field(default_factory=list, max_length=MAX_SKILLS)
    job_skills: list[JobSkillRequest] = Field(default_factory=list, max_length=MAX_SKILLS)

    @field_validator("resume_skills")
    @classmethod
    def validate_resume_skills(cls, values: list[str]) -> list[str]:
        cleaned = [" ".join(value.split()) for value in values]
        if any(not value for value in cleaned):
            raise ValueError("Resume skill names must not be empty.")
        return cleaned


class EvaluationPairRequest(SimilarityRequest):
    label: int = Field(ge=0, le=3)
    resume_skills: list[str] = Field(default_factory=list, max_length=MAX_SKILLS)
    job_skills: list[JobSkillRequest] = Field(default_factory=list, max_length=MAX_SKILLS)

    @field_validator("resume_skills")
    @classmethod
    def validate_resume_skills(cls, values: list[str]) -> list[str]:
        cleaned = [" ".join(value.split()) for value in values]
        if any(not value for value in cleaned):
            raise ValueError("Resume skill names must not be empty.")
        return cleaned


class EvaluationRequest(RequestModel):
    pairs: list[EvaluationPairRequest] = Field(min_length=1, max_length=500)
