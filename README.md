# RagDemo

A full end-to-end Retrieval Augmented Generation (RAG) demo showcasing:
- Document ingestion (Markdown/PDF → embeddings)
- Vector similarity search (Qdrant)
- Hybrid RAG and HyDE style query flows
- Local LLM interaction via Ollama (chat, generation, embeddings)
- Blazor interactive UI for search, chat, and experimentation
- Shared kernel abstractions for models/constants/services

## Project Structure & Purpose
| Project | Purpose |
|---------|---------|
| `SemanticSearchApi` | ASP.NET Core API exposing semantic search, RAG (simple + HyDE), and chat endpoints using Ollama + Qdrant. |
| `Rag.VectorBaseInitializer` | Console/Kernel app that builds the initial document vector store: reads Markdown files, generates embeddings, creates Qdrant collection(s). |
| `Rag.UI` | Blazor Server interactive UI for querying semantic search, simple RAG, HyDE RAG, and chat-based interactions. Tailwind + Flowbite for styling. |
| `SharedKernel` | Cross-project shared constants (e.g. model IDs), response models, and abstractions. |
| `Python Scripts/docling_script.py` | Optional OCR + PDF → Markdown pipeline using Docling + fallback OCR engines for preparing additional content. |

## High-Level Flow
1. Source content (Markdown / optionally converted PDFs) is embedded using Ollama's `nomic-embed-text` model.
2. Embeddings are stored in a Qdrant collection (`DocumentVectors`).
3. Queries generate embeddings, perform vector similarity search in Qdrant, retrieve top documents.
4. For HyDE, a hypothetical answer is synthesized first to refine retrieval.
5. Retrieved context is assembled and sent to the chat / text generation model (`llama3.2:1b`) to produce an answer.

## Prerequisites
Ensure the following are installed locally:
- **.NET SDK** (Preview/RC supporting `net10.0` target) – if unavailable, install the latest from: https://dotnet.microsoft.com/download
- **Docker** (for Qdrant) – https://docs.docker.com/get-docker/
- **Ollama** (local LLM runtime) – https://ollama.com/download
- **Node.js + npm** (for Tailwind build) – https://nodejs.org/
- **Python 3.11+** (optional OCR/Docling pipeline) – https://www.python.org/downloads/

## 1. Start Qdrant (Vector Database)
From repository root:
```powershell
docker compose up -d
```
This starts Qdrant at `localhost:6333`. Data persists under `./qdrant_data`.

### Qdrant Details
- Image: `qdrant/qdrant:latest`
- Ports: 6333 (HTTP), 6334 (gRPC)
- Collection created by initializer: `DocumentVectors`
- Distance metric: Cosine
- Embedding dimension: 768 (matches `nomic-embed-text`)

Qdrant Docs: https://qdrant.tech/documentation/

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
- Verify Ollama is running and the vector store is available by running `docker-compose up -d`

From repository root:
```powershell
cd .\src\Rag.VectorBaseInitializer
dotnet run
```
You should see logs indicating each document embedding insertion. Once the process completes, the Qdrant collection `document_collection` will be populated. You can view this at http://localhost:6333/dashboard#/collections.

## 4. Run the Semantic Search API
Configure `appsettings.Development.json` or `appsettings.json` with:
```json
{
  "OllamaUri": "http://localhost:11434"
}
```
Then start the API:
```powershell
cd ..\SemanticSearchApi
dotnet run
```
Default ports (HTTPS) usually: `https://localhost:7039` (verify in launchSettings.json).
OpenAPI/Scalar UI is available in Development.

### Available Endpoints (Conceptual)
- `GET /semantic-search?query=...` – vector similarity + semantic ranking.
- `GET /ask/simple-rag?query=...` – retrieve + generate answer.
- `GET /ask/rag-with-hyde?query=...` – HyDE pipeline (generate hypothetical answer → refined retrieval → final answer).
- `POST /chat` – streaming or batched chat response based on conversation history (list of messages).

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

Pages:
- `/semantic-search` – semantic vector search results with scores.
- `/simple-rag-search` – basic RAG answer view.
- `/rag-search-with-hyde` – HyDE style retrieval augmentation.
- `/chat` – conversational interface with the LLM.

## 6. (Optional) Convert PDFs to Markdown
Use `docling_script.py` to OCR and convert a PDF to Markdown (adds richer content before indexing):
```powershell
cd ..\Python Scripts
python -m venv .venv
.\.venv\Scripts\activate
pip install langchain-docling ocrmypdf rapidocr-paddle tesseract
python .\docling_script.py
```
Output will be placed under `output/` – move the generated `.md` file into `src/Rag.VectorBaseInitializer/Markdown` and re-run the initializer.

Docling: https://github.com/integrations/awesome-langchain#document-loaders
OCRmyPDF: https://ocrmypdf.readthedocs.io/

## Models Used
| Purpose | Model | Identifier | Notes |
|---------|-------|------------|-------|
| Embeddings | Nomic Embed Text | `nomic-embed-text:latest` | 768 dims, used for document + query embeddings. |
| Chat / Generation | Llama 3.2 (1B) | `llama3.2:1b` | Lightweight general model for answer synthesis & chat. |

To change: edit `OllamaModels` constants and re-run initializer/API/UI.

## Configuration Summary
| Setting | Location | Description |
|---------|----------|-------------|
| OllamaUri | `appsettings*.json` (API) | Base URL for Ollama runtime. Must match local install (default `http://localhost:11434`). |
| Embedding Dimensions | `OllamaModels.NomicEmbedTextDimensions` | Must match chosen embedding model dimension. |
| Qdrant Host | hardcoded in program files (`localhost`) | Adjust if running remote Qdrant. |
| Collections | `VectorDbCollections.DocumentVectors` | The primary embedding collection. |

## Troubleshooting
| Issue | Cause | Resolution |
|-------|-------|-----------|
| 404 / cannot connect to Qdrant | Container not running | `docker compose ps` then `docker compose up -d` |
| Embedding dimension mismatch | Changed model without updating code | Update `NomicEmbedTextDimensions` and recreate collection. |
| Ollama model not found | Model not pulled yet | Run `ollama pull <model>` again. |
| Slow first request | Model cold start | Subsequent requests are faster; optionally warm using a dummy prompt. |
| Tailwind classes missing | CSS not rebuilt | Run `npm run tailwind-build` after markup changes. |

## Relevant Documentation & References
- Ollama: https://ollama.com
- Qdrant: https://qdrant.tech/documentation/
- Semantic Kernel: https://github.com/microsoft/semantic-kernel
- Kernel Memory (embeddings): https://github.com/microsoft/kernel-memory
- Flowbite Components: https://flowbite.com/docs/
- Tailwind CSS: https://tailwindcss.com/docs

