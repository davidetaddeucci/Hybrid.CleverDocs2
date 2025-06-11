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

## Known Gaps
- Missing OpenAPI/Swagger specification (`.yaml` file)
- No automated test suites (API unit tests, Blazor component tests, E2E, load tests)
- Missing CI/CD pipeline definitions
- Monitoring and health checks not instrumented (Prometheus/Grafana)

## Prerequisites
- .NET 9.0 SDK
- PostgreSQL 16+
- Redis 7.0+
- RabbitMQ 3.12+
- SciPhi AI R2R API server up and running

## Service Endpoints
The following endpoints correspond to the Docker Compose setup:
- **PostgreSQL**: `localhost:5433`, database: `mydatabase`, user: `your_postgres_user`, password: `your_strong_password`
- **Redis**: `localhost:6380`, password: `your_redis_password`
- **RabbitMQ**: AMQP at `localhost:5674`, Management UI at `http://localhost:15674`, user: `your_rabbitmq_user`, password: `your_strong_password`

## R2R Wrapper Implementation Plan
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
