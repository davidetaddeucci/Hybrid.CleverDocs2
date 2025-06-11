# Progetto WebUI Multitenant per SciPhi AI R2R

## Raccolta Documentazione e Analisi
- [x] Analizzare i requisiti utente
- [x] Raccogliere documentazione introduttiva su SciPhi R2R
- [x] Esaminare le API e limitazioni di R2R
  - [x] Studiare gli endpoint di gestione documenti
  - [x] Studiare gli endpoint di autenticazione e gestione utenti
  - [x] Studiare gli endpoint di gestione collezioni
  - [x] Identificare i limiti di upload e processamento documenti
  - [x] Analizzare le modalità di integrazione con sistemi esterni
- [x] Ricercare best practices per WebUI multitenant enterprise-grade
- [ ] Definire l'architettura complessiva del sistema

## Progettazione Database e Backend
- [x] Progettare il modello dati PostgreSQL
- [ ] Progettare i flussi di autenticazione e autorizzazione per i ruoli
- [x] Definire l'integrazione con R2R e RabbitMQ per la gestione delle code
- [x] Definire l'integrazione con Redis per il caching avanzato

## Progettazione Frontend

## WebServices Backend API

- [x] Scaffolding R2ROptions, AuthClient & DI registrations
- [ ] Implement DocumentClient with CRUD and DI registration
- [x] Implement ConversationClient with CRUD and DI registration
- [ ] Implement PromptClient with CRUD and DI registration
- [ ] Implement IngestionClient with CRUD and DI registration
- [ ] Implement GraphClient with CRUD and DI registration
- [ ] Implement SearchClient with CRUD and DI registration
- [ ] Implement ToolsClient with CRUD and DI registration
- [ ] Implement MaintenanceClient with CRUD and DI registration
- [ ] Implement OrchestrationClient with CRUD and DI registration
- [ ] Implement LocalLLMClient with CRUD and DI registration
- [ ] Implement ValidationClient with CRUD and DI registration
- [ ] Implement McpTuningClient with CRUD and DI registration
- [ ] Implement WebDevClient with CRUD and DI registration
- [x] Progettare l'interfaccia utente con MudBlazor
- [x] Dettagliare i workflow per i diversi ruoli (Admin, Company, User)
- [x] Definire componenti riutilizzabili e layout responsive

## Documentazione Tecnica
- [x] Redigere README per il backend API server
- [x] Redigere README per il frontend Blazor
- [x] Redigere README per l'architettura e il deployment
- [x] Validare la completezza della documentazione
- [x] Preparare il report finale per la consegna all'utente
