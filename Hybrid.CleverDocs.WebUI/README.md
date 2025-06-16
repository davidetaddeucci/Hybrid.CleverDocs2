# README: Frontend MVC per Hybrid.CleverDocs2

## Panoramica

Hybrid.CleverDocs2.WebUI è l'interfaccia utente del sistema, sviluppata in .NET 9.0 ASP.NET Core MVC con Material Design 3. Fornisce un'esperienza utente intuitiva e accattivante per la gestione di collezioni di documenti e l'interazione con il chatbot basato su LLM.

Questo documento fornisce una guida specifica per il frontend MVC, con riferimenti alla documentazione dettagliata disponibile nella directory `docs/`.

## Caratteristiche Principali

- Dashboard personalizzate per ruoli Admin, Company e User
- Gestione collezioni e documenti
- Interfaccia chatbot per interazione in linguaggio naturale
- Design responsive e accessibile con Material Design 3
- Supporto multitenancy
- Architettura MVC con pattern MVVM per ViewModels
- **🚀 Ottimizzazioni Performance**:
  - Caching Redis con autenticazione (192.168.1.4:6380)
  - Caching dual-layer (Memory + Redis)
  - Caricamento API parallelo per dashboard
  - Tempo di caricamento dashboard < 2 secondi
  - Endpoint di monitoraggio performance

## Prerequisiti

- .NET 9.0 SDK
- Visual Studio 2022+ o Visual Studio Code con estensioni C#
- Backend WebServices in esecuzione

## Struttura del Progetto

```
Hybrid.CleverDocs.WebUI/
├── Controllers/                          # MVC Controllers per gestione richieste
├── Views/                                # Razor Views (.cshtml) per UI
│   ├── Auth/                             # Views per autenticazione
│   ├── Dashboard/                        # Views per dashboard
│   ├── AdminDashboard/                   # Views per admin
│   ├── CompanyDashboard/                 # Views per company admin
│   ├── UserDashboard/                    # Views per utenti
│   └── Shared/                           # Layout e componenti condivisi
├── ViewModels/                           # ViewModels per pattern MVVM
├── Services/                             # Servizi frontend (API clients)
├── Models/                               # Modelli e DTOs
├── wwwroot/                              # File statici (CSS, JS, immagini)
└── README.md                             # Questo file
```

## Documentazione

Per una comprensione approfondita del frontend, consultare i seguenti documenti:

### Documentazione Specifica del Frontend

- [Documentazione Dettagliata del Frontend](./docs/README_frontend_mvc.md)
- [Progettazione dell'Interfaccia Utente](./docs/interfaccia_utente.md)
- [Workflow per i Ruoli Utente](./docs/workflow_ruoli_dettagliati.md)
- [Guida al Template Grafico](./docs/guida_template_grafico.md)
- [Implementazione dei Componenti Bootstrap](./docs/implementazione_componenti.md)
- [Gestione dello Stato Frontend](./docs/gestione_stato_frontend.md)
- [Integrazione con WebServices](./docs/integrazione_webservices.md)
- [Testing Frontend](./docs/testing_frontend.md)
- [Localizzazione e Internazionalizzazione](./docs/localizzazione.md)

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

2. Navigare nella directory `Hybrid.CleverDocs2.WebUI`
   ```bash
   cd Hybrid.CleverDocs2.WebUI
   ```

3. Ripristinare le dipendenze
   ```bash
   dotnet restore
   ```

4. (Opzionale) Installare dipendenze npm e compilare CSS
   ```bash
   cd src
   npm install
   npm run build:css
   ```

5. Avviare l'applicazione
   ```bash
   dotnet run --project src/Hybrid.CleverDocs2.WebUI.csproj
   ```

6. Aprire il browser all'URL indicato (solitamente `https://localhost:7XXX` o `http://localhost:5XXX`)

## Configurazione

La configurazione principale dell'applicazione si trova in `src/appsettings.json`. È possibile sovrascrivere queste impostazioni in `appsettings.Development.json` per l'ambiente di sviluppo.

Principali impostazioni:

```json
{
  "BackendApiUrl": "https://localhost:7272",
  "Authentication": {
    "TokenStorageKey": "auth_token",
    "RefreshTokenStorageKey": "refresh_token"
  },
  "UI": {
    "DefaultTheme": "light",
    "EnableDarkMode": true
  }
}
```

## Ruoli e Funzionalità

### Admin
- Gestione Companies (creazione, modifica, eliminazione)
- Gestione Users globale
- Configurazione impostazioni globali
- Monitoraggio sistema

### Company
- Gestione Users della propria Company
- Visualizzazione di tutte le collezioni degli Users
- Interrogazione chatbot su tutte le collezioni
- Monitoraggio attività

### User
- Gestione collezioni personali
- Upload e gestione documenti
- Interrogazione chatbot sulle proprie collezioni
- Gestione profilo personale

Per dettagli sui workflow specifici per ciascun ruolo, consultare [Workflow per i Ruoli Utente](./docs/workflow_ruoli_dettagliati.md).

## Interfaccia Utente

L'interfaccia utente è costruita con Bootstrap 5, un framework CSS moderno per applicazioni web responsive. Il design è responsive e accessibile, con un focus sulla semplicità e sull'usabilità per utenti non tecnici.

Principali componenti UI:

- Dashboard personalizzate per ruolo
- Gestione collezioni e documenti
- Interfaccia chatbot
- Gestione utenti e profili
- Impostazioni e configurazioni

Per dettagli sulla progettazione dell'interfaccia utente, consultare [Progettazione dell'Interfaccia Utente](./docs/interfaccia_utente.md) e [Guida al Template Grafico](./docs/guida_template_grafico.md).

## Integrazione con Backend

Il frontend comunica con il backend tramite API RESTful. I servizi di integrazione si trovano nella directory `src/Services/Api/`.

Principali servizi di integrazione:

- `AuthApiClient`: Autenticazione e gestione token
- `CollectionApiClient`: Gestione collezioni
- `DocumentApiClient`: Gestione documenti
- `ChatApiClient`: Interazione con chatbot
- `UserApiClient`: Gestione utenti

Per dettagli sull'integrazione con il backend, consultare [Integrazione con WebServices](./docs/integrazione_webservices.md).

## Testing

Il progetto include test unitari, di integrazione e end-to-end:

- Test unitari: Per componenti e servizi
- Test di integrazione: Per client API
- Test E2E: Per pagine e workflow

Eseguire i test:
```bash
dotnet test tests/Hybrid.CleverDocs2.WebUI.Tests.csproj
```

Per dettagli sulle strategie di testing, consultare [Testing Frontend](./docs/testing_frontend.md).

## Best Practices

- Utilizzare componenti riutilizzabili in `Shared/Components/`
- Seguire il pattern di gestione stato descritto in [Gestione dello Stato Frontend](./docs/gestione_stato_frontend.md)
- Utilizzare i servizi di autenticazione e API client per comunicare con il backend
- Seguire le linee guida UI/UX descritte in [Guida al Template Grafico](./docs/guida_template_grafico.md)
- Implementare la localizzazione come descritto in [Localizzazione e Internazionalizzazione](./docs/localizzazione.md)

## Contribuzione

Seguire le linee guida di sviluppo definite nel progetto:

- Utilizzare feature branches per nuove funzionalità
- Scrivere test per nuovo codice
- Seguire le convenzioni di codice definite in `.editorconfig`
- Sottoporre pull request per review

## Licenza

Copyright (c) 2025. Tutti i diritti riservati.
