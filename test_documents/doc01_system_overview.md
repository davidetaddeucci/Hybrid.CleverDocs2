# System Overview

## Architecture
Hybrid.CleverDocs2 is a comprehensive document management system built on .NET 9.0 with enterprise-grade features.

### Key Components
- **WebUI**: ASP.NET Core MVC frontend with Razor views
- **WebServices**: RESTful API backend with JWT authentication
- **R2R Integration**: Advanced RAG capabilities through SciPhi AI R2R API
- **PostgreSQL Database**: Multi-tenant data storage
- **Redis Cache**: High-performance caching layer
- **RabbitMQ**: Message queue for async processing

### Security Features
- JWT authentication with HttpOnly cookies
- Multi-tenant architecture with company-level isolation
- Role-based access control (RBAC)
- Secure file upload with validation

### Performance Targets
- Dashboard load time: <2 seconds
- Document processing: Real-time status updates via SignalR
- Concurrent users: 100+ per tenant
- File upload: Chunked upload for large files

This system provides enterprise-grade document management with AI-powered search and analysis capabilities.