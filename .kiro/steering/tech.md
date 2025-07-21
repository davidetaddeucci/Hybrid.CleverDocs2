# Technology Stack

## Framework & Runtime
- **.NET 9.0**: Latest LTS version with modern C# features
- **ASP.NET Core MVC**: Frontend web application (NOT Blazor - see architectural correction)
- **ASP.NET Core Web API**: Backend services with OpenAPI/Swagger
- **Entity Framework Core 9.0**: ORM with PostgreSQL provider

## Database & Storage
- **PostgreSQL 16+**: Primary database (Host: 192.168.1.4:5433)
- **Redis 7.0+**: Caching and session storage (Host: 192.168.1.4:6380)
- **File System**: Document storage with chunked upload support

## Message Queue & Background Processing
- **RabbitMQ 3.12+**: Message queuing (Host: 192.168.1.4:5674)
- **MassTransit 8.4+**: .NET messaging abstraction layer
- **Hosted Services**: Background workers for document processing

## External Integrations
- **SciPhi AI R2R API**: Document processing and AI chat (Host: 192.168.1.4:7272)
- **Multiple LLM Providers**: OpenAI, Anthropic, Azure OpenAI support

## Key Libraries & Packages
- **Authentication**: Microsoft.AspNetCore.Authentication.JwtBearer
- **Database**: Npgsql.EntityFrameworkCore.PostgreSQL
- **Caching**: Microsoft.Extensions.Caching.StackExchangeRedis
- **Resilience**: Polly.Extensions.Http (circuit breaker, retry policies)
- **Security**: BCrypt.Net-Next for password hashing
- **Real-time**: SignalR for live updates
- **Health Checks**: AspNetCore.HealthChecks.* packages

## Architecture Patterns
- **Multi-tenant**: Shared database with tenant isolation
- **CQRS-lite**: Separate read/write operations where beneficial
- **Repository Pattern**: Data access abstraction
- **Service Layer**: Business logic separation
- **Circuit Breaker**: Resilience for external API calls
- **Multi-level Caching**: L1 (Memory) + L2 (Redis) + L3 (Persistent)

## Development Tools
- **Visual Studio 2022+** or **VS Code** with C# extensions
- **Entity Framework Tools**: For migrations and database management
- **Docker**: For external services (PostgreSQL, Redis, RabbitMQ, R2R)

## Common Commands

### Build & Run
```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run backend API
dotnet run --project Hybrid.CleverDocs2.WebServices

# Run frontend MVC
dotnet run --project Hybrid.CleverDocs.WebUI
```

### Database Management
```bash
# Add migration
dotnet ef migrations add MigrationName --project Hybrid.CleverDocs2.WebServices

# Update database
dotnet ef database update --project Hybrid.CleverDocs2.WebServices

# Generate SQL script
dotnet ef migrations script --project Hybrid.CleverDocs2.WebServices
```

### Testing
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test Hybrid.CleverDocs2.Tests/
```

### Health Checks
- Backend health: `GET /health`
- Individual service health checks for PostgreSQL, Redis, RabbitMQ

## Configuration
- **appsettings.json**: Base configuration
- **appsettings.Development.json**: Development overrides
- **appsettings.Production.json**: Production settings
- **Environment Variables**: Override configuration in deployment