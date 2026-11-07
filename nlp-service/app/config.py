import math
import os
from dataclasses import dataclass

from dotenv import load_dotenv

load_dotenv()


@dataclass(frozen=True)
class Settings:
    model_name: str = os.getenv(
        "NLP_SEMANTIC_MODEL", "sentence-transformers/all-MiniLM-L6-v2"
    )
    device: str = os.getenv("NLP_DEVICE", "cpu")
    model_cache_dir: str | None = os.getenv("NLP_MODEL_CACHE_DIR") or None
    load_model_at_startup: bool = os.getenv(
        "NLP_LOAD_MODEL", "true"
    ).strip().casefold() not in {"0", "false", "no"}
    max_text_length: int = int(os.getenv("NLP_MAX_TEXT_LENGTH", "200000"))
    tfidf_weight: float = float(os.getenv("NLP_TFIDF_WEIGHT", "0"))
    semantic_weight: float = float(os.getenv("NLP_SEMANTIC_WEIGHT", "0.5"))
    skill_weight: float = float(os.getenv("NLP_SKILL_WEIGHT", "0.5"))

    def validate(self) -> None:
        if self.max_text_length < 1 or self.max_text_length > 200_000:
            raise ValueError("NLP_MAX_TEXT_LENGTH must be between 1 and 200000.")
        weights = (self.tfidf_weight, self.semantic_weight, self.skill_weight)
        if any(not math.isfinite(weight) or weight < 0 for weight in weights):
            raise ValueError("Scoring weights must be finite and non-negative.")
        if not math.isclose(sum(weights), 1.0, abs_tol=1e-9):
            raise ValueError("NLP scoring weights must sum to 1.")
