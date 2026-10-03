from fastapi import FastAPI

app = FastAPI(title="Resume Matcher NLP Service")


@app.get("/health")
def health_check():
    return {"status": "ok", "service": "nlp"}


@app.get("/")
def root():
    return {"message": "Resume Matcher NLP service is running."}
