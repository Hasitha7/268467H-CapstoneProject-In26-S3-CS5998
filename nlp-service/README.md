# Resume Matcher NLP Service

FastAPI service for text similarity, technical-skill extraction and matching,
skill coverage, and evaluation against human-labelled resume/JD pairs. Python
performs NLP only; SQL Server access, business logic, and persistence stay in
the ASP.NET Core API.

## Requirements and startup

Python 3.11 or newer is required. From this directory:

```powershell
python -m venv .venv
.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
Copy-Item .env.example .env
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

The semantic model (`sentence-transformers/all-MiniLM-L6-v2`) is loaded once
when the application starts. Its first download requires network access. Set
`NLP_MODEL_CACHE_DIR` to retain the model at a chosen location and
`NLP_DEVICE` to select the inference device (defaults to CPU). If model loading
fails, the service logs the error, remains available for health, TF-IDF, and
skill routes, and responds with HTTP 503 on semantic-dependent routes.

`GET /health` reports whether the semantic model loaded and its configured
version. The root-level `main.py` is kept as the Uvicorn import target used by
the existing Docker Compose configuration; the application implementation is
in `app/main.py`.

## API

- `GET /health`
- `POST /nlp/extract-skills` — body `{"text":"..."}`; returns unique normalized
  skill names and categories.
- `POST /nlp/tfidf-score` and `POST /nlp/semantic-score` — body
  `{"resume_text":"...","job_description":"..."}`; return a score in `[0,1]`.
- `POST /nlp/skill-coverage` — compares `resume_skills` against `job_skills`,
  each of which has a name and `Required` or `Preferred` importance.
- `POST /nlp/analyze` — complete analysis; when lists are omitted or empty,
  skills are extracted from each text. Automatically extracted job skills are
  treated as Required because importance cannot be inferred reliably; clients
  should send reviewed `job_skills` to express Preferred requirements.
- `POST /nlp/evaluate` — accepts 1–500 labelled pairs with integer labels from
  0 through 3 and returns classification and ordinal-error metrics.

Text input is normalized for Unicode and whitespace without stripping
technical punctuation (for example `C#`, `C++`, `.NET`, or `Node.js`). Empty
text and inputs longer than `NLP_MAX_TEXT_LENGTH` are rejected. Logs include
operation counts but never resume or job-description contents.

The original `POST /similarity` route is retained for compatibility with the
existing ASP.NET Core client. It returns raw cosine as `cosineScore` and the
normalized score as `similarity`.

## Scores and limitations

TF-IDF uses lexical unigram/bigram cosine similarity. Semantic score is
`(Sentence-BERT cosine + 1) / 2`; it is a normalized similarity score, **not a
probability**. The legacy `cosineScore` remains the raw cosine for compatibility.

The default final ranking score is:

```text
0.0 × TF-IDF + 0.5 × normalized semantic score + 0.5 × weighted skill coverage
```

Weights are configurable with `NLP_TFIDF_WEIGHT`, `NLP_SEMANTIC_WEIGHT`, and
`NLP_SKILL_WEIGHT`; they must be finite, non-negative, and sum to 1. Skill
coverage uses weight 1.0 for each Required skill and 0.5 for each Preferred
skill. Required coverage with no Required skills is 1.0; overall coverage with
no listed skills is 0.0. A missing resume-text mention is only a missing
mention for matching purposes, not evidence that the candidate lacks the skill.
Review automatically extracted skills and priorities before making decisions.

Evaluation maps final scores to labels with fixed boundaries: `<0.25` → 0,
`[0.25, 0.50)` → 1, `[0.50, 0.75)` → 2, and `>=0.75` → 3. This endpoint
does not train or tune the model. Reported metrics measure performance only on
the supplied labelled examples; thresholds need validation against a separate,
representative human-labelled set.

The initial skill lexicon is in `app/data/skills.json`; aliases are normalized
to canonical skill names. The `.NET Core` to `ASP.NET Core` equivalence is only
used when nearby web, ASP.NET, MVC, or Web API context supports that reading.

## Tests

The suite uses a small fake semantic model and does not download the Sentence-
BERT model:

```powershell
pytest
```

Run the API locally and open `http://localhost:8000/docs` for the interactive
OpenAPI UI.
