from pydantic import BaseModel, ConfigDict, Field


class ResponseModel(BaseModel):
    model_config = ConfigDict(extra="forbid")


class SkillResponse(ResponseModel):
    name: str
    category: str


class ExtractSkillsResponse(ResponseModel):
    skills: list[SkillResponse]


class HealthResponse(ResponseModel):
    status: str
    service: str
    model_loaded: bool
    model_version: str
    model_loading_status: str


class TfIdfScoreResponse(ResponseModel):
    score: float = Field(ge=0, le=1)
    metric: str = "tfidf_cosine_similarity"


class SemanticScoreResponse(ResponseModel):
    score: float = Field(ge=0, le=1)
    metric: str = "normalized_sentence_bert_cosine_similarity"
    model_version: str


class SkillCoverageResponse(ResponseModel):
    matched_required_skills: list[str]
    missing_required_skills: list[str]
    matched_preferred_skills: list[str]
    missing_preferred_skills: list[str]
    required_skill_coverage_score: float = Field(ge=0, le=1)
    overall_skill_coverage_score: float = Field(ge=0, le=1)
    matched_skills: list[str]


class AnalysisResponse(SkillCoverageResponse):
    tfidf_score: float = Field(ge=0, le=1)
    semantic_score: float = Field(ge=0, le=1)
    skill_coverage_score: float = Field(ge=0, le=1)
    final_score: float = Field(ge=0, le=1)
    model_version: str
    scoring_formula: str


class EvaluationPrediction(ResponseModel):
    actual_label: int
    predicted_label: int
    final_score: float = Field(ge=0, le=1)


class EvaluationResponse(ResponseModel):
    accuracy: float = Field(ge=0, le=1)
    macro_precision: float = Field(ge=0, le=1)
    macro_recall: float = Field(ge=0, le=1)
    macro_f1: float = Field(ge=0, le=1)
    mean_absolute_error: float = Field(ge=0, le=3)
    confusion_matrix: list[list[int]]
    predictions: list[EvaluationPrediction]
    score_to_label_mapping: dict[str, str]
    limitation: str
