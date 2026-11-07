import pytest

from app.services.tfidf_service import TfIdfService


def test_tfidf_score_is_symmetric_and_in_range():
    service = TfIdfService()
    first = "Built a Python REST API with SQL Server and C#."
    second = "Python and C# developer building REST APIs using SQL Server."

    forward = service.score(first, second)
    reverse = service.score(second, first)

    assert 0 < forward <= 1
    assert forward == pytest.approx(reverse)


def test_tfidf_keeps_technical_tokens_and_returns_zero_without_terms():
    service = TfIdfService()
    assert service.score("C# C++ .NET Node.js", "C# C++ .NET Node.js") == pytest.approx(1)
    assert service.score("!!!", "???") == 0


def test_tfidf_rejects_empty_text():
    with pytest.raises(ValueError, match="must not be empty"):
        TfIdfService().score(" ", "python")
