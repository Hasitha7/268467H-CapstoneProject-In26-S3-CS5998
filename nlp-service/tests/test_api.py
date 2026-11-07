from fastapi.testclient import TestClient

from app.config import Settings
from app.main import create_app


def test_health_and_skill_extraction_endpoints(api_client):
    health = api_client.get("/health")
    assert health.status_code == 200
    assert health.json()["model_loaded"] is True
    assert health.json()["model_loading_status"] == "loaded"

    response = api_client.post(
        "/nlp/extract-skills",
        json={"text": "Developed REST APIs using Python Flask and SQL Server."},
    )
    assert response.status_code == 200
    assert [skill["name"] for skill in response.json()["skills"]] == [
        "REST API", "Python", "Flask", "SQL Server"
    ]


def test_similarity_coverage_and_analysis_endpoints(api_client):
    body = {
        "resume_text": "same text Python SQL",
        "job_description": "same text Python SQL",
    }
    tfidf = api_client.post("/nlp/tfidf-score", json=body)
    semantic = api_client.post("/nlp/semantic-score", json=body)
    analysis = api_client.post("/nlp/analyze", json=body)
    coverage = api_client.post(
        "/nlp/skill-coverage",
        json={
            "resume_skills": ["Python", "SQL"],
            "job_skills": [
                {"name": "Python", "importance": "Required"},
                {"name": "SQL", "importance": "Required"},
                {"name": "Azure", "importance": "Preferred"},
            ],
        },
    )

    assert tfidf.status_code == semantic.status_code == analysis.status_code == 200
    assert 0 <= tfidf.json()["score"] <= 1
    assert semantic.json()["score"] == 1
    assert analysis.json()["final_score"] == 1
    assert analysis.json()["missing_required_skills"] == []
    assert analysis.json()["missing_preferred_skills"] == []
    assert coverage.status_code == 200
    assert coverage.json()["missing_preferred_skills"] == ["Microsoft Azure"]
    assert coverage.json()["overall_skill_coverage_score"] == 0.8


def test_evaluate_endpoint_reports_metrics_and_legacy_similarity_contract(api_client):
    response = api_client.post(
        "/nlp/evaluate",
        json={
            "pairs": [
                {
                    "resume_text": "same text Python",
                    "job_description": "same text Python",
                    "label": 3,
                }
            ]
        },
    )
    legacy = api_client.post(
        "/similarity", json={"text1": "same text", "text2": "same text"}
    )

    assert response.status_code == 200
    assert len(response.json()["confusion_matrix"]) == 4
    assert response.json()["mean_absolute_error"] == 0
    assert legacy.status_code == 200
    assert legacy.json()["cosineScore"] == 1
    assert legacy.json()["similarity"] == 1


def test_invalid_or_empty_inputs_are_rejected(api_client):
    empty = api_client.post("/nlp/tfidf-score", json={
        "resume_text": " ",
        "job_description": "python",
    })
    bad_label = api_client.post("/nlp/evaluate", json={
        "pairs": [{
            "resume_text": "python",
            "job_description": "python",
            "label": 4,
        }]
    })

    assert empty.status_code == 422
    assert bad_label.status_code == 422


def test_text_over_configured_limit_is_rejected(api_client):
    response = api_client.post(
        "/nlp/tfidf-score",
        json={"resume_text": "x" * 200_001, "job_description": "python"},
    )
    assert response.status_code == 422


def test_semantic_endpoints_return_503_when_model_is_unavailable():
    settings = Settings(
        load_model_at_startup=False,
        max_text_length=200_000,
        tfidf_weight=0,
        semantic_weight=0.5,
        skill_weight=0.5,
    )
    application = create_app(settings, load_model_at_startup=False)
    with TestClient(application) as client:
        assert client.get("/health").json()["model_loaded"] is False
        response = client.post(
            "/nlp/semantic-score",
            json={"resume_text": "python", "job_description": "python"},
        )
    assert response.status_code == 503
