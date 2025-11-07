# AI Reviews RAG — Production Ready Product QnA

A retrieval optimized backend that answers product questions from thousands of reviews.
This README explains concepts, use cases, architecture, setup, API, and where to extend the repo.

## What this is
- Clean reference for building a reviews powered QnA system
- Low latency and grounded answers
- Ready for demos and production hardening

## Use cases
- Electronics: battery life, thermals, camera
- Appliances: noise at night, reliability
- Wearables: sensor accuracy
- Gaming gear: heat after long sessions

## Architecture
See the diagram at `docs/architecture.png`.

### Flow
- Ingest: chunk -> embed -> index (FAISS or Pinecone)
- Query: embed -> vector search -> optional rerank -> grounded prompt -> LLM
- Cache: Redis optional

## Repo layout
```
src/ ... core code
scripts/ingest_from_jsonl.py ... build the index from JSONL
docs/architecture.png ... system diagram
docs/api.http ... runnable REST examples
data/sample_reviews.jsonl ... sample input
```
Add your own modules here:
- `src/ingest/db_reader.py` for DB connectors
- `src/retrieval/pinecone_store.py` for Pinecone adapter
- `frontend/` (new) for a React chat UI
- `jobs/` (new) for cache warmers
- `evals/` (new) for quality evaluation

## Quickstart
```bash
cp .env.example .env
pip install -r requirements.txt
python scripts/ingest_from_jsonl.py data/sample_reviews.jsonl --product-id demo-123
uvicorn src.app.main:app --reload --port 8000
```
Open http://localhost:8000/docs

## API
See `docs/api.http` or use curl:
```bash
curl -s -X POST localhost:8000/ask -H "Content-Type: application/json"   -d '{"query":"How is the battery life?","product_id":"demo-123"}'
```

## Configuration
- OPENAI_API_KEY required
- VECTOR_BACKEND: faiss or pinecone
- Optional: Redis cache and cross encoder reranking

## Production notes
- Enable auth and rate limits
- Add observability (metrics, tracing)
- Use Pinecone for managed vector search
- Cache frequent QnA to control cost

## Roadmap
See ROADMAP.md

## Contributing and Security
See CONTRIBUTING.md and SECURITY.md

## License
MIT
