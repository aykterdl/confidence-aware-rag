from fastapi import FastAPI
from schemas import QuestionRequest
from rag_engine import ask_question

app = FastAPI()

@app.post("/ask")
def ask(req: QuestionRequest):
    return ask_question(req.question)
