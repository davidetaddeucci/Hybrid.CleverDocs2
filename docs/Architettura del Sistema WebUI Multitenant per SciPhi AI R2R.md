# Architettura del Sistema WebUI Multitenant per SciPhi AI R2R

## Panoramica

Questo documento descrive l'architettura complessiva di un sistema WebUI multitenant enterprise-grade per l'accesso e l'interazione con i servizi offerti da un server SciPhi AI R2R API. Il sistema è progettato per supportare operazioni di creazione e gestione di collezioni di documenti personalizzate e protette per utente, e per consentire l'interazione con queste collezioni tramite linguaggio naturale utilizzando le API LLM.

## Requisiti Principali

- **Multitenancy**: Supporto per più aziende (Companies) e utenti con isolamento dei dati
- **Gestione Ruoli**: Admin, Company, User con permessi differenziati
- **Integrazione R2R**: Comunicazione con il backend R2R API server esistente
- **Gestione Code**: Sistema avanzato per gestire l'upload di documenti
- **Caching**: Utilizzo di Redis per ottimizzare le performance
- **Sicurezza**: Protezione dei dati e autenticazione robusta
- **Scalabilità**: Architettura enterprise-grade per gestire carichi elevati

## Architettura Generale

Il sistema è composto da tre componenti principali:

1. **Frontend Blazor**: Interfaccia utente basata su Microsoft .NET 9.0 Blazor con MudBlazor
2. **Backend API Server**: Servizi .NET 9.0 Web API per la comunicazione con R2R
3. **Database e Storage**: SQL Server per i dati applicativi e Redis per il caching

### Diagramma Architetturale

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│                 │     │                 │     │                 │
│  Frontend       │     │  Backend        │     │  R2R API Server │
│  Blazor         │◄────┤  API Server     │◄────┤  (Esistente)    │
│  (.NET 9.0)     │     │  (.NET 9.0)     │     │                 │
│                 │     │                 │     │                 │
└────────┬────────┘     └────────┬────────┘     └─────────────────┘
         │                       │
         │                       │
