# Resume Matcher Capstone Project

This repository contains the full project structure for the resume matching application across frontend, backend, NLP service, ML experimentation, data management, and documentation.

## Project structure

- `frontend/resume-matcher-ui` – React UI for candidate and job matching workflows
- `backend/ResumeMatcher.API` – ASP.NET Core API for application endpoints and business logic
- `nlp-service` – Python NLP microservice for text preprocessing, matching, and scoring
- `data` – raw and processed datasets
- `notebooks` – exploratory analysis and experimentation notebooks
- `ml` – trained models and experiment artifacts
- `docs` – architecture, methodology, and evaluation documents

## Run locally

### Frontend

```bash
cd frontend/resume-matcher-ui
npm install
npm run dev
```

### Backend

```bash
dotnet restore
cd backend/ResumeMatcher.API
dotnet run
```

### API and matching

The API uses Dapper with the SQL Server schema in `database/create_database.sql`. Run that script before using database-backed routes; it also adds the `NeedsReview` flag used for human review of extracted job skills. Start the NLP service as well for matching and evaluation routes. The NLP service loads `sentence-transformers/all-MiniLM-L6-v2` once during startup; its first startup downloads the model if it is not already cached.

- Job descriptions: `POST/GET /api/job-descriptions`, `GET/PUT/DELETE /api/job-descriptions/{id}`, `POST /api/job-descriptions/{id}/extract-skills`. Review or edit extracted skill importance using `PUT /api/job-descriptions/{id}/skills` with a `skills` array of `{ "skillName": "...", "importance": "Required|Preferred" }`.
- Skills: `GET/POST /api/skills`, `PUT /api/skills/{id}`, `GET /api/skills/categories`, `GET /api/skills/search?query=...`.
- Matching: `POST/GET /api/matches`, `GET /api/matches/{id}`, `GET /api/matches/{id}/skill-gaps`, and `GET /api/matches/compare?resumeId=1&jobDescriptionId=2`.
- Evaluation: `POST/GET /api/evaluations/pairs`, `PUT /api/evaluations/pairs/{id}`, `POST /api/evaluations/run`, `GET /api/evaluations/{id}/results`, and `GET /api/evaluations/summary`.
- Reporting: `GET /api/dashboard/summary`, `GET /api/reports/matches/{id}`, and `GET /api/reports/evaluations/{id}`.

The final match score is `0.50 × normalized Sentence-BERT cosine similarity + 0.50 × weighted skill coverage` (required skill weight 1.0, preferred 0.5). The normalized semantic score is `(cosine + 1) / 2`; neither it nor the final ranking score is a calibrated probability. TF-IDF cosine similarity is reported as a baseline. Evaluation maps final scores to ordinal labels with thresholds 0.25, 0.50, and 0.75 and reports accuracy, macro-F1, and mean absolute error for the final score and each component. These thresholds should be validated on a separate labelled evaluation set.

Before matching, extract job skills with `POST /api/job-descriptions/{id}/extract-skills` and review any entries with `needsReview: true`; the matching pipeline refreshes resume skill extraction automatically.

### NLP Service

```bash
cd nlp-service
python -m venv .venv
source .venv/bin/activate  # Windows: .venv\Scripts\activate
pip install -r requirements.txt
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

The NLP API provides `/health`, `/nlp/extract-skills`, `/nlp/tfidf-score`,
`/nlp/semantic-score`, `/nlp/skill-coverage`, `/nlp/analyze`, and `/nlp/evaluate`.
The original `/similarity` route remains for backend compatibility. See
[`nlp-service/README.md`](./nlp-service/README.md) for request/response details,
the score formula, configuration, and tests. Run tests with
`python -m pytest` from `nlp-service`.

## Docker

```bash
docker-compose up --build
```
