# 🧠 Production-Grade RAG Knowledge System

> **A Clean Architecture RAG system with confidence-aware gating, source traceability, and balanced semantic retrieval**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Next.js](https://img.shields.io/badge/Next.js-16.1-000000?logo=nextdotjs)](https://nextjs.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql)](https://www.postgresql.org/)
[![pgvector](https://img.shields.io/badge/pgvector-0.3-orange)](https://github.com/pgvector/pgvector)

---

## 📋 Table of Contents

- [Overview](#-overview)
- [System Architecture](#-system-architecture)
- [Tech Stack](#-tech-stack)
- [RAG Pipeline](#-rag-pipeline)
- [Project Structure](#-project-structure)
- [API Endpoints](#-api-endpoints)
- [Getting Started](#-getting-started)
- [Project Status](#-project-status)
- [Design Principles](#-design-principles)

---

## 🔍 Overview

This is a **production-grade Retrieval-Augmented Generation (RAG)** system designed for high-stakes knowledge retrieval scenarios (legal, compliance, technical documentation).

### Key Features

✅ **PDF Document Ingestion** - Upload and process PDF documents automatically  
✅ **Semantic-Aware Chunking** - Intelligent text segmentation preserving context  
✅ **Vector Similarity Search** - Fast, accurate retrieval using PostgreSQL + pgvector  
✅ **Confidence-Aware Gating** - Prevents LLM invocation for irrelevant queries  
✅ **Source Traceability** - Every answer links back to original document chunks  
✅ **Balanced Prompting Strategy** - Guardrails against hallucination  
✅ **Clean Architecture** - Domain-driven design, CQRS, Hexagonal (Ports & Adapters)  
✅ **Modern UI** - React 19 + Next.js 16 with defensive rendering  

---

## 🏗️ System Architecture

### Clean Architecture Layers

```
┌─────────────────────────────────────────────────────────────┐
│                         API Layer                            │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  HTTP Endpoints │ Request Validation │ DI Setup    │    │
│  └─────────────────────────────────────────────────────┘    │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│                    Infrastructure Layer                      │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  EF Core │ Ollama │ PdfPig │ pgvector │ Adapters   │    │
│  └─────────────────────────────────────────────────────┘    │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│                    Application Layer                         │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  Use Cases │ Handlers │ DTOs │ Ports (Interfaces)  │    │
│  │  • SemanticSearch  • ComposePrompt  • GenerateAnswer│    │
│  └─────────────────────────────────────────────────────┘    │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│                       Domain Layer                           │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  Entities │ Value Objects │ Business Rules          │    │
│  │  • KnowledgeDocument  • ConfidenceScore             │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

### Hexagonal Architecture (Ports & Adapters)

| Port (Interface)           | Adapter (Implementation)       | Layer            |
|---------------------------|--------------------------------|------------------|
| `IDocumentRepository`      | `DocumentRepository`           | Infrastructure   |
| `IVectorSearchEngine`      | `PgVectorSearchEngine`         | Infrastructure   |
| `IEmbeddingGenerator`      | `OllamaEmbeddingGenerator`     | Infrastructure   |
| `ILanguageModel`           | `OllamaLanguageModel`          | Infrastructure   |
| `ITextExtractor`           | `PdfTextExtractor`             | Infrastructure   |
| `IChunkingStrategy`        | `SemanticChunkingStrategy`     | Infrastructure   |
| `IDocumentIngestionService`| `DocumentIngestionService`     | Infrastructure   |

---

## 🛠️ Tech Stack

### Backend
- **.NET 10.0** - Modern, high-performance runtime
- **ASP.NET Core Minimal APIs** - Lightweight HTTP endpoints
- **Entity Framework Core 10** - ORM with PostgreSQL provider
- **FluentValidation** - Input validation
- **PdfPig** - PDF text extraction

### Database
- **PostgreSQL 16** - Primary data store
- **pgvector 0.3** - Vector similarity search extension

### AI/ML
- **Ollama** - Local LLM inference
  - **llama3.2:1b** - Answer generation
  - **nomic-embed-text** - Embedding generation (768 dimensions)

### Frontend
- **Next.js 16.1.1** - React framework with Turbopack
- **React 19.2.3** - UI library
- **TypeScript** - Type safety
- **Tailwind CSS 4** - Styling

### DevOps
- **Docker & Docker Compose** - Containerization
- **PostgreSQL init scripts** - Database setup automation

---

## 🔄 RAG Pipeline

### Document Ingestion Flow

```
PDF Upload
    ↓
Text Extraction (PdfPig)
    ↓
Semantic Chunking (paragraph-first, sentence splitting, soft overlap)
    ↓
Embedding Generation (Ollama: nomic-embed-text)
    ↓
Persistence (PostgreSQL + pgvector)
```

### Question Answering Flow (3-Step Pipeline)

```
User Question
    ↓
STEP 1: Semantic Search
    • Generate query embedding (Ollama)
    • Vector similarity search (pgvector cosine distance)
    • Retrieve top-K relevant chunks
    • Calculate confidence score (Domain)
    ↓
STEP 2: Prompt Composition
    • Build system prompt (Balanced strategy guardrails)
    • Construct user prompt (query + context chunks)
    • Apply confidence-aware instructions
    ↓
STEP 3: Answer Generation
    • Confidence gating (if score < threshold → skip LLM)
    • Invoke LLM with composed prompt (Ollama: llama3.2)
    • Return answer + sources + confidence explanation
```

### Confidence Levels

| Level  | Behavior                                              |
|--------|-------------------------------------------------------|
| **None**  | No relevant content found → Skip LLM, return explanation |
| **Low**   | Partial relevance → Invoke LLM with cautious prompting |
| **High**  | Strong relevance → Invoke LLM with confident prompting |

---

## 📂 Project Structure

```
rag-demo/
├── backend/
│   ├── KnowledgeSystem.Api/              [API Layer - Endpoints, DI]
│   │   ├── Program.cs                    [Main entry point]
│   │   ├── appsettings.json              [Configuration]
│   │   └── Dockerfile
│   ├── KnowledgeSystem.Application/      [Application Layer - Use Cases]
│   │   ├── UseCases/
│   │   │   ├── SemanticSearch/           [Phase 4 Step 1]
│   │   │   ├── Prompting/                [Phase 4 Step 2]
│   │   │   └── GenerateAnswer/           [Phase 4 Step 3]
│   │   ├── Interfaces/                   [Ports]
│   │   │   ├── IDocumentRepository.cs
│   │   │   ├── IEmbeddingGenerator.cs
│   │   │   ├── ILanguageModel.cs
│   │   │   └── IVectorSearchEngine.cs
│   │   └── Services/
│   │       └── IDocumentIngestionService.cs
│   ├── KnowledgeSystem.Domain/           [Domain Layer - Business Logic]
│   │   ├── Entities/
│   │   │   ├── KnowledgeDocument.cs      [Aggregate Root]
│   │   │   └── ContentSection.cs         [Aggregate]
│   │   └── ValueObjects/
│   │       ├── ConfidenceScore.cs        [Business rule encapsulation]
│   │       ├── ConfidencePolicy.cs       [Domain policy]
│   │       ├── DocumentId.cs             [Strongly-typed ID]
│   │       └── SectionId.cs
│   ├── KnowledgeSystem.Infrastructure/   [Infrastructure Layer - Adapters]
│   │   ├── Persistence/                  [EF Core, PostgreSQL]
│   │   ├── VectorSearch/                 [pgvector adapter]
│   │   ├── Embedding/                    [Ollama embeddings]
│   │   ├── LanguageModel/                [Ollama LLM]
│   │   ├── TextExtraction/               [PdfPig]
│   │   ├── Chunking/                     [Semantic chunking]
│   │   └── Services/
│   │       └── DocumentIngestionService.cs
│   └── KnowledgeSystem.Application.Tests/ [Unit Tests]
├── frontend/
│   └── web/                              [Next.js 16 + React 19]
│       ├── app/
│       │   └── page.tsx                  [Main chat interface]
│       ├── components/
│       │   ├── ChatMessage.tsx
│       │   ├── ChatInput.tsx
│       │   ├── SourcesPanel.tsx
│       │   ├── ConfidenceBadge.tsx
│       │   └── PdfUpload.tsx
│       └── types/
│           └── index.ts                  [TypeScript type definitions]
├── database/
│   └── postgres/
│       └── init.sql                      [pgvector extension setup]
├── docker-compose.yml                    [Service orchestration]
└── README.md
```

---

## 🌐 API Endpoints

### Production Endpoints

| Method | Endpoint                   | Purpose                          | Status |
|--------|----------------------------|----------------------------------|--------|
| POST   | `/api/documents/ingest`    | Upload & process PDF documents   | ✅ Active |
| POST   | `/api/query/answer`        | RAG question answering (3-step)  | ✅ Active |
| POST   | `/api/query/semantic-search` | Semantic retrieval only (debug) | ✅ Active |

### Monitoring Endpoints

| Method | Endpoint               | Purpose                      | Status |
|--------|------------------------|------------------------------|--------|
| GET    | `/health`              | Database connectivity check  | ✅ Active |
| GET    | `/api/documents/count` | Total document count (debug) | ✅ Active |

### Request/Response Examples

#### Upload PDF Document
```bash
curl -X POST http://localhost:8080/api/documents/ingest \
  -F "file=@document.pdf" \
  -F "title=My Document"
```

**Response:**
```json
{
  "success": true,
  "documentId": "123e4567-e89b-12d3-a456-426614174000",
  "title": "My Document",
  "chunkCount": 42,
  "characterCount": 15230,
  "pageCount": 8,
  "message": "Document successfully ingested with 42 semantic chunks"
}
```

#### Ask Question
```bash
curl -X POST http://localhost:8080/api/query/answer \
  -H "Content-Type: application/json" \
  -d '{
    "query": "What is the main topic of this document?",
    "topK": 5,
    "language": "en"
  }'
```

**Response:**
```json
{
  "answer": "The document primarily discusses...",
  "sources": [
    {
      "chunkId": "abc-123",
      "documentId": "doc-456",
      "documentTitle": "My Document",
      "content": "This is the relevant chunk content...",
      "similarityScore": 0.8523,
      "sectionType": "paragraph"
    }
  ],
  "confidence": "high",
  "confidenceExplanation": "Strong match found (85.2% similarity). The answer is based on highly relevant content.",
  "sourceCount": 5,
  "llmInvoked": true
}
```

---

## 🚀 Getting Started

### Prerequisites

- **Docker** and **Docker Compose** installed
- **Ollama** installed locally (or accessible via network)
  - Pull models: `ollama pull llama3.2:1b` and `ollama pull nomic-embed-text`

### Quick Start

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd rag-demo
   ```

2. **Start services**
   ```bash
   docker-compose up -d
   ```

   This starts:
   - PostgreSQL (port 5432) with pgvector extension
   - Backend API (port 8080)
   - Frontend UI (port 3000)

3. **Verify Ollama is running**
   ```bash
   ollama list  # Should show llama3.2:1b and nomic-embed-text
   ```

4. **Access the application**
   - **Frontend UI:** http://localhost:3000
   - **Backend API:** http://localhost:8080
   - **Health Check:** http://localhost:8080/health

5. **Upload a PDF**
   - Use the frontend UI to upload a PDF document
   - Wait for ingestion to complete
   - Ask questions in the chat interface

---

## 📊 Project Status

### ✅ Completed Phases

- **Phase 1-3:** Clean Architecture foundation, Domain/Application/Infrastructure layers
- **Phase 4:** Full RAG pipeline (Semantic Search → Prompt Composition → Answer Generation)
- **Phase 5 – Step 1:** Legacy cleanup (37 files removed, 63% code reduction in Program.cs)

### 🚧 Phase 5 – Step 2 (In Progress)

- [x] README.md update
- [ ] UI/UX improvements (source cards, dark mode, typing effect)
- [ ] Backend quality tuning (logging, safety)
- [ ] Performance optimizations

### 🔮 Planned Features

- **Streaming Answers:** Real-time token-by-token response
- **Caching Layer:** Redis for repeated queries
- **Rate Limiting:** API throttling and abuse prevention
- **Observability:** OpenTelemetry integration
- **API Versioning:** `/v1/` prefix
- **Multi-document Cross-Reference:** Link related content across documents

---

## 🎯 Design Principles

### Architectural Rules

1. **Dependency Rule:** Dependencies point inward (Infrastructure → Application → Domain)
2. **No Leakage:** Domain layer has ZERO external dependencies
3. **Ports & Adapters:** All external systems accessed via interfaces
4. **Explicit Mapping:** No AutoMapper, manual mapping for clarity
5. **CQRS:** Commands and Queries separated

### RAG Strategy

1. **Confidence-Aware Gating:** LLM not called if relevance < threshold
2. **Source Traceability:** Every answer includes original document chunks
3. **Balanced Prompting:** Strict guardrails against hallucination
4. **No External Knowledge:** LLM only uses provided context
5. **Explicit Uncertainty:** Low confidence → cautious language

### Code Quality

1. **Defensive Programming:** Null checks, input validation, graceful degradation
2. **Type Safety:** Strongly-typed IDs, value objects
3. **Immutability:** Value objects are immutable
4. **Clear Intent:** Descriptive naming, no abbreviations
5. **Fail Fast:** Validation errors thrown early

---

## 📝 Configuration

### Backend (appsettings.json)

```json
{
  "ConnectionStrings": {
    "KnowledgeDb": "Host=postgres;Port=5432;Database=knowledgeDB;Username=raguser;Password=ragpass"
  },
  "Ollama": {
    "BaseUrl": "http://ollama:11434",
    "Embeddings": {
      "Model": "nomic-embed-text",
      "TimeoutSeconds": 60
    },
    "LanguageModel": {
      "Model": "llama3.2:1b",
      "TimeoutSeconds": 120
    }
  },
  "RagConfidence": {
    "MinAnswerSimilarity": 0.04,
    "LowConfidenceThreshold": 0.06
  }
}
```

---

## 👤 Author

**Aykut Erdal**  
Software Engineer | RAG & AI Systems  
Clean Architecture & Domain-Driven Design Advocate

---

## 📄 License

This project is a demonstration of Clean Architecture principles applied to RAG systems.  
All code is provided as-is for educational and reference purposes.

---

## 🔗 Related Documentation

- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Hexagonal Architecture (Ports & Adapters)](https://alistair.cockburn.us/hexagonal-architecture/)
- [pgvector Documentation](https://github.com/pgvector/pgvector)
- [Ollama Documentation](https://ollama.ai/docs)

---

**Last Updated:** January 2026  
**Version:** Phase 5 – Step 2 (Quality & UX Improvements)
