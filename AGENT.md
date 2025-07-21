# Hybrid.CleverDocs2 Agent Guide

## Build & Test Commands
- **Build solution**: `dotnet build Hybrid.CleverDocs2.sln`
- **Build backend**: `dotnet build Hybrid.CleverDocs2.WebServices`
- **Build frontend**: `dotnet build Hybrid.CleverDocs.WebUI`
- **Run all tests**: `dotnet test`
- **Run single project tests**: `dotnet test Hybrid.CleverDocs2.Tests`
- **Backend dev server**: `dotnet run --project Hybrid.CleverDocs2.WebServices`
- **Frontend dev server**: `dotnet run --project Hybrid.CleverDocs.WebUI`

## Architecture
**Multi-tenant document management system** (.NET 9.0) with R2R AI integration:
- **WebServices**: REST API backend (JWT auth, PostgreSQL, Redis cache, RabbitMQ queues)
- **WebUI**: MVC frontend (Material Design, SignalR real-time)
- **Tests**: xUnit test project with Moq and in-memory EF Core
- **Database**: PostgreSQL multi-tenant with company isolation
- **External**: R2R API (192.168.1.4:7272), Redis (6380), RabbitMQ (5674), PostgreSQL (5433)

## Key System Components
- **SignalR Hubs**: ChatHub, DocumentUploadHub, CollectionHub with event persistence
- **R2R Integration**: Complete v3 API clients with circuit breakers, rate limiting (10 req/s docs, 20 req/s search)
- **LLM Configuration**: Per-user provider selection (OpenAI, Anthropic, Azure) with encrypted API keys
- **Caching Strategy**: Multi-level (L1 memory, L2 Redis, L3 persistent) with tag-based invalidation
- **Background Workers**: R2RDocumentProcessingWorker for queue processing

## Code Style & Conventions
- **Imports**: Group Microsoft.* first, then project imports, system last
- **Naming**: PascalCase for public members, camelCase for private fields
- **Controllers**: `[Authorize]`, async/await pattern, return ActionResult<T>
- **Services**: Dependency injection via Program.cs, interface-based
- **Models**: Separate DTOs, ViewModels, and Entity classes
- **Error handling**: Try-catch with structured logging, return ApiResponse<T>
- **Authentication**: JWT Bearer tokens with ClaimsPrincipal.GetUserId() extension

## Testing & Development Guidelines
- **Test User**: r.antoniucci@microsis.it | password: Maremmabona1! (User role)
- **Templates**: DO NOT modify existing WebUI templates - create new ones following existing patterns
- **Material Design**: Follow existing _Layout.cshtml and navigation structure
- **SignalR**: Use existing hub patterns for real-time features
