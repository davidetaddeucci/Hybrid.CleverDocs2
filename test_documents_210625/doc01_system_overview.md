# Hybrid.CleverDocs2 System Overview

## Architecture
Hybrid.CleverDocs2 is an enterprise-grade multi-tenant WebUI for managing document collections and interacting with the SciPhi AI R2R API engine.

### Components
- **WebServices**: .NET 9.0 Web API backend
- **WebUI**: .NET 9.0 MVC frontend
- **R2R Integration**: Document processing and search
- **PostgreSQL**: Multi-tenant database
- **Redis**: Caching layer
- **RabbitMQ**: Message queuing

### Key Features
- JWT authentication with multi-tenant support
- Real-time document processing with SignalR
- Advanced caching strategies
- Rate limiting and circuit breaker patterns
- Modern responsive UI with Material Design

This document serves as a test for the document processing pipeline.