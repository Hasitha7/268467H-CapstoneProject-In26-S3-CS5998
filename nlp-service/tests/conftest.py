import numpy as np
import pytest
from fastapi.testclient import TestClient

from app.config import Settings
from app.main import create_app


class FakeEmbeddingModel:
    def encode(self, texts, **_kwargs):
        vectors = {
            "same text": [1.0, 0.0],
            "similar text": [0.8, 0.6],
            "opposite text": [-1.0, 0.0],
        }
        return np.asarray(
            [vectors.get(text.casefold(), [0.0, 1.0]) for text in texts],
            dtype=float,
        )


@pytest.fixture
def api_client():
    settings = Settings(
        load_model_at_startup=False,
        max_text_length=200_000,
        tfidf_weight=0,
        semantic_weight=0.5,
        skill_weight=0.5,
    )
    application = create_app(settings, load_model_at_startup=False)
    application.state.services.semantic.set_model(FakeEmbeddingModel())
    with TestClient(application) as client:
        yield client
