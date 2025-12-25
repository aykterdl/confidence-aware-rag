# RAG Demo Frontend

Demo-ready chat interface for the RAG (Retrieval Augmented Generation) system.

## Features

✅ **Chat Interface** - Clean, professional message display  
✅ **Confidence Indicators** - Visual badges (High/Low/None)  
✅ **Source Citations** - Expandable document references  
✅ **Multi-turn Conversations** - Automatic conversation tracking  
✅ **Language Aware** - Supports Turkish & English  
✅ **Loading States** - Visual feedback during processing  

---

## Quick Start

### 1. Install Dependencies

```bash
cd frontend/web
npm install
```

### 2. Start Development Server

```bash
npm run dev
```

The UI will be available at: **http://localhost:3000**

### 3. Ensure Backend is Running

Make sure the following services are running:

- **PostgreSQL + pgvector**: `localhost:5432`
- **Ollama**: `localhost:11434`
- **RAG API**: `localhost:5001`
- **API Gateway**: `localhost:8080`

Start backend with:

```bash
cd ../../
docker-compose up -d
```

---

## How to Use

1. **Open** http://localhost:3000
2. **Type** a question (Turkish or English)
3. **Press Enter** or click Send
4. **View**:
   - Answer from the RAG system
   - Confidence level badge
   - Source documents used
5. **Continue** the conversation - context is maintained

---

## Confidence Levels

| Level | Badge Color | Meaning |
|-------|-------------|---------|
| **High** | 🟢 Green | Strong document match, reliable answer |
| **Low** | 🟡 Yellow | Weak match, answer may be incomplete |
| **None** | 🔴 Red | No relevant documents found |

---

## Architecture

```
User Input
    ↓
Next.js Frontend (Port 3000)
    ↓
API Gateway (Port 8080)
    ↓
RAG.Api (Port 5001)
    ↓
PostgreSQL + Ollama
```

---

## API Integration

The frontend calls:

```
POST http://localhost:8080/api/rag/ask
```

Request:
```json
{
  "question": "string",
  "conversationId": "guid (optional)",
  "topK": 5
}
```

Response:
```json
{
  "answer": "string",
  "conversationId": "guid",
  "language": "tr | en",
  "confidence": {
    "level": "high | low | none",
    "maxSimilarity": 0.0,
    "averageSimilarity": 0.0,
    "explanation": "string"
  },
  "sources": [...]
}
```

---

## Project Structure

```
frontend/web/
├── app/
│   ├── page.tsx          # Main chat interface
│   ├── layout.tsx        # Root layout
│   └── globals.css       # Global styles
├── components/
│   ├── ChatMessage.tsx   # Message bubble component
│   ├── ChatInput.tsx     # Input field + send button
│   ├── ConfidenceBadge.tsx  # Confidence level indicator
│   └── SourcesPanel.tsx  # Document sources display
└── types/
    └── index.ts          # TypeScript type definitions
```

---

## Production Build

```bash
npm run build
npm start
```

---

## Notes

- ⚠️ This is a **DEMO** interface, not production-ready
- ⚠️ No authentication implemented
- ⚠️ No persistent storage on frontend
- ⚠️ API URL is hardcoded (can be configured)
- ✅ Backend is fully functional and production-ready
- ✅ Frontend focuses on clarity and demonstration

---

## Technology Stack

- **Next.js 16** - React framework (App Router)
- **TypeScript** - Type safety
- **Tailwind CSS** - Styling
- **React Hooks** - State management

---

## Demo Scenarios

### Turkish Question
```
Soru: Türk Medeni Kanunu madde 1 nedir?
Cevap: [Turkish answer with sources]
```

### English Question
```
Question: What is the summary of Turkish Civil Law article 1?
Answer: [English answer with sources]
```

### Low Confidence
```
Question: [Unrelated question]
Result: Yellow badge + cautious answer
```

### No Relevance
```
Question: [Completely irrelevant]
Result: Red badge + fallback message
```

---

**Ready to Demo!** 🚀


