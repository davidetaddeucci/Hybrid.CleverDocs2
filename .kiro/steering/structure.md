# Project Structure

## Solution Organization
```
Hybrid.CleverDocs2/
├── Hybrid.CleverDocs2.WebServices/    # Backend API (.NET 9.0 Web API)
├── Hybrid.CleverDocs.WebUI/           # Frontend MVC (.NET 9.0 MVC)
├── Hybrid.CleverDocs2.Tests/          # Unit tests
├── docs/                              # Documentation and architecture
├── scripts/                           # Database and deployment scripts
└── *.sql, *.ps1, *.md                # Root-level utilities and docs
```

## Backend Structure (WebServices)
```
Hybrid.CleverDocs2.WebServices/
├── Controllers/          # API endpoints (AuthController, DocumentController, etc.)
├── Services/            # Business logic and external integrations
│   ├── Auth/           # Authentication and JWT services
│   ├── Cache/          # Multi-level caching implementation
│   ├── Clients/        # R2R API client implementations
│   ├── Collections/    # Collection management services
│   ├── Companies/      # Company management services
│   ├── Documents/      # Document processing services
│   ├── LLM/           # LLM provider configuration services
│   ├── Logging/       # Correlation and audit logging
│   ├── Queue/         # RabbitMQ and rate limiting services
│   └── Users/         # User management services
├── Data/               # Entity Framework DbContext and entities
├── Hubs/              # SignalR hubs for real-time communication
├── Middleware/        # Custom middleware (JWT, Tenant, Exception)
├── Workers/           # Background services and hosted workers
├── Models/            # DTOs and API models
├── Extensions/        # Extension methods and utilities
├── Consumers/         # RabbitMQ message consumers
├── Messages/          # Message types for RabbitMQ
├── Migrations/        # Entity Framework database migrations
└── Exceptions/        # Custom exception types
```

## Frontend Structure (WebUI)
```
Hybrid.CleverDocs.WebUI/
├── Controllers/        # MVC controllers for page handling
├── Views/             # Razor views (.cshtml files)
│   ├── Auth/         # Authentication pages
│   ├── Dashboard/    # Dashboard views (Admin, Company, User)
│   ├── Collections/  # Collection management views
│   ├── Documents/    # Document management views
│   ├── Chat/         # Chat/conversation views
│   └── Shared/       # Shared layouts and components
├── ViewModels/        # MVVM pattern view models
├── Services/          # API client services for backend communication
├── Models/           # DTOs and frontend models
├── Helpers/          # Utility classes and extensions
├── ViewComponents/   # Reusable view components
└── wwwroot/          # Static assets (CSS, JS, images)
    ├── css/         # Stylesheets (Material Design 3)
    ├── js/          # JavaScript files
    ├── lib/         # Third-party libraries
    └── images/      # Static images
```

## Key Architectural Patterns

### Service Layer Organization
- **Interface-first**: All services implement interfaces for testability
- **Scoped services**: Most business logic services are scoped per request
- **Singleton services**: Caching and rate limiting services are singletons
- **Dependency injection**: All dependencies injected via constructor

### Data Access Pattern
- **Entity Framework Core**: Primary ORM with PostgreSQL
- **Repository pattern**: Abstracted data access where needed
- **DbContext per request**: Scoped lifetime for database contexts
- **Migrations**: Code-first database schema management

### Caching Strategy
- **L1 Cache**: In-memory caching for frequently accessed data
- **L2 Cache**: Redis for distributed caching across instances
- **L3 Cache**: Persistent file-based cache for expensive operations
- **Cache invalidation**: Event-driven invalidation with correlation IDs

### Multi-tenancy Implementation
- **Shared database**: Single database with tenant isolation
- **Tenant resolution**: Middleware extracts tenant from JWT claims
- **Global filters**: EF Core global query filters for data isolation
- **Tenant-aware caching**: Cache keys include tenant identifiers

## File Naming Conventions
- **Controllers**: `{Entity}Controller.cs` (e.g., `DocumentController.cs`)
- **Services**: `I{Service}Service.cs` and `{Service}Service.cs`
- **Models/DTOs**: `{Entity}Dto.cs`, `{Entity}ViewModel.cs`
- **Views**: `{Action}.cshtml` in `Views/{Controller}/` folder
- **Configuration**: `{Feature}Options.cs` for strongly-typed config

## Database Schema Organization
- **Core entities**: Users, Companies, Documents, Collections
- **Audit tables**: AuditLogs, LLMAuditLogs, LLMUsageLogs
- **System tables**: TokenBlacklists, UserLLMPreferences
- **Widget system**: UserDashboardWidgets, WidgetTemplates
- **Multi-tenant**: CompanyId foreign keys for data isolation

## Configuration Structure
- **Hierarchical sections**: Grouped by feature (Database, Redis, R2R, etc.)
- **Environment-specific**: Development/Production overrides
- **Strongly-typed**: Options pattern with validation
- **External services**: Centralized connection strings and API endpoints

## Documentation Organization
- **Root docs/**: High-level architecture and system design
- **Project-specific**: README.md in each project folder
- **API documentation**: OpenAPI/Swagger for backend endpoints
- **Architectural decisions**: ADR format for major decisions