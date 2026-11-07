from app.utils.text_utils import normalize_text


class TextPreprocessor:
    """Normalize text consistently without discarding technical punctuation."""

    def preprocess(self, text: str) -> str:
        return normalize_text(text)
