📘 Confidence-Aware \& Citation-Enforced RAG System

🔍 Overview



This project is a production-grade Retrieval-Augmented Generation (RAG) system

designed for high-risk domains such as legal documents.



The system prioritizes:



Accuracy



Source traceability



Confidence transparency



Hallucination prevention



🚀 Key Features



✅ Document upload (PDF / text)



✅ Semantic text chunking (legal article-aware)



✅ Vector search with pgvector



✅ Citation-enforced answers



✅ Confidence-aware responses (high / low / none)



✅ Relevance gating (LLM not called if irrelevant)



✅ Multi-turn conversation support



✅ Turkish \& English language awareness



✅ Demo-ready chat UI



🧠 RAG Pipeline

Document Upload

→ Semantic Chunking

→ Embedding Generation (Ollama)

→ PostgreSQL + pgvector

→ Similarity Search

→ Confidence Gating

→ Citation-Aware Prompting

→ LLM Response



🛡️ Confidence Levels

Level	Behavior

none	No relevant content → no LLM call

low	Partial relevance → cautious answer

high	Strong relevance → confident answer

🔧 Technologies



Backend: ASP.NET Core (.NET)



Database: PostgreSQL + pgvector



LLM \& Embeddings: Ollama



Containerization: Docker



Frontend: Demo Chat UI



Language Support: Turkish / English



📂 Project Structure (Simplified)

backend/

&nbsp;├── RAG.Api

&nbsp;│   ├── Services

&nbsp;│   ├── Models

&nbsp;│   ├── Configuration

&nbsp;│   └── Controllers

docker-compose.yml

README.md



🎯 Design Principles



No hallucination



Source-backed answers only



Explicit uncertainty



Legal/compliance-friendly



Production-first mindset



📌 Status



✅ Actively developed

✅ Demo-ready

🚧 Advanced features planned (streaming, auth, Redis, UI polish)



👤 Author



Developed by Aykut Erdal

Software Engineer | RAG \& AI Systems

