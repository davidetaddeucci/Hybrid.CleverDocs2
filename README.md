# Hybrid.CleverDocs2

## Overview
Hybrid.CleverDocs2 is an enterprise-grade multi-tenant WebUI for managing document collections and interacting with the SciPhi AI R2R API engine. It comprises:

- **Hybrid.CleverDocs2.WebServices**: .NET 9.0 Web API backend, implements REST endpoints, JWT authentication, multi-tenant data isolation, and background workers for R2R queue consumption.
- **Hybrid.CleverDocs.WebUI**: .NET 9.0 Blazor frontend (WebAssembly) with MudBlazor components, supporting multi-tenant login, document management, and AI chat interface.

## Objectives
- Provide an intuitive UI for non-technical users
- Implement robust queuing of R2R jobs to handle high document volumes
- Ensure tenant data isolation
- Optimize performance via advanced caching
- Secure authentication and authorization with JWT
- Enable monitoring and scalability

## Repository Structure
```
Hybrid.CleverDocs2/
├── docs/                          # General documentation and design artifacts
├── Hybrid.CleverDocs2.WebServices # Backend API server project
├── Hybrid.CleverDocs.WebUI        # Frontend Blazor project
└── README.md                      # This file
```

## Documentation
### General System Documentation (docs/)
- **System Architecture**: `docs/Architettura del Sistema Hybrid.CleverDocs2.md`
- **Auth Flows**: `docs/Flussi di Autenticazione e Autorizzazione.md`, `docs/autenticazione_autorizzazione.md`
- **Data Model (PostgreSQL)**: `docs/modello_dati.md`, `docs/Modello Dati PostgreSQL per WebUI Multitenant R2R.md`
- **RabbitMQ Integration**: `docs/Integrazione con R2R e Sistema di Gestione Code con RabbitMQ.md`
- **Redis Caching**: `docs/Integrazione con Redis per Caching Avanzato.md`
- **Scalability & Robustness**: `docs/scalabilita_robustezza.md`
- **Deployment Guide**: `docs/README_ Architettura e Deployment.md`

### Subproject Documentation
- **Frontend (Blazor)**: `Hybrid.CleverDocs.WebUI/README.md`, `docs/R2R WebUI Frontend Blazor.md`, `docs/interfaccia_utente.md`
- **Backend (WebServices)**: `Hybrid.CleverDocs2.WebServices/README.md`, `docs/R2R WebUI Backend API Server.md`, `docs/Documenti Specifici per WebServices.md`

## Current Development Status 🚀

### ✅ Completed (Phase 2)
- **Backend Authentication System**: Complete JWT-based authentication with multi-tenant support
- **Entity Framework Models**: Full models with User, Company, Document, Collection, AuditLog entities
- **Database Schema**: PostgreSQL database with 8 tables created via migrations
- **Authentication Services**: IAuthService, AuthService, IJwtService, JwtService fully implemented
- **AuthController**: Complete with login, register, refresh, logout, profile, password management endpoints
- **External Services**: All services (PostgreSQL, Redis, RabbitMQ, R2R API) tested and operational
- **Package Management**: Updated to EF Core 8.0.10, compatible versions across all packages

### 🔄 In Progress
- **Frontend Development**: Basic structure without Blazor (removed MudBlazor packages as requested)
- **Role-based Dashboard Templates**: Admin, Company, User dashboard implementations pending

### 📋 Next Steps (Phase 3)
1. **Authentication Testing**: Test all authentication endpoints with actual requests
2. **Dashboard Implementation**: Create role-based dashboard templates
3. **Login→Dashboard Workflow**: Complete end-to-end user flow testing
4. **Frontend UI Controls**: Maintain current UI controls (non-Blazor approach)

### 🎯 Ready for Testing
- **Backend API**: All authentication endpoints ready for testing
- **Database**: Schema created and ready for data
- **External Services**: All dependencies operational

## Tasks & To-Do
- For a high-level roadmap of features and design tasks, see `docs/todo.md`.
- For detailed pending fixes and cleanup items, see `docs/to_fix.md`.

