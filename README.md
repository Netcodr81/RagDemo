# RagDemo

A full end-to-end Retrieval Augmented Generation (RAG) demo showcasing:
- Document ingestion (Markdown/PDF → embeddings)
- Vector similarity search (**Postgres + pgvector** via Semantic Kernel `VectorStore`)
- Hybrid RAG and HyDE style query flows
- Local LLM interaction via Ollama (chat, generation, embeddings)
- Blazor interactive UI for search, chat, and experimentation
- Shared kernel abstractions for models/constants/services

## Project Structure & Purpose
| Project | Purpose |
|---------|---------|
| `SemanticSearchApi` | ASP.NET Core API exposing semantic search, RAG (simple + HyDE), and chat endpoints using Ollama + a Postgres/pgvector vector store. |
| `Rag.VectorBaseInitializer` | Console/Kernel app that builds the initial document vector store: reads Markdown files, generates embeddings, and writes them to Postgres/pgvector. |
| `Rag.UI` | Blazor Server interactive UI for querying semantic search, simple RAG, HyDE RAG, and chat-based interactions. Tailwind + Flowbite for styling. |
| `SharedKernel` | Cross-project shared constants (e.g. model IDs), response models, and abstractions. |
| `Python Scripts/docling_script.py` | Optional OCR + PDF → Markdown pipeline using Docling + fallback OCR engines for preparing additional content. |

## High-Level Flow
1. Source content (Markdown / optionally converted PDFs) is embedded using Ollama's `nomic-embed-text` model.
2. Embeddings are stored in **Postgres** using the **pgvector** extension (Semantic Kernel `VectorStore`).
3. Queries generate embeddings, perform vector similarity search in the vector store, retrieve top documents.
4. For HyDE, a hypothetical answer is synthesized first to refine retrieval.
5. Retrieved context is assembled and sent to the chat / text generation model (`llama3.2:1b`) to produce an answer.

## Prerequisites
Ensure the following are installed locally:
- **.NET SDK** (Preview/RC supporting `net10.0` target) – if unavailable, install the latest from: https://dotnet.microsoft.com/download
- **Docker** (for Postgres/pgvector) – https://docs.docker.com/get-docker/
- **Ollama** (local LLM runtime) – https://ollama.com/download
- **Node.js + npm** (for Tailwind build) – https://nodejs.org/
- **Python 3.11+** (optional OCR/Docling pipeline) – https://www.python.org/downloads/

## 1. Start Postgres (Vector Database)
From repository root:
```powershell
docker compose up -d
```
This starts Postgres at `localhost:5432`.

### Vector DB Details (pgvector)
- Container image: `pgvector/pgvector:pg17`
- Database: `vector_db`
- User/Password: `postgres` / `postgres`
- Data persists in a named Docker volume (`pgdata`)

> Note: `docker-compose.yml` mounts `./postgres/schema.sql` into `/docker-entrypoint-initdb.d/`. If you haven’t created the schema file yet, the container will still run, but tables/extension may not be initialized.

## 2. Install & Pull Ollama Models
Install Ollama (see link above) then pull required models:
```powershell
ollama pull nomic-embed-text:latest
ollama pull llama3.2:1b
```
If you change models, update `SharedKernel/Constants/OllamaModels.cs` accordingly.

Ollama Docs: https://ollama.com/library

## 3. Build Vector Store (Initial Indexing)
Run the vector base initializer to:
- Verify Ollama is running
- Ensure the vector store collection exists
- Read Markdown files under `src/Rag.VectorBaseInitializer/Markdown`
- Generate embeddings and persist them to Postgres/pgvector

From repository root:
```powershell
cd .\src\Rag.VectorBaseInitializer
 dotnet run
```
You should see logs indicating each document insertion.

## 4. Run the Semantic Search API
The API uses the same vector database as the initializer.

### Configuration
In `src/SemanticSearchApi/appsettings.Development.json` or `appsettings.json`:
```json
{
  "OllamaUri": "http://localhost:11434"
}
```

The Postgres connection string is currently set in code (see `src/SemanticSearchApi/Program.cs`) as:

`Host=localhost;Port=5432;Database=vector_db;Username=postgres;Password=postgres`

Then start the API:
```powershell
cd ..\SemanticSearchApi
 dotnet run
```
Default ports (HTTPS) usually: `https://localhost:7039` (verify in `launchSettings.json`).
OpenAPI/Scalar UI is available in Development.

### Available Endpoints (Conceptual)
- `GET /semantic-search?query=...` – vector similarity search.
- `GET /ask/simple-rag?query=...` – retrieve + generate answer.
- `GET /ask/rag-with-hyde?query=...` – HyDE pipeline.
- `POST /chat` – chat response based on conversation history.

## 5. Run the Blazor UI
Install front-end dependencies and build Tailwind CSS:
```powershell
cd ..\Rag.UI
npm install
npm run tailwind-build
```
Then start the UI:
```powershell
 dotnet run
```
Navigate to the served HTTPS port (e.g. `https://localhost:7235`).

> The UI calls the API via an `HttpClient` configured in `src/Rag.UI/Program.cs` (`BaseAddress = https://localhost:7039/`). If your API runs on a different port, update that value.

Pages:
- `/semantic-search` – semantic vector search results with scores.
- `/simple-rag-search` – basic RAG answer view.
- `/rag-search-with-hyde` – HyDE style retrieval augmentation.
- `/chat` – conversational interface with the LLM.

## Configuration Summary
| Setting | Location | Description |
|---------|----------|-------------|
| OllamaUri | `appsettings*.json` (API) | Base URL for Ollama runtime. Must match local install (default `http://localhost:11434`). |
| Vector DB | `docker-compose.yml` | Postgres + pgvector container exposed on `localhost:5432`. |
| Vector DB connection string | `Program.cs` (API + initializer) | Currently hardcoded to local Docker Postgres. |
| Collection name | `SharedKernel/Constants/VectorDbCollections.cs` | `document_collection` (used by both indexing + search). |
| Embedding Dimensions | `OllamaModels.NomicEmbedTextDimensions` | Must match chosen embedding model dimension. |

## Troubleshooting
| Issue | Cause | Resolution |
|-------|-------|-----------|
| Cannot connect to Postgres | Container not running | `docker compose ps` then `docker compose up -d` |
| No tables / extension created | schema init didn’t run | Confirm `./postgres/schema.sql` exists and recreate the container/volume if needed |
| Embedding dimension mismatch | Changed model without updating code | Update `NomicEmbedTextDimensions` and re-index data |
| Ollama model not found | Model not pulled yet | Run `ollama pull <model>` again |
| Slow first request | Model cold start | Subsequent requests are faster; optionally warm using a dummy prompt |
| Tailwind classes missing | CSS not rebuilt | Run `npm run tailwind-build` after markup changes |

## Relevant Documentation & References
- Ollama: https://ollama.com
- Semantic Kernel: https://github.com/microsoft/semantic-kernel
- Vector store connectors (pgvector): https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/postgres-connector
- Flowbite Components: https://flowbite.com/docs/
- Tailwind CSS: https://tailwindcss.com/docs

