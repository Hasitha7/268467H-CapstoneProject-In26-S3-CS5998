from typing import Any

import numpy as np

from app.services.preprocessing import TextPreprocessor


class SemanticService:
    def __init__(
        self,
        model_name: str,
        device: str = "cpu",
        cache_dir: str | None = None,
        preprocessor: TextPreprocessor | None = None,
    ) -> None:
        self.model_name = model_name
        self.device = device
        self.cache_dir = cache_dir
        self._model: Any | None = None
        self._preprocessor = preprocessor or TextPreprocessor()

    @property
    def is_loaded(self) -> bool:
        return self._model is not None

    @property
    def model_version(self) -> str:
        return self.model_name

    def load_model(self) -> None:
        if self.is_loaded:
            return

        from sentence_transformers import SentenceTransformer

        options: dict[str, str] = {"device": self.device}
        if self.cache_dir:
            options["cache_folder"] = self.cache_dir
        self._model = SentenceTransformer(self.model_name, **options)

    def set_model(self, model: Any) -> None:
        """Inject a compatible model, primarily for tests and alternate deployments."""
        self._model = model

    def cosine_similarity(self, first_text: str, second_text: str) -> float:
        if not self.is_loaded:
            raise RuntimeError("Sentence-BERT model has not been loaded.")

        documents = [
            self._preprocessor.preprocess(first_text),
            self._preprocessor.preprocess(second_text),
        ]
        embeddings = self._model.encode(
            documents,
            convert_to_numpy=True,
            normalize_embeddings=True,
            show_progress_bar=False,
        )
        vectors = np.asarray(embeddings, dtype=np.float64)
        if vectors.shape[0] != 2 or vectors.ndim != 2:
            raise ValueError("Sentence-BERT returned embeddings with an invalid shape.")
        cosine = float(np.dot(vectors[0], vectors[1]))
        if not np.isfinite(cosine):
            raise ValueError("Sentence-BERT returned a non-finite similarity.")
        return min(1.0, max(-1.0, cosine))

    def score(self, first_text: str, second_text: str) -> float:
        return (self.cosine_similarity(first_text, second_text) + 1.0) / 2.0
