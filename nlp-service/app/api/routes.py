import logging

from fastapi import APIRouter, HTTPException, Request, status

from app.schemas.requests import (
    AnalysisRequest,
    EvaluationRequest,
    ExtractSkillsRequest,
    LegacySimilarityRequest,
    SimilarityRequest,
    SkillCoverageRequest,
)
from app.schemas.responses import (
    AnalysisResponse,
    EvaluationResponse,
    ExtractSkillsResponse,
    HealthResponse,
    SemanticScoreResponse,
    SkillCoverageResponse,
    TfIdfScoreResponse,
)

logger = logging.getLogger(__name__)
router = APIRouter()


def _services(request: Request):
    return request.app.state.services


def _semantic_unavailable() -> HTTPException:
    return HTTPException(
        status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
        detail="The semantic model is unavailable. Check service logs and model configuration.",
    )


@router.get("/health", response_model=HealthResponse)
def health(request: Request) -> HealthResponse:
    semantic = _services(request).semantic
    return HealthResponse(
        status="ok",
        service="resume-matcher-nlp",
        model_loaded=semantic.is_loaded,
        model_version=semantic.model_version,
        model_loading_status="loaded" if semantic.is_loaded else "unavailable",
    )


@router.get("/")
def root() -> dict[str, str]:
    return {"message": "Resume Matcher NLP service is running."}


@router.post("/similarity")
def legacy_similarity(
    payload: LegacySimilarityRequest, request: Request
) -> dict[str, float | str]:
    """Keep the original response shape used by the ASP.NET Core client."""
    semantic = _services(request).semantic
    if not semantic.is_loaded:
        raise _semantic_unavailable()

    raw_cosine = semantic.cosine_similarity(payload.text1, payload.text2)
    return {
        "cosineScore": raw_cosine,
        "similarity": (raw_cosine + 1.0) / 2.0,
        "modelVersion": semantic.model_version,
    }


@router.post("/nlp/extract-skills", response_model=ExtractSkillsResponse)
def extract_skills(
    payload: ExtractSkillsRequest, request: Request
) -> ExtractSkillsResponse:
    skills = _services(request).skill_extractor.extract(payload.text)
    logger.info("Extracted %d unique skills.", len(skills))
    return ExtractSkillsResponse(skills=skills)


@router.post("/nlp/tfidf-score", response_model=TfIdfScoreResponse)
def tfidf_score(payload: SimilarityRequest, request: Request) -> TfIdfScoreResponse:
    score = _services(request).tfidf.score(payload.resume_text, payload.job_description)
    return TfIdfScoreResponse(score=score)


@router.post("/nlp/semantic-score", response_model=SemanticScoreResponse)
def semantic_score(
    payload: SimilarityRequest, request: Request
) -> SemanticScoreResponse:
    semantic = _services(request).semantic
    if not semantic.is_loaded:
        raise _semantic_unavailable()
    score = semantic.score(payload.resume_text, payload.job_description)
    return SemanticScoreResponse(score=score, model_version=semantic.model_version)


@router.post("/nlp/skill-coverage", response_model=SkillCoverageResponse)
def skill_coverage(
    payload: SkillCoverageRequest, request: Request
) -> SkillCoverageResponse:
    services = _services(request)
    result = services.skill_matcher.calculate_coverage(
        payload.resume_skills, payload.job_skills
    )
    return SkillCoverageResponse(**result)


@router.post("/nlp/analyze", response_model=AnalysisResponse)
def analyze(payload: AnalysisRequest, request: Request) -> AnalysisResponse:
    services = _services(request)
    if not services.semantic.is_loaded:
        raise _semantic_unavailable()
    result = services.matcher.analyze(payload)
    logger.info("Completed document analysis.")
    return AnalysisResponse(**result)


@router.post("/nlp/evaluate", response_model=EvaluationResponse)
def evaluate(
    payload: EvaluationRequest, request: Request
) -> EvaluationResponse:
    services = _services(request)
    if not services.semantic.is_loaded:
        raise _semantic_unavailable()
    result = services.evaluator.evaluate(payload.pairs, services.matcher)
    logger.info("Evaluated %d labelled document pairs.", len(payload.pairs))
    return EvaluationResponse(**result)
