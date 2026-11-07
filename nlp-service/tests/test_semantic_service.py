import sys
import types

import numpy as np
import pytest

from app.services.semantic_service import SemanticService


class FakeModel:
    def encode(self, texts, **_kwargs):
        vectors = {
            "same": [1.0, 0.0],
            "similar": [0.8, 0.6],
            "opposite": [-1.0, 0.0],
        }
        return np.asarray([vectors[text] for text in texts])


def test_semantic_score_is_normalized_cosine_similarity():
    service = SemanticService("fake-model")
    service.set_model(FakeModel())
    assert service.cosine_similarity("same", "opposite") == -1
    assert service.score("same", "opposite") == 0
    assert service.score("same", "same") == 1
    assert service.score("same", "similar") == pytest.approx(0.9)


def test_model_loads_once_and_reports_not_loaded_before_startup(monkeypatch):
    calls = []

    class FakeSentenceTransformer:
        def __init__(self, model_name, **options):
            calls.append((model_name, options))

    monkeypatch.setitem(
        sys.modules,
        "sentence_transformers",
        types.SimpleNamespace(SentenceTransformer=FakeSentenceTransformer),
    )
    service = SemanticService("model", device="cpu")
    assert not service.is_loaded
    service.load_model()
    service.load_model()
    assert service.is_loaded
    assert calls == [("model", {"device": "cpu"})]


def test_semantic_service_rejects_use_before_model_load():
    with pytest.raises(RuntimeError, match="has not been loaded"):
        SemanticService("model").score("one", "two")
