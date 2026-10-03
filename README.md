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

### NLP Service

```bash
cd nlp-service
python -m venv .venv
source .venv/bin/activate  # Windows: .venv\Scripts\activate
pip install -r requirements.txt
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

## Docker

```bash
docker-compose up --build
```
