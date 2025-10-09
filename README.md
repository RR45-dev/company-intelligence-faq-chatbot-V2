# Enterprise Intelligence FAQ Chatbot
*A production-grade, full-stack RAG starter using React + .NET 8 + Qdrant + OpenAI*

This repository delivers a complete Retrieval-Augmented Generation (RAG) system that ingests company knowledge (PDF/TXT), stores embeddings in a vector database, and serves grounded answers through a modern chat UI.

> **What this is:** a clean architecture you can demo to founders/CTOs/investors and extend into a real product.

---

1 Concept

Modern businesses deal with endless customer questions:

“Where’s my order?”
“How do I reset my password?”
“What’s your refund policy?”

Most of these answers already exist — in FAQs, help-center articles, or support tickets — but customers still wait in queues or search manually.
This chatbot automates that.
It reads your FAQ documents, support guides, and policy pages, then answers customer questions instantly using a mix of:
Retrieval-Augmented Generation (RAG) for factual accuracy
Vector Database Search for meaning-based retrieval
Large Language Models (GPT-4o) for conversational responses

It’s like giving every customer their own smart assistant — one that always gives the right answer, the same way your brand would.

2️ Use Case Examples
Industry	Example Questions
E-commerce	“How do I track my order?” / “Can I return a sale item?”
SaaS / Tech	“How do I reset my account password?” / “What’s included in the Pro plan?”
Education	“How do I submit my assignment?” / “What’s the refund policy for courses?”
Finance / Banking	“How can I update my KYC?” / “Is there a fee for early withdrawals?”

---

## Architecture
```mermaid
flowchart LR
  A[User in React Chat UI] -- /api/chat --> B[.NET 8 Web API]
  A -- /api/ingest (PDF/TXT) --> D[IngestController]
  subgraph API & Services
    B --> E[EmbeddingService]
    B --> F[QdrantService]
    B --> G[LlmService]
    D --> H[DocumentParser + Chunker]
    H --> E
    E --> F
  end
  F <--> I[(Qdrant Vector DB)]
  E <--> J[(OpenAI Embeddings)]
  G <--> K[(OpenAI LLM)]
  B --> A
```

Also included as a PNG: `docs/architecture.png`

![Architecture](docs/architecture.png)

**Design choices**
- Qdrant (cosine, 3072-dim) with OpenAI embeddings
- Thin controllers, testable services
- Stateless API; add Redis cache later

---

## Repository Layout
```
.
├─ backend/ (ASP.NET Core 8 API)
├─ frontend/ (React + Vite)
├─ docs/ (architecture.png, api.http)
├─ sample-data/ (seed docs to ingest)
├─ docker-compose.yml
└─ .env.example
```

---


Customer Workflow
Step-by-Step Flow

Upload Knowledge Base

Upload your FAQ pages, policy PDFs, or helpdesk articles
The system automatically splits text into small sections (chunks), embeds them using OpenAI’s text-embedding-3-large, and stores them in a vector database (Qdrant or Pinecone)

Customer Asks a Question

The chatbot UI captures their message
The backend converts the question into an embedding and searches for the most semantically similar chunks

Retrieve & Combine

The system retrieves the top matching snippets from your FAQs or support docs
These snippets form a “context”

AI Generates the Answer
GPT-4o uses the context to produce a clear, conversational response — fact-based, brand-aligned, and on-tone

Response Display

The React chat UI displays the answer instantly
Optional: add thumbs-up/down feedback or follow-up actions (“Would you like to start a return?”)

## Quickstart (Local Dev)
**Qdrant**
```bash
docker compose up -d qdrant
```

**API**
```bash
cd backend/src/CompanyIntelligence.Api
dotnet restore
export OPENAI_API_KEY=sk-...
export QDRANT_URL=http://localhost:6333
export QDRANT_COLLECTION=company_knowledge
export OPENAI_MODEL=gpt-4o-mini
export OPENAI_EMBEDDING_MODEL=text-embedding-3-large
dotnet run
# Swagger: http://localhost:5088/swagger
```

**Web**
```bash
cd frontend
echo "VITE_API_BASE=http://localhost:5088" > .env
npm i
npm run dev
# http://localhost:5173
```

**Try it**
- Upload `sample-data/company_policy_example.txt`
- Ask: "What's our refund policy for premium users?"

---

## Environment Variables
**Backend**
- OPENAI_API_KEY (required)
- OPENAI_MODEL (default gpt-4o-mini)
- OPENAI_EMBEDDING_MODEL (default text-embedding-3-large)
- QDRANT_URL (default http://localhost:6333)
- QDRANT_COLLECTION (default company_knowledge)

**Frontend**
- VITE_API_BASE (default http://localhost:5088)

---

## REST API
- `POST /api/ingest` (multipart `file`) -> chunks+embeds and upserts to Qdrant
- `POST /api/chat` -> returns answer + sources

See `docs/api.http` for runnable requests.

---

## Production Tips
- Add JWT/Auth0 auth and RBAC
- Redis cache for frequent Q&A
- Hybrid search + cross-encoder rerank
- Rate limiting and audit logs
- Deploy with docker-compose or separate services

---

## License
MIT
