from functools import lru_cache

from fastapi import FastAPI
from pydantic import BaseModel, Field
from sentence_transformers import SentenceTransformer
from sentence_transformers.util import cos_sim

app = FastAPI(title="Resume Matcher NLP Service")


class SimilarityRequest(BaseModel):
    text1: str = Field(min_length=1, max_length=200_000)
    text2: str = Field(min_length=1, max_length=200_000)


@lru_cache(maxsize=1)
def get_sentence_model():
    return SentenceTransformer("sentence-transformers/all-MiniLM-L6-v2", device="cpu")


@app.post("/similarity")
def semantic_similarity(request: SimilarityRequest):
    model = get_sentence_model()
    embeddings = model.encode(
        [request.text1, request.text2],
        convert_to_tensor=True,
        normalize_embeddings=True,
    )
    cosine_score = float(cos_sim(embeddings[0:1], embeddings[1:2]).item())
    cosine_score = max(-1.0, min(1.0, cosine_score))
    return {
        "cosineScore": cosine_score,
        "similarity": (cosine_score + 1.0) / 2.0,
        "modelVersion": "sentence-transformers-all-MiniLM-L6-v2",
    }


@app.get("/health")
def health_check():
    return {"status": "ok", "service": "nlp"}


@app.get("/")
def root():
    return {"message": "Resume Matcher NLP service is running."}