┌────────▼────────┐     ┌────────▼────────┐     ┌─────────────────┐
│                 │     │                 │     │                 │
│  SQL Server     │     │  Redis Cache    │     │  Queue System   │
│  (192.168.1.4)  │     │  (192.168.1.4)  │     │                 │
│                 │     │                 │     │                 │
└─────────────────┘     └─────────────────┘     └─────────────────┘
```

## Componenti del Sistema

### 1. Frontend Blazor (.NET 9.0)

Il frontend è implementato utilizzando Microsoft .NET 9.0 Blazor con componenti MudBlazor per un'interfaccia utente moderna e reattiva.

**Caratteristiche principali**:
- Architettura MVVM (Model-View-ViewModel)
- Componenti riutilizzabili per ogni funzionalità
- Routing basato su ruoli
- Interfaccia responsive per desktop e mobile
- Gestione stato con Fluxor o simili
- Localizzazione per supporto multilingua
- Validazione lato client

**Workflow principale**:
- Login → Dashboard personalizzata per ruolo → Funzionalità specifiche per ruolo

### 2. Backend API Server (.NET 9.0)

Il backend è implementato come un servizio Web API .NET 9.0 che funge da intermediario tra il frontend e il server R2R API.

**Caratteristiche principali**:
- Architettura Clean con separazione di concerns
- Pattern CQRS per separare operazioni di lettura e scrittura
- Mediator pattern per gestire le richieste
- Sistema di gestione code avanzato
- Caching con Redis
- Logging e monitoraggio completi
- Gestione errori centralizzata

**Componenti principali**:
- **API Controllers**: Endpoint REST per le operazioni del frontend
- **Service Layer**: Logica di business e orchestrazione
- **Repository Layer**: Accesso ai dati
- **Queue Manager**: Gestione delle code per upload documenti
- **R2R Client**: Client per comunicare con il server R2R API
- **Auth Service**: Gestione autenticazione e autorizzazione

### 3. Sistema di Gestione Code

Data la limitazione di R2R nell'elaborare troppi documenti contemporaneamente, è implementato un sistema di gestione code avanzato.

**Caratteristiche**:
- Code prioritarie basate su tenant e tipo di operazione
- Throttling per evitare sovraccarichi del server R2R
- Retry automatico con backoff esponenziale
- Monitoraggio dello stato delle operazioni
- Notifiche in tempo reale sullo stato delle operazioni
- Possibilità di annullare operazioni in coda

**Implementazione**:
- Utilizzo di un sistema di code distribuito
- Persistenza delle code per resistere ai riavvii
- Worker services per processare le code in background

### 4. Database e Storage

**SQL Server**:
- Schema multitenant con isolamento dei dati
- Tabelle principali: Companies, Users, Settings, Collections, Documents, Logs
- Stored procedures ottimizzate per operazioni comuni
- Indici appropriati per query frequenti

**Redis Cache**:
- Caching di sessioni utente
- Caching di risultati di query frequenti
- Caching di configurazioni
- Stato distribuito per i worker services

## Modello di Multitenancy

Il sistema utilizza un approccio di multitenancy con database condiviso e schema separato per tenant (Shared Database, Separate Schema), che offre un buon equilibrio tra isolamento dei dati e efficienza delle risorse.

**Vantaggi di questo approccio**:
- Migliore isolamento dei dati rispetto a schema condiviso
- Più efficiente rispetto a database separati per tenant
- Facilità di backup e ripristino
- Possibilità di personalizzazione per tenant

**Implementazione**:
- Ogni Company ha il proprio schema nel database
- Tabelle di sistema in uno schema condiviso
- Discriminatore di tenant in ogni query
- Middleware per la risoluzione automatica del tenant

## Sicurezza

**Autenticazione**:
- JWT (JSON Web Tokens) per l'autenticazione
- Refresh token per sessioni prolungate
- Possibilità di integrazione con provider esterni (OAuth)
- MFA (Multi-Factor Authentication) per account sensibili

**Autorizzazione**:
- RBAC (Role-Based Access Control) con ruoli Admin, Company, User
- Politiche di autorizzazione granulari
- Validazione delle richieste a più livelli

**Protezione dati**:
- Crittografia dei dati sensibili
- HTTPS per tutte le comunicazioni
- Sanitizzazione degli input
- Protezione contro attacchi comuni (CSRF, XSS, SQL Injection)

## Scalabilità e Robustezza

**Scalabilità**:
- Architettura stateless per facilitare lo scaling orizzontale
- Caching distribuito con Redis
- Bilanciamento del carico tra istanze
- Database sharding per grandi volumi di dati

**Robustezza**:
- Circuit breaker pattern per gestire fallimenti di servizi esterni
- Health checks per tutti i componenti
- Graceful degradation in caso di servizi non disponibili
- Logging completo per diagnostica e audit

## Integrazione con R2R API

L'integrazione con il server R2R API esistente avviene attraverso un client dedicato nel backend API server.

**Caratteristiche**:
- Mappatura completa degli endpoint R2R
- Gestione automatica dell'autenticazione
- Retry per richieste fallite
- Circuit breaker per evitare sovraccarichi
- Caching dei risultati quando appropriato
- Logging dettagliato delle interazioni

**Endpoint principali utilizzati**:
- Gestione documenti (upload, parsing, estrazione)
- Gestione collezioni
- Interrogazione chatbot
- Autenticazione e gestione utenti

## Conclusioni

L'architettura proposta è progettata per essere robusta, scalabile e sicura, soddisfacendo i requisiti di un sistema enterprise-grade. La separazione in componenti distinti (Frontend Blazor, Backend API Server) permette una manutenzione più semplice e una migliore scalabilità. Il sistema di gestione code avanzato risolve la limitazione di R2R nell'elaborare troppi documenti contemporaneamente, mentre l'utilizzo di Redis per il caching migliora le performance complessive.

Questa architettura fornisce una base solida per lo sviluppo del sistema, con chiare linee guida per l'implementazione di ciascun componente.
