from collections.abc import Sequence

from sklearn.metrics import (
    accuracy_score,
    confusion_matrix,
    mean_absolute_error,
    precision_recall_fscore_support,
)

from app.schemas.requests import EvaluationPairRequest
from app.services.matching_service import MatchingService

_LABELS = [0, 1, 2, 3]
_LIMITATION = (
    "This endpoint evaluates fixed score thresholds; it does not train a model. "
    "Thresholds should be validated on a separate, representative human-labelled set."
)


class EvaluationService:
    @staticmethod
    def score_to_label(score: float) -> int:
        if score < 0.25:
            return 0
        if score < 0.50:
            return 1
        if score < 0.75:
            return 2
        return 3

    def evaluate(
        self,
        pairs: Sequence[EvaluationPairRequest],
        matcher: MatchingService,
    ) -> dict[str, object]:
        actual = [pair.label for pair in pairs]
        results = [matcher.analyze(pair) for pair in pairs]
        scores = [float(result["final_score"]) for result in results]
        predicted = [self.score_to_label(score) for score in scores]
        precision, recall, f1, _support = precision_recall_fscore_support(
            actual, predicted, labels=_LABELS, average="macro", zero_division=0
        )

        return {
            "accuracy": float(accuracy_score(actual, predicted)),
            "macro_precision": float(precision),
            "macro_recall": float(recall),
            "macro_f1": float(f1),
            "mean_absolute_error": float(mean_absolute_error(actual, predicted)),
            "confusion_matrix": confusion_matrix(
                actual, predicted, labels=_LABELS
            ).tolist(),
            "predictions": [
                {
                    "actual_label": actual_label,
                    "predicted_label": predicted_label,
                    "final_score": score,
                }
                for actual_label, predicted_label, score in zip(
                    actual, predicted, scores, strict=True
                )
            ],
            "score_to_label_mapping": {
                "0": "score < 0.25",
                "1": "0.25 <= score < 0.50",
                "2": "0.50 <= score < 0.75",
                "3": "score >= 0.75",
            },
            "limitation": _LIMITATION,
        }
