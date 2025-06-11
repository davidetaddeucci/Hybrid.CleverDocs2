# README: Backend API Server per Hybrid.CleverDocs2

## Panoramica

Hybrid.CleverDocs2.WebServices è il backend del sistema, sviluppato in .NET 9.0 Web API. Fornisce API RESTful per la gestione di utenti, companies, collezioni e documenti, e si integra con SciPhi AI R2R API per l'elaborazione dei documenti e l'interazione con il chatbot.

Questo documento fornisce una guida specifica per il backend API server, con riferimenti alla documentazione dettagliata disponibile nella directory `docs/`.

## Caratteristiche Principali

- API RESTful per tutte le funzionalità del sistema
- Autenticazione JWT con refresh token
- Multitenancy con isolamento dati
- Integrazione con SciPhi AI R2R API
- Elaborazione asincrona dei documenti con RabbitMQ
- Caching avanzato con Redis
- Monitoring e health checks

## Architettura

Il backend è strutturato secondo i principi di Clean Architecture:

```
Hybrid.CleverDocs2.WebServices/
├── src/
│   ├── Hybrid.CleverDocs2.WebServices.Api/        # Controllers, middleware, filtri
│   ├── Hybrid.CleverDocs2.WebServices.Core/       # Logica di business, interfacce
│   ├── Hybrid.CleverDocs2.WebServices.Infrastructure/ # Implementazioni concrete
│   └── Hybrid.CleverDocs2.WebServices.Workers/    # Servizi background
```

- **Api**: Controllers, middleware, filtri e configurazione API
- **Core**: Logica di business, interfacce e modelli di dominio
- **Infrastructure**: Implementazioni concrete (database, servizi esterni, messaging, caching)
- **Workers**: Servizi background per elaborazione asincrona

## Prerequisiti

- .NET 9.0 SDK
- PostgreSQL 16+
- Redis 7.0+
- RabbitMQ 3.12+
- SciPhi AI R2R API server attivo

## Documentazione

Per una comprensione approfondita del backend, consultare i seguenti documenti:

### Documentazione Specifica del Backend

- [Documentazione Dettagliata del Backend](./docs/README_backend_api_server.md)
- [Autenticazione e Autorizzazione](./docs/autenticazione_autorizzazione.md)
- [Integrazione con R2R](./docs/integrazione_r2r_dettagli.md)
- [Gestione Code RabbitMQ](./docs/gestione_code_rabbitmq.md)
- [Caching Redis](./docs/caching_redis_implementazione.md)
- [API Endpoints](./docs/api_endpoints.md)
- [Gestione Multitenancy](./docs/gestione_multitenancy.md)
- [Gestione Documenti e Collezioni](./docs/gestione_documenti_collezioni.md)
- [Implementazione Chatbot](./docs/implementazione_chatbot.md)
- [Struttura dei Controllers](./docs/struttura_controllers.md)
- [Struttura dei Servizi](./docs/struttura_servizi_backend.md)
- [Struttura dei Repository](./docs/struttura_repository.md)
- [Struttura dei Worker Services](./docs/struttura_worker_services.md)
- [Entity Framework Core e PostgreSQL](./docs/implementazione_ef_postgresql.md)
- [Gestione Utenti e Companies](./docs/implementazione_utenti_companies.md)
- [Monitoring e Health Checks](./docs/monitoring_health_checks.md)
- [Testing Backend](./docs/testing_backend.md)
- [Sicurezza API](./docs/sicurezza_api.md)

### Documentazione Architetturale Generale

Per la documentazione generale del sistema, consultare i documenti nella radice del repository:

- [Architettura del Sistema](../docs/architettura/architettura_sistema.md)
- [Modello Dati](../docs/architettura/modello_dati_postgresql.md)
- [Guida al Deployment](../docs/deployment/README_architettura_deployment.md)

## Quick Start

1. Clonare il repository
   ```bash
   git clone https://github.com/hybrid/cleverdocs2.git
   cd Hybrid.CleverDocs2
   ```

