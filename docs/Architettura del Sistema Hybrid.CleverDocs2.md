# Architettura del Sistema Hybrid.CleverDocs2

## Panoramica

Hybrid.CleverDocs2 è una WebUI multitenant enterprise-grade progettata per interfacciarsi con SciPhi AI R2R API. Il sistema permette la creazione e gestione di collezioni di documenti personalizzate e protette per utente, con interazione in linguaggio naturale tramite LLM API.

L'architettura è strutturata in due componenti principali:
1. **Hybrid.CleverDocs2.WebUI**: Frontend Blazor con MudBlazor
2. **Hybrid.CleverDocs2.WebServices**: Backend API server per comunicazione con R2R

Questa separazione permette una chiara divisione delle responsabilità e facilita lo sviluppo e la manutenzione del sistema.

## Struttura del Repository

```
Hybrid.CleverDocs2/
├── docs/                              # Documentazione generale
├── shared/                            # Codice e modelli condivisi
├── Hybrid.CleverDocs2.WebUI/          # Progetto frontend Blazor
├── Hybrid.CleverDocs2.WebServices/    # Progetto backend API
└── README.md                          # README principale
```

## Architettura Generale

### Diagramma Architetturale

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Client Browser                               │
└───────────────────────────────┬─────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                  Hybrid.CleverDocs2.WebUI                           │
│                                                                     │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────┐   ┌──────────┐ │
│  │    Pages    │   │   Shared    │   │  Services   │   │  Models  │ │
│  │             │   │ Components  │   │             │   │          │ │
│  └─────────────┘   └─────────────┘   └─────────────┘   └──────────┘ │
└───────────────────────────────┬─────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                 Hybrid.CleverDocs2.WebServices                      │
│                                                                     │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────┐   ┌──────────┐ │
│  │     API     │   │    Core     │   │Infrastructure│  │  Workers │ │
│  │ Controllers │   │  Services   │   │ Repositories │  │          │ │
│  └─────────────┘   └─────────────┘   └─────────────┘   └──────────┘ │
└───────────────────────────────┬─────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      External Services                              │
│                                                                     │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────┐                │
│  │  SciPhi AI  │   │ PostgreSQL  │   │   Redis     │   ┌──────────┐ │
│  │  R2R API    │   │  Database   │   │   Cache     │   │ RabbitMQ │ │
│  └─────────────┘   └─────────────┘   └─────────────┘   └──────────┘ │
└─────────────────────────────────────────────────────────────────────┘
```

### Componenti Principali

#### 1. Frontend (Hybrid.CleverDocs2.WebUI)

Il frontend è implementato utilizzando Blazor .NET 9.0 con MudBlazor come framework UI. Fornisce un'interfaccia utente intuitiva e responsive per:

- Autenticazione e gestione utenti
- Dashboard personalizzate per ruoli (Admin, Company, User)
- Gestione collezioni e documenti
- Interfaccia chatbot per interazione in linguaggio naturale
- Impostazioni e configurazioni

Per dettagli specifici sul frontend, consultare:
- [README Frontend](./Hybrid.CleverDocs2.WebUI/README.md)
- [Documentazione Dettagliata Frontend](./Hybrid.CleverDocs2.WebUI/docs/README_frontend_blazor.md)

#### 2. Backend (Hybrid.CleverDocs2.WebServices)

Il backend è implementato come un servizio API RESTful in .NET 9.0, strutturato secondo i principi di Clean Architecture:

- **API Layer**: Controllers, middleware, filtri e configurazione API
- **Core Layer**: Logica di business, interfacce e modelli di dominio
- **Infrastructure Layer**: Implementazioni concrete (database, servizi esterni, messaging, caching)
- **Workers**: Servizi background per elaborazione asincrona

Per dettagli specifici sul backend, consultare:
- [README Backend](./Hybrid.CleverDocs2.WebServices/README.md)
- [Documentazione Dettagliata Backend](./Hybrid.CleverDocs2.WebServices/docs/README_backend_api_server.md)

#### 3. Servizi Esterni

Il sistema si integra con diversi servizi esterni:

- **SciPhi AI R2R API**: Per l'elaborazione dei documenti e l'interazione con il chatbot
- **PostgreSQL**: Database relazionale per la persistenza dei dati
- **Redis**: Per il caching avanzato
- **RabbitMQ**: Per la gestione delle code e l'elaborazione asincrona

## Flussi di Dati Principali

### 1. Upload e Elaborazione Documenti

```
┌────────────┐    ┌────────────┐    ┌────────────┐    ┌────────────┐
│   WebUI    │───>│ WebServices│───>│  RabbitMQ  │───>│  Workers   │
└────────────┘    └────────────┘    └────────────┘    └────────────┘
                                                           │
                                                           ▼
                                                     ┌────────────┐
                                                     │  R2R API   │
                                                     └────────────┘
                                                           │
                                                           ▼
┌────────────┐    ┌────────────┐    ┌────────────┐    ┌────────────┐
│   WebUI    │<───│ WebServices│<───│  RabbitMQ  │<───│  Workers   │
└────────────┘    └────────────┘    └────────────┘    └────────────┘
```

1. L'utente carica un documento tramite WebUI
2. WebUI invia il documento a WebServices
3. WebServices salva il documento nel database e invia un messaggio a RabbitMQ
4. Un worker service elabora il messaggio e invia il documento a R2R API
5. R2R API elabora il documento e restituisce i risultati
6. Il worker service aggiorna il database e invia una notifica tramite RabbitMQ
7. WebServices riceve la notifica e aggiorna lo stato del documento
8. WebUI mostra lo stato aggiornato all'utente

### 2. Interazione con il Chatbot

```
┌────────────┐    ┌────────────┐    ┌────────────┐
│   WebUI    │───>│ WebServices│───>│  R2R API   │
└────────────┘    └────────────┘    └────────────┘
      ▲                                   │
      └───────────────────────────────────┘
