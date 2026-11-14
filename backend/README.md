# DocuMind Backend (.NET 10 Clean Architecture)

DocuMind is an AI-powered PDF Document Analyzer and RAG-based Chat Application. This repository contains the production-grade ASP.NET Core .NET 10 backend following Clean Architecture principles.

---

## Architecture Overview

```
                      ┌────────────────────────────────────────┐
                      │          React 19 + Vite UI            │
                      │         (http://localhost:5173)        │
                      └───────────────────┬────────────────────┘
                                          │ HTTP / SSE
                                          ▼
                      ┌────────────────────────────────────────┐
                      │            DocuMind.WebApi             │
                      │         (http://localhost:5000)        │
                      └───────┬────────────────────────┬───────┘
                              │                        │
                              ▼                        ▼
        ┌─────────────────────────────┐        ┌─────────────────────────────┐
        │     DocuMind.Application    │        │   DocuMind.Infrastructure   │
        │   (Commands, Queries, DTOs) │        │  (MySQL, Qdrant, Azure AI)  │
        └──────────────┬──────────────┘        └──────────────┬──────────────┘
                       │                                      │
                       └──────────────────┬───────────────────┘
                                          ▼
                               ┌──────────────────────┐
                               │   DocuMind.Domain    │
                               │ (Entities, Rules)    │
                               └──────────────────────┘
```

### Clean Architecture Layers

1. **`DocuMind.Domain`**: Pure business rules, entity models, domain events, and core exceptions. Has no external dependencies.
2. **`DocuMind.Application`**: Orchestrates application logic, CQRS commands/queries, DTOs, interfaces for external services, and result abstractions.
3. **`DocuMind.Infrastructure`**: Concrete adapters for EF Core MySQL (Pomelo), Qdrant vector database, Azure OpenAI LLM/Embeddings, PdfPig extraction, and file storage.
4. **`DocuMind.WebApi`**: Entry point presenting RESTful API endpoints, SSE streams, Swagger/OpenAPI documentation, Serilog structured logging, and CORS policies.

---

## Prerequisites

* **.NET 10 SDK**: Version 10.0.404 or later
* **Docker Desktop**: Version 29.x with Docker Compose v5.x
* **MySQL 8.0+** (or run via Docker Compose)
* **Qdrant Vector Database** (or run via Docker Compose)

---

## Configuration & Environment Variables

Copy `.env.example` to `.env`:

```powershell
cp .env.example .env
```

Key environment settings:
* `MYSQL_ROOT_PASSWORD`, `MYSQL_DATABASE`, `MYSQL_USER`, `MYSQL_PASSWORD`: MySQL database credentials.
* `QDRANT_HTTP_PORT`: Qdrant REST port (default: 6333).
* `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_API_KEY`: Azure OpenAI Foundry resource credentials.
* `AZURE_OPENAI_CHAT_DEPLOYMENT`: e.g. `gpt-4.1-mini`.
* `AZURE_OPENAI_EMBEDDING_DEPLOYMENT`: e.g. `text-embedding-3-small`.

---

## Build & Test Instructions

### Restore Dependencies

```powershell
& "C:\Program Files\dotnet\dotnet.exe" restore DocuMind.sln
```

### Build Solution

```powershell
& "C:\Program Files\dotnet\dotnet.exe" build DocuMind.sln -c Release
```

### Run Unit Tests

```powershell
& "C:\Program Files\dotnet\dotnet.exe" test DocuMind.sln --logger "console;verbosity=normal"
```

---

## Running Locally

### Option 1: Standalone Web API with Local Services

```powershell
cd src\DocuMind.WebApi
& "C:\Program Files\dotnet\dotnet.exe" run --launch-profile http
```

The Web API starts on `http://localhost:5000`:
* **Health Probe**: `http://localhost:5000/health`
* **Swagger OpenAPI UI**: `http://localhost:5000/swagger`

### Option 2: Docker Compose (MySQL 8 + Qdrant)

To spin up MySQL and Qdrant in the background:

```powershell
docker-compose up -d mysql qdrant
```

To run the entire stack (Database, Vector DB, and Web API):

```powershell
docker-compose up --build -d
```
