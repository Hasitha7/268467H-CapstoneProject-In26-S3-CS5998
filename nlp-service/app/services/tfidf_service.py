import re

from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics.pairwise import cosine_similarity

from app.services.preprocessing import TextPreprocessor

_TOKEN_PATTERN = re.compile(
    r"\.net(?:\s+core)?|asp\.net(?:\s+core)?|node\.js|c\+\+|c#|f#|"
    r"[\w]+(?:[.+#-][\w+#-]+)*",
    re.IGNORECASE | re.UNICODE,
)


class TfIdfService:
    def __init__(self, preprocessor: TextPreprocessor | None = None) -> None:
        self._preprocessor = preprocessor or TextPreprocessor()

    @staticmethod
    def _tokenize(text: str) -> list[str]:
        return [token.casefold() for token in _TOKEN_PATTERN.findall(text)]

    def score(self, resume_text: str, job_description: str) -> float:
        documents = [
            self._preprocessor.preprocess(resume_text),
            self._preprocessor.preprocess(job_description),
        ]
        vectorizer = TfidfVectorizer(
            tokenizer=self._tokenize,
            token_pattern=None,
            lowercase=False,
            ngram_range=(1, 2),
        )
        try:
            vectors = vectorizer.fit_transform(documents)
        except ValueError as exc:
            if "empty vocabulary" in str(exc):
                return 0.0
            raise

        score = float(cosine_similarity(vectors[0:1], vectors[1:2])[0, 0])
        return min(1.0, max(0.0, score))
