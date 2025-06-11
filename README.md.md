# Hybrid.CleverDocs2

## Panoramica

Hybrid.CleverDocs2 è una WebUI multitenant enterprise-grade per accedere in concorrenza a tutti i servizi esibiti da un SciPhi AI R2R API server. Il sistema permette la creazione e gestione di collezioni di documenti personalizzate e protette per utente, con interazione in linguaggio naturale tramite LLM API.

Il progetto è strutturato in due componenti principali:
- **Hybrid.CleverDocs2.WebUI**: Frontend Blazor con MudBlazor
- **Hybrid.CleverDocs2.WebServices**: Backend API server per comunicazione con R2R

## Obiettivi del Sistema

- Fornire un'interfaccia utente intuitiva e accessibile per utenti non tecnici
- Implementare un sistema robusto di gestione code per superare i limiti di R2R nell'elaborare upload di troppi documenti
- Garantire l'isolamento dei dati in un ambiente multitenant
- Ottimizzare le performance con caching avanzato
- Offrire un'esperienza di uso intuitiva, facile, accattivante e non respingente

## Struttura del Repository

```
Hybrid.CleverDocs2/
├── docs/                              # Documentazione generale
│   ├── architettura/                  # Documenti di architettura
│   ├── deployment/                    # Guide di deployment
│   └── img/                           # Immagini e diagrammi
├── shared/                            # Codice e modelli condivisi
├── Hybrid.CleverDocs2.WebUI/          # Progetto frontend Blazor
├── Hybrid.CleverDocs2.WebServices/    # Progetto backend API
└── README.md                          # Questo file
```

## Documentazione

### Documentazione Generale

La documentazione generale del sistema si trova nella directory `docs/` e copre gli aspetti architetturali e di deployment comuni a entrambi i sottoprogetti:

- [Architettura del Sistema](./docs/architettura/architettura_sistema.md): Descrizione dettagliata dell'architettura complessiva
- [Scalabilità e Robustezza](./docs/architettura/scalabilita_robustezza.md): Considerazioni enterprise-grade
- [Modello Dati PostgreSQL](./docs/architettura/modello_dati_postgresql.md): Schema del database condiviso
- [Integrazione con R2R e RabbitMQ](./docs/architettura/integrazione_r2r_rabbitmq.md): Visione generale dell'integrazione
- [Integrazione con Redis](./docs/architettura/integrazione_redis.md): Visione generale del caching
- [Guida al Deployment](./docs/deployment/README_architettura_deployment.md): Istruzioni per il deployment del sistema

### Documentazione Specifica dei Sottoprogetti

Ogni sottoprogetto ha la propria documentazione specifica nella rispettiva directory:

- [README Frontend](./Hybrid.CleverDocs2.WebUI/README.md): Guida al frontend Blazor
- [README Backend](./Hybrid.CleverDocs2.WebServices/README.md): Guida al backend API server

## Ruoli Utente

Il sistema supporta tre ruoli principali:

1. **Admin**:
   - Crea e gestisce Companies e Users
   - Configura impostazioni globali e specifiche per Company
   - Gestisce configurazioni di R2R API

2. **Company**:
   - Consulta dati di monitoring
   - Visualizza tutte le basi di conoscenza dei propri Users
   - Interroga il chatbot su tutte le collezioni

3. **User**:
   - Gestisce diverse collections
   - Carica e cancella files nelle sue collections
   - Interroga il chatbot selezionando quali collections utilizzare

## Tecnologie Principali

- **Frontend**: Microsoft .NET 9.0 Blazor con MudBlazor
- **Backend**: Microsoft .NET 9.0 Web API
- **Database**: PostgreSQL
- **Message Broker**: RabbitMQ
- **Caching**: Redis
- **Integrazione**: SciPhi AI R2R API

## Prerequisiti

- .NET 9.0 SDK
- PostgreSQL 16+
- Redis 7.0+
- RabbitMQ 3.12+
- SciPhi AI R2R API server attivo (http://192.168.1.4:7272)

## Quick Start

1. Clonare il repository
   ```bash
   git clone https://github.com/hybrid/cleverdocs2.git
   cd Hybrid.CleverDocs2
   ```

2. Configurare le connessioni a PostgreSQL, Redis, RabbitMQ e R2R API nei rispettivi file di configurazione

3. Avviare il backend WebServices
   ```bash
   cd Hybrid.CleverDocs2.WebServices
   dotnet run --project src/Hybrid.CleverDocs2.WebServices.Api/Hybrid.CleverDocs2.WebServices.Api.csproj
   ```

4. Avviare il frontend WebUI
   ```bash
   cd Hybrid.CleverDocs2.WebUI
   dotnet run --project src/Hybrid.CleverDocs2.WebUI.csproj
   ```

5. Accedere all'applicazione tramite browser all'indirizzo indicato (solitamente `https://localhost:7000`)

Per istruzioni dettagliate, consultare:
- [Guida al Deployment](./docs/deployment/README_architettura_deployment.md)
- [README WebUI](./Hybrid.CleverDocs2.WebUI/README.md)
- [README WebServices](./Hybrid.CleverDocs2.WebServices/README.md)

## Contribuzione

Seguire le linee guida di sviluppo definite nel progetto:
- Utilizzare feature branches per nuove funzionalità
- Scrivere test per nuovo codice
- Seguire le convenzioni di codice definite in `.editorconfig`
- Sottoporre pull request per review

## Licenza

Copyright (c) 2025. Tutti i diritti riservati.
