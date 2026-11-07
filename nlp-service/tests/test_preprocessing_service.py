import pytest

from app.services.preprocessing import TextPreprocessor
from app.utils.text_utils import normalize_text


def test_preprocessor_normalizes_unicode_and_whitespace_but_keeps_tech_symbols():
    text = "  Ｃ＃\tC++\n.NET   Node.js  "
    assert TextPreprocessor().preprocess(text) == "C# C++ .NET Node.js"


@pytest.mark.parametrize("text", ["", " \t\n ", "\u200b"])
def test_empty_or_invisible_text_is_rejected(text):
    with pytest.raises(ValueError):
        normalize_text(text)
