import numpy as np

from app.config import Settings
from app.main import build_services
from app.schemas.requests import AnalysisRequest


class FakeModel:
    def encode(self, texts, **_kwargs):
        return np.asarray([[1.0, 0.0], [1.0, 0.0]])


def test_analysis_runs_pipeline_and_applies_documented_default_formula():
    settings = Settings(
        load_model_at_startup=False,
        max_text_length=200_000,
        tfidf_weight=0,
        semantic_weight=0.5,
        skill_weight=0.5,
    )
    services = build_services(settings)
    services.semantic.set_model(FakeModel())

    request = AnalysisRequest(
        resume_text="Python developer",
        job_description="Python developer",
    )
    result = services.matcher.analyze(request)

    assert result["tfidf_score"] == 1
    assert result["semantic_score"] == 1
    assert result["skill_coverage_score"] == 1
    assert result["final_score"] == 1
    assert result["matched_skills"] == ["Python"]
    assert result["scoring_formula"] == "0 × TF-IDF + 0.5 × semantic + 0.5 × skill coverage"
