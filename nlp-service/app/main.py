import logging
from contextlib import asynccontextmanager
from dataclasses import dataclass

from fastapi import FastAPI

from app.api.routes import router
from app.config import Settings
from app.services.evaluation_service import EvaluationService
from app.services.matching_service import MatchingService, ScoreWeights
from app.services.preprocessing import TextPreprocessor
from app.services.semantic_service import SemanticService
from app.services.skill_extraction_service import SkillExtractionService
from app.services.skill_matching_service import SkillMatchingService
from app.services.tfidf_service import TfIdfService

logger = logging.getLogger(__name__)


@dataclass
class NLPServices:
    semantic: SemanticService
    tfidf: TfIdfService
    skill_extractor: SkillExtractionService
    skill_matcher: SkillMatchingService
    matcher: MatchingService
    evaluator: EvaluationService


def build_services(settings: Settings) -> NLPServices:
    preprocessor = TextPreprocessor()
    semantic = SemanticService(
        settings.model_name,
        device=settings.device,
        cache_dir=settings.model_cache_dir,
        preprocessor=preprocessor,
    )
    tfidf = TfIdfService(preprocessor)
    skill_extractor = SkillExtractionService()
    skill_matcher = SkillMatchingService(skill_extractor)
    matcher = MatchingService(
        preprocessor=preprocessor,
        tfidf=tfidf,
        semantic=semantic,
        skill_extractor=skill_extractor,
        skill_matcher=skill_matcher,
        weights=ScoreWeights(
            tfidf=settings.tfidf_weight,
            semantic=settings.semantic_weight,
            skills=settings.skill_weight,
        ),
    )
    return NLPServices(
        semantic=semantic,
        tfidf=tfidf,
        skill_extractor=skill_extractor,
        skill_matcher=skill_matcher,
        matcher=matcher,
        evaluator=EvaluationService(),
    )


def create_app(
    settings: Settings | None = None,
    *,
    load_model_at_startup: bool | None = None,
) -> FastAPI:
    configured = settings or Settings()
    configured.validate()
    should_load = (
        configured.load_model_at_startup
        if load_model_at_startup is None
        else load_model_at_startup
    )
    services = build_services(configured)

    @asynccontextmanager
    async def lifespan(_app: FastAPI):
        if should_load:
            try:
                services.semantic.load_model()
                logger.info("Loaded semantic model %s.", services.semantic.model_version)
            except Exception:
                logger.exception(
                    "Failed to load semantic model %s; semantic endpoints will return 503.",
                    services.semantic.model_version,
                )
        yield

    application = FastAPI(
        title="Resume Matcher NLP Service",
        description="Text similarity, technical skill analysis, and labelled-pair evaluation.",
        version="1.0.0",
        lifespan=lifespan,
    )
    application.state.services = services
    application.include_router(router)
    return application


app = create_app()