```

1. L'utente seleziona le collezioni e invia una domanda tramite WebUI
2. WebUI invia la domanda e le collezioni selezionate a WebServices
3. WebServices invia la richiesta a R2R API
4. R2R API elabora la domanda e restituisce la risposta
5. WebServices inoltra la risposta a WebUI
6. WebUI mostra la risposta all'utente

## Multitenancy

Il sistema è progettato con un approccio multitenant, che permette a più aziende (Companies) di utilizzare la stessa istanza dell'applicazione mantenendo i dati isolati:

- **Isolamento Dati**: Ogni Company ha accesso solo ai propri dati
- **Filtri Automatici**: Implementati a livello di database tramite Entity Framework Core
- **Middleware di Risoluzione Tenant**: Identifica il tenant corrente in base all'utente autenticato
- **Caching con Namespace**: Ogni tenant ha il proprio namespace nel cache Redis

Per dettagli sull'implementazione della multitenancy, consultare:
- [Gestione Multitenancy](./Hybrid.CleverDocs2.WebServices/docs/gestione_multitenancy.md)

## Gestione Code con RabbitMQ

Per gestire l'elaborazione asincrona dei documenti e superare i limiti di R2R nell'elaborare upload di troppi documenti, il sistema utilizza RabbitMQ:

- **Code Separate**: Per diversi tipi di operazioni (upload, parsing, indicizzazione)
- **Worker Services**: Servizi dedicati per l'elaborazione dei messaggi
- **Circuit Breaker**: Per gestire errori e fallimenti
- **Retry con Backoff Esponenziale**: Per operazioni fallite
- **Dead Letter Queue**: Per messaggi non elaborabili

Per dettagli sull'implementazione del sistema di code, consultare:
- [Gestione Code RabbitMQ](./Hybrid.CleverDocs2.WebServices/docs/gestione_code_rabbitmq.md)

## Caching con Redis

Il sistema utilizza Redis per implementare una strategia di caching avanzata:

- **Caching Multi-livello**: Per diversi tipi di dati (documenti, collezioni, risultati chatbot)
- **Caching Tenant-aware**: Isolamento dei dati in cache per tenant
- **Invalidazione Selettiva**: Invalidazione mirata della cache quando i dati cambiano
- **Distributed Locking**: Per operazioni concorrenti
- **Rate Limiting**: Per limitare le richieste a R2R API

Per dettagli sull'implementazione del caching, consultare:
- [Caching Redis](./Hybrid.CleverDocs2.WebServices/docs/caching_redis_implementazione.md)

## Sicurezza

Il sistema implementa diverse misure di sicurezza:

- **Autenticazione JWT**: Con refresh token
- **Autorizzazione Basata su Ruoli e Policy**: Admin, Company, User
- **HTTPS**: Per tutte le comunicazioni
- **Validazione Input**: Per prevenire attacchi di iniezione
- **CORS**: Configurato per permettere solo origini autorizzate
- **Audit Trail**: Logging di tutte le operazioni sensibili

Per dettagli sull'implementazione della sicurezza, consultare:
- [Autenticazione e Autorizzazione](./Hybrid.CleverDocs2.WebServices/docs/autenticazione_autorizzazione.md)
- [Sicurezza API](./Hybrid.CleverDocs2.WebServices/docs/sicurezza_api.md)

## Scalabilità e Robustezza

Il sistema è progettato per essere scalabile e robusto:

- **Architettura Stateless**: Facilita lo scaling orizzontale
- **Load Balancing**: Supporto per distribuire il carico su più istanze
- **Caching Distribuito**: Con Redis
- **Messaging Asincrono**: Con RabbitMQ
- **Health Checks**: Per monitorare lo stato del sistema
- **Circuit Breaker**: Per gestire fallimenti di servizi esterni
- **Logging Centralizzato**: Per diagnostica e troubleshooting

Per dettagli sulla scalabilità e robustezza, consultare:
- [Scalabilità e Robustezza](./docs/architettura/scalabilita_robustezza.md)

## Deployment

Il sistema può essere deployato in diversi ambienti:

- **Sviluppo**: Per sviluppo e testing locale
- **Staging**: Per test di integrazione e UAT
- **Produzione**: Per l'ambiente di produzione

Per dettagli sul deployment, consultare:
- [Guida al Deployment](./docs/deployment/README_architettura_deployment.md)

$(cat /tmp/endpoints_snippet.txt)

## Conclusioni

L'architettura di Hybrid.CleverDocs2 è progettata per essere robusta, scalabile e facile da mantenere. La separazione tra frontend e backend, l'uso di tecnologie moderne come Blazor, PostgreSQL, Redis e RabbitMQ, e l'implementazione di pattern come Clean Architecture e CQRS, permettono di creare un sistema enterprise-grade che soddisfa i requisiti di multitenancy, gestione documenti e interazione chatbot.

Per iniziare a lavorare con il sistema, consultare:
- [README WebUI](./Hybrid.CleverDocs2.WebUI/README.md)
- [README WebServices](./Hybrid.CleverDocs2.WebServices/README.md)
