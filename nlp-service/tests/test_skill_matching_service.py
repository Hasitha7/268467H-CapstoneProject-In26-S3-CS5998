import pytest

from app.schemas.requests import JobSkillRequest
from app.services.skill_extraction_service import SkillExtractionService
from app.services.skill_matching_service import SkillMatchingService


def test_coverage_normalizes_aliases_and_weights_preferred_skills():
    extractor = SkillExtractionService()
    service = SkillMatchingService(extractor)
    result = service.calculate_coverage(
        ["JS", "MS SQL"],
        [
            JobSkillRequest(name="JavaScript", importance="Required"),
            JobSkillRequest(name="SQL Server", importance="Required"),
            JobSkillRequest(name="Azure", importance="Preferred"),
        ],
    )

    assert result["matched_required_skills"] == ["JavaScript", "SQL Server"]
    assert result["missing_required_skills"] == []
    assert result["missing_preferred_skills"] == ["Microsoft Azure"]
    assert result["required_skill_coverage_score"] == 1
    assert result["overall_skill_coverage_score"] == pytest.approx(0.8)


def test_required_importance_wins_duplicate_preferred_entry():
    service = SkillMatchingService(SkillExtractionService())
    result = service.calculate_coverage(
        ["Python"],
        [
            {"name": "Python", "importance": "Preferred"},
            {"name": "Python", "importance": "Required"},
        ],
    )
    assert result["matched_required_skills"] == ["Python"]
    assert result["matched_preferred_skills"] == []


def test_empty_requirement_coverage_is_defined_and_bounded():
    result = SkillMatchingService(SkillExtractionService()).calculate_coverage([], [])
    assert result["required_skill_coverage_score"] == 1
    assert result["overall_skill_coverage_score"] == 0


def test_contextual_dotnet_equivalence_is_used_for_analyze_text():
    service = SkillMatchingService(SkillExtractionService())
    result = service.calculate_coverage(
        [".NET Core"],
        [{"name": "ASP.NET Core", "importance": "Required"}],
        resume_context="Built .NET Core web APIs",
        job_context="Developed ASP.NET Core web services",
    )
    assert result["matched_required_skills"] == ["ASP.NET Core"]
