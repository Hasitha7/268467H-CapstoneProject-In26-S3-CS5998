from app.services.evaluation_service import EvaluationService


class FakeMatcher:
    def analyze(self, pair):
        return {"final_score": pair.job_description}


def test_fixed_threshold_label_mapping():
    mapping = EvaluationService.score_to_label
    assert [mapping(score) for score in [0, 0.249, 0.25, 0.499, 0.5, 0.749, 0.75, 1]] == [
        0, 0, 1, 1, 2, 2, 3, 3
    ]


def test_evaluation_returns_metrics_and_fixed_four_by_four_matrix():
    from app.schemas.requests import EvaluationPairRequest

    pairs = [
        EvaluationPairRequest(
            resume_text="resume",
            job_description=score,
            label=label,
        )
        for score, label in [
            ("0.10", 0),
            ("0.30", 1),
            ("0.60", 2),
            ("0.90", 3),
        ]
    ]
    result = EvaluationService().evaluate(pairs, FakeMatcher())

    assert result["accuracy"] == 1
    assert result["macro_precision"] == 1
    assert result["macro_recall"] == 1
    assert result["macro_f1"] == 1
    assert result["mean_absolute_error"] == 0
    assert result["confusion_matrix"] == [
        [1, 0, 0, 0],
        [0, 1, 0, 0],
        [0, 0, 1, 0],
        [0, 0, 0, 1],
    ]
    assert "does not train" in result["limitation"]