## Known Gaps
- Missing OpenAPI/Swagger specification (`.yaml` file)
- No automated test suites (API unit tests, Blazor component tests, E2E, load tests)
- Missing CI/CD pipeline definitions
- Monitoring and health checks not instrumented (Prometheus/Grafana)
- Frontend role-based dashboard templates not yet implemented

## Prerequisites
- .NET 9.0 SDK
- PostgreSQL 16+
- Redis 7.0+
- RabbitMQ 3.12+
- SciPhi AI R2R API server up and running

## Service Endpoints

### WebServices API Endpoints

- Auth:
  - POST /api/auth/login
  - POST /api/auth/refresh
  - POST /api/auth/logout
  - POST /api/auth/users
  - GET  /api/auth/users
  - GET  /api/auth/users/{id}
  - PUT  /api/auth/users/{id}
  - DELETE /api/auth/users/{id}

- Documents:
  - POST   /api/documents
  - GET    /api/documents
  - GET    /api/documents/{id}
  - PUT    /api/documents/{id}
  - DELETE /api/documents/{id}

- Conversations:
  - POST   /api/conversations
  - GET    /api/conversations
  - GET    /api/conversations/{id}
  - PUT    /api/conversations/{id}
  - DELETE /api/conversations/{id}

- Prompts:
  - POST   /api/prompts
  - GET    /api/prompts
  - GET    /api/prompts/{id}
  - PUT    /api/prompts/{id}
  - DELETE /api/prompts/{id}

- Ingestions:
  - POST   /api/ingestions
  - GET    /api/ingestions
  - GET    /api/ingestions/{id}
  - PUT    /api/ingestions/{id}
  - DELETE /api/ingestions/{id}

- Graphs:
  - POST   /api/graphs
  - GET    /api/graphs
  - GET    /api/graphs/{id}
  - PUT    /api/graphs/{id}
  - DELETE /api/graphs/{id}

- Search:
  - POST   /api/search
  - GET    /api/search
  - GET    /api/search/{id}
  - PUT    /api/search/{id}
  - DELETE /api/search/{id}

- Tools:
  - POST   /api/tools
  - GET    /api/tools
  - GET    /api/tools/{id}
  - PUT    /api/tools/{id}
  - DELETE /api/tools/{id}

- Maintenance:
  - POST   /api/maintenance
  - GET    /api/maintenance
  - GET    /api/maintenance/{id}
  - PUT    /api/maintenance/{id}
  - DELETE /api/maintenance/{id}

- Orchestrations:
  - POST   /api/orchestrations
  - GET    /api/orchestrations
  - GET    /api/orchestrations/{id}
  - PUT    /api/orchestrations/{id}
  - DELETE /api/orchestrations/{id}

- Local-LLM:
  - POST   /api/local-llm
  - GET    /api/local-llm
  - GET    /api/local-llm/{id}
  - PUT    /api/local-llm/{id}
  - DELETE /api/local-llm/{id}

- Validations:
  - POST   /api/validations
  - GET    /api/validations
  - GET    /api/validations/{id}
  - PUT    /api/validations/{id}
  - DELETE /api/validations/{id}

- MCP Tuning:
  - POST   /api/mcp-tuning
  - GET    /api/mcp-tuning
  - GET    /api/mcp-tuning/{id}
  - PUT    /api/mcp-tuning/{id}
  - DELETE /api/mcp-tuning/{id}

- WebDev:
  - POST   /api/webdev
  - GET    /api/webdev
  - GET    /api/webdev/{id}
  - PUT    /api/webdev/{id}
  - DELETE /api/webdev/{id}
## External Services Configuration

The following endpoints correspond to the Docker Compose setup:
- **PostgreSQL**: `localhost:5433`, database: `cleverdocs`, user: `admin`, password: `MiaPassword123`
- **Redis**: `localhost:6380`, password: `your_redis_password`
- **RabbitMQ**: AMQP at `localhost:5674`, Management UI at `http://localhost:15674`
- **R2R API**: `localhost:7272` with Swagger UI at `/docs`

