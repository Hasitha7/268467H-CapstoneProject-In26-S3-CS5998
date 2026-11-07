from dataclasses import dataclass

from app.schemas.requests import AnalysisRequest, EvaluationPairRequest
from app.services.preprocessing import TextPreprocessor
from app.services.semantic_service import SemanticService
from app.services.skill_extraction_service import SkillExtractionService
from app.services.skill_matching_service import SkillMatchingService
from app.services.tfidf_service import TfIdfService


@dataclass(frozen=True)
class ScoreWeights:
    tfidf: float
    semantic: float
    skills: float

    @property
    def formula(self) -> str:
        return (
            f"{self.tfidf:g} × TF-IDF + {self.semantic:g} × semantic + "
            f"{self.skills:g} × skill coverage"
        )


class MatchingService:
    def __init__(
        self,
        preprocessor: TextPreprocessor,
        tfidf: TfIdfService,
        semantic: SemanticService,
        skill_extractor: SkillExtractionService,
        skill_matcher: SkillMatchingService,
        weights: ScoreWeights,
    ) -> None:
        self._preprocessor = preprocessor
        self._tfidf = tfidf
        self._semantic = semantic
        self._skill_extractor = skill_extractor
        self._skill_matcher = skill_matcher
        self._weights = weights

    def analyze(self, request: AnalysisRequest | EvaluationPairRequest) -> dict[str, object]:
        resume_text = self._preprocessor.preprocess(request.resume_text)
        job_description = self._preprocessor.preprocess(request.job_description)

        tfidf_score = self._tfidf.score(resume_text, job_description)
        semantic_score = self._semantic.score(resume_text, job_description)

        resume_skills = request.resume_skills or [
            skill["name"] for skill in self._skill_extractor.extract(resume_text)
        ]
        job_skills = request.job_skills or [
            {"name": skill["name"], "importance": "Required"}
            for skill in self._skill_extractor.extract(job_description)
        ]
        coverage = self._skill_matcher.calculate_coverage(
            resume_skills,
            job_skills,
            resume_context=resume_text,
            job_context=job_description,
        )
        skill_coverage = float(coverage["overall_skill_coverage_score"])
        final_score = (
            self._weights.tfidf * tfidf_score
            + self._weights.semantic * semantic_score
            + self._weights.skills * skill_coverage
        )
        final_score = min(1.0, max(0.0, final_score))

        return {
            **coverage,
            "tfidf_score": tfidf_score,
            "semantic_score": semantic_score,
            "skill_coverage_score": skill_coverage,
            "final_score": final_score,
            "model_version": self._semantic.model_version,
            "scoring_formula": self._weights.formula,
        }
