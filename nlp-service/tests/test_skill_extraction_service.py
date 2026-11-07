from app.services.skill_extraction_service import SkillExtractionService


def test_extracts_unique_canonical_skills_from_sample():
    skills = SkillExtractionService().extract(
        "Developed REST APIs using Python Flask and SQL Server."
    )
    assert skills == [
        {"name": "REST API", "category": "Backend"},
        {"name": "Python", "category": "Programming"},
        {"name": "Flask", "category": "Framework"},
        {"name": "SQL Server", "category": "Database"},
    ]


def test_aliases_are_normalized_without_merging_unrelated_technologies():
    skills = SkillExtractionService().extract("Used JS, JavaScript, MS SQL, and SQL Server.")
    assert [skill["name"] for skill in skills] == ["JavaScript", "SQL Server"]


def test_dotnet_core_equivalence_requires_web_context():
    extractor = SkillExtractionService()
    assert extractor.normalize_skill(".NET Core") == ".NET Core"
    assert extractor.extract("Worked with .NET Core web APIs.") == [
        {"name": "ASP.NET Core", "category": "Framework"}
    ]
    assert extractor.extract("Worked with .NET Core runtime.") == [
        {"name": ".NET Core", "category": "Programming"}
    ]