### Service Status ✅
- ✅ **PostgreSQL**: Connected, schema created with 8 tables via Entity Framework migrations
- ✅ **Redis**: Connected and responding (PONG)
- ✅ **RabbitMQ**: Available and accessible
- ✅ **R2R API**: Running and accessible with Swagger documentation

## Self-hosted R2R API Container
We assume a self-hosted R2R container (using `full.toml`) running on an independent Postgres, RabbitMQ, and Redis instance. Configure the container with your custom configuration file by setting the environment variable `R2R_CONFIG_PATH` inside the container to your TOML (e.g., `/configs/full.toml`).

For runtime overrides (e.g., search settings, model selection), the wrapper supports passing dynamic configuration in each API call. See https://r2r-docs.sciphi.ai/self-hosting/configuration/overview.

In `appsettings.json` / `appsettings.Development.json`, configure the R2R section:
```json
"R2R": {
  "BaseUrl": "http://localhost:7272",
  "ConfigPath": "/configs/full.toml",
  "DefaultTimeout": 30,
  "MaxRetries": 3
}
```

## R2R Wrapper Implementation Plan
11. Auth client: login, token refresh, logout, session introspection.
12. Document & Collection clients: CRUD, metadata, multi-tenant filters.
13. Conversation client: manage chat sessions, pagination, transcripts.
14. Prompt client: template CRUD, versioning, validation.
15. Search & RAG client: vector, hybrid, advanced, agentic flows, with deduplication and contextual enrichment pre-processing.

The WebServices project will implement a resilient .NET client wrapper against the SciPhi AI R2R API in a modular 10-phase approach:
1. Common infrastructure: named HttpClients with BaseAddress, Polly policies (retry, circuit breaker, timeout, bulkhead), Serilog logging, Prometheus metrics, health checks.
2. Ingestion client: chunked uploads, idempotency tokens, parallel ingestion with throttling, retry/backoff, resume support.
3. Graph client: batch graph operations, caching, eventual consistency, error handling.
4. Custom tools client: plugin registration, sandbox execution, metadata & health-check endpoints, versioning.
5. Maintenance client: scheduled and on-demand cleanup, dry-run mode, alerting, database/index maintenance.
6. Orchestration service: idempotent workflows combining ingestion, indexing, RAG, tools, backed by RabbitMQ and state persisted in PostgreSQL.
7. Local-LLM fallback: strategy pattern between remote R2R and on-premise LLMs, TTL caching, health probes, load balancing.
8. Structured-output validation: JSON schema enforcement, auto-correction of deviations, centralized schema registry.
9. MCP introspection & tuning: runtime tuning via API, pipeline metrics, introspection endpoints.
10. Web-Dev integration & Evals: CORS/iframe support, SSE/WebSocket streaming, embedded Swagger UI, health metrics, evaluation endpoints.


## Quick Start
1. Prepare a `docker-compose.yml` for PostgreSQL, Redis, and RabbitMQ and note their endpoints.
2. Clone the repository:
   ```bash
   git clone https://github.com/davidetaddeucci/Hybrid.CleverDocs2.git
   cd Hybrid.CleverDocs2
   ```
3. Configure service endpoints in `appsettings.Development.json` for both WebServices and WebUI:
   - `ConnectionStrings:Postgres`
   - `Redis:Configuration`
   - `RabbitMQ:Host`
   - `SciPhi:R2R:Url`
4. Run the backend:
   ```bash
   cd Hybrid.CleverDocs2.WebServices
   dotnet run
   ```
5. Run the frontend:
   ```bash
   cd Hybrid.CleverDocs.WebUI
   dotnet run
   ```
6. Open your browser to the address shown (e.g., `https://localhost:7000`).

## Contribution
- Use feature branches, write tests, follow `.editorconfig`, and submit pull requests for review.

## License
© 2025 Hybrid Research. All rights reserved.