2. Navigare nella directory `Hybrid.CleverDocs2.WebServices`
   ```bash
   cd Hybrid.CleverDocs2.WebServices
   ```

3. Ripristinare le dipendenze
   ```bash
   dotnet restore
   ```

4. Configurare le connessioni a PostgreSQL, Redis, RabbitMQ e R2R API in `src/Hybrid.CleverDocs2.WebServices.Api/appsettings.json`

5. Applicare le migrazioni del database
   ```bash
   cd src/Hybrid.CleverDocs2.WebServices.Api
   dotnet ef database update
   ```

6. Avviare l'applicazione API
   ```bash
   dotnet run --project src/Hybrid.CleverDocs2.WebServices.Api/Hybrid.CleverDocs2.WebServices.Api.csproj
   ```

7. Avviare i worker services (in una finestra separata)
   ```bash
   dotnet run --project src/Hybrid.CleverDocs2.WebServices.Workers/Hybrid.CleverDocs2.WebServices.Workers.csproj
   ```

## Configurazione

La configurazione principale dell'applicazione si trova in `src/Hybrid.CleverDocs2.WebServices.Api/appsettings.json`. È possibile sovrascrivere queste impostazioni in `appsettings.Development.json` per l'ambiente di sviluppo.

Principali sezioni di configurazione:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=192.168.1.4;Port=5432;Database=cleverdocs;Username=postgres;Password=yourpassword",
    "Redis": "192.168.1.4:6379",
    "RabbitMQ": "amqp://guest:guest@192.168.1.4:5672"
  },
  "R2R": {
    "ConfigPath": "./configs/full.toml",  # Custom TOML config path inside R2R container
    "ApiUrl": "http://192.168.1.4:7272",
    "DefaultTimeout": 30,
    "MaxRetries": 3
  },
  "Jwt": {
    "Key": "your-secret-key-here-at-least-32-characters",
    "Issuer": "cleverdocs-api",
    "Audience": "cleverdocs-clients",
    "ExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 7
  },
  "AllowedOrigins": {
    "WebUI": "https://localhost:7000"
  },
  "RabbitMQ": {
    "DocumentExchange": "document.exchange",
    "NotificationExchange": "notification.exchange",
    "DocumentProcessingQueue": "document.processing",
    "DocumentIndexingQueue": "document.indexing",
    "NotificationQueue": "notification.queue",
    "PrefetchCount": 10
  },
  "Redis": {
    "DefaultExpiryMinutes": 60,
    "LongExpiryHours": 24
  }
}
```

## API Endpoints

Il backend espone i seguenti gruppi di endpoint:

- **Auth**: `/api/auth/login`, `/api/auth/refresh`, `/api/auth/logout`
- **Users**: `/api/users`, `/api/users/{id}`, etc.
- **Companies**: `/api/companies`, `/api/companies/{id}`, etc.
- **Collections**: `/api/collections`, `/api/collections/{id}`, etc.
- **Documents**: `/api/documents`, `/api/documents/{id}`, etc.
- **Chat**: `/api/chat/query`, `/api/chat/conversations`, etc.
- **Settings**: `/api/settings`, `/api/settings/{key}`, etc.
- **Health**: `/health`

Per una documentazione completa degli endpoint API, consultare [API Endpoints](./docs/api_endpoints.md) o accedere alla documentazione Swagger all'indirizzo `/swagger` quando l'API è in esecuzione.

## Multitenancy

Il sistema implementa un approccio multitenant, che permette a più aziende (Companies) di utilizzare la stessa istanza dell'applicazione mantenendo i dati isolati:

- **Risoluzione Tenant**: Middleware che identifica il tenant corrente in base all'utente autenticato
- **Filtri Globali**: Implementati in Entity Framework Core per filtrare automaticamente i dati per tenant
- **Caching Tenant-aware**: Ogni tenant ha il proprio namespace nel cache Redis

Per dettagli sull'implementazione della multitenancy, consultare [Gestione Multitenancy](./docs/gestione_multitenancy.md).

## Integrazione con R2R

Il backend si integra con SciPhi AI R2R API per l'elaborazione dei documenti e l'interazione con il chatbot:

- **Client R2R**: Implementato con resilienza (retry, circuit breaker)
- **Mapping DTO**: Conversione tra modelli R2R e modelli di dominio
- **Caching**: Risultati R2R memorizzati in cache per migliorare le performance
- **Rate Limiting**: Limitazione delle richieste a R2R API

Per dettagli sull'integrazione con R2R, consultare [Integrazione con R2R](./docs/integrazione_r2r_dettagli.md).

## Gestione Code con RabbitMQ

Per gestire l'elaborazione asincrona dei documenti, il sistema utilizza RabbitMQ:

- **Code Separate**: Per diversi tipi di operazioni (upload, parsing, indicizzazione)
- **Worker Services**: Servizi dedicati per l'elaborazione dei messaggi
- **Circuit Breaker**: Per gestire errori e fallimenti
- **Retry con Backoff Esponenziale**: Per operazioni fallite

## Contribution
- Seguire il piano di implementazione modulare per i client R2R
- Scrivere test unitari e di integrazione per ogni servizio in `tests/`
- Aggiornare la documentazione `docs/` con esempi di utilizzo e scenari di failure
- Creare PR per ogni feature branch

## License
© 2025 Hybrid Research. All rights reserved.

- **Dead Letter Queue**: Per messaggi non elaborabili

Per dettagli sull'implementazione del sistema di code, consultare [Gestione Code RabbitMQ](./docs/gestione_code_rabbitmq.md).

## Caching con Redis

Il sistema utilizza Redis per implementare una strategia di caching avanzata:

- **Caching Multi-livello**: Per diversi tipi di dati (documenti, collezioni, risultati chatbot)
- **Caching Tenant-aware**: Isolamento dei dati in cache per tenant
- **Invalidazione Selettiva**: Invalidazione mirata della cache quando i dati cambiano
- **Distributed Locking**: Per operazioni concorrenti
- **Rate Limiting**: Per limitare le richieste a R2R API

Per dettagli sull'implementazione del caching, consultare [Caching Redis](./docs/caching_redis_implementazione.md).

## Testing

Il progetto include test unitari, di integrazione e API:

- **Test Unitari**: Per servizi e logica di business
- **Test di Integrazione**: Per repository e servizi di infrastruttura
- **Test API**: Per endpoint API end-to-end

Eseguire i test:
```bash
dotnet test tests/Hybrid.CleverDocs2.WebServices.Core.Tests/Hybrid.CleverDocs2.WebServices.Core.Tests.csproj
dotnet test tests/Hybrid.CleverDocs2.WebServices.Api.Tests/Hybrid.CleverDocs2.WebServices.Api.Tests.csproj
dotnet test tests/Hybrid.CleverDocs2.WebServices.Infrastructure.Tests/Hybrid.CleverDocs2.WebServices.Infrastructure.Tests.csproj
dotnet test tests/Hybrid.CleverDocs2.WebServices.IntegrationTests/Hybrid.CleverDocs2.WebServices.IntegrationTests.csproj
```

Per dettagli sulle strategie di testing, consultare [Testing Backend](./docs/testing_backend.md).

## Best Practices

- Seguire i principi di Clean Architecture
- Utilizzare il pattern Repository per l'accesso ai dati
- Implementare la validazione dei modelli con FluentValidation
- Utilizzare il middleware di gestione eccezioni per gestire gli errori in modo consistente
- Implementare logging dettagliato per diagnostica e troubleshooting
- Utilizzare health checks per monitorare lo stato del sistema

## Contribuzione

Seguire le linee guida di sviluppo definite nel progetto:

- Utilizzare feature branches per nuove funzionalità
- Scrivere test per nuovo codice
- Seguire le convenzioni di codice definite in `.editorconfig`
- Sottoporre pull request per review

## Licenza

Copyright (c) 2025. Tutti i diritti riservati.
