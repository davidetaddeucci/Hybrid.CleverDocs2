# Test Document per Workflow Upload

## Introduzione
Questo è un documento di test per verificare l'intero workflow di upload dei documenti nel sistema Hybrid.CleverDocs2.

## Obiettivi del Test
1. Verificare che l'upload da WebUI funzioni correttamente
2. Monitorare le chiamate a WebServices
3. Verificare le chiamate a R2R API
4. Controllare gli aggiornamenti del database
5. Verificare gli aggiornamenti real-time via SignalR

## Contenuto del Test
Questo documento contiene abbastanza testo per essere processato da R2R ma non troppo per evitare timeout durante il test.

### Sezione 1: Architettura del Sistema
Il sistema Hybrid.CleverDocs2 è composto da:
- WebUI (ASP.NET Core MVC)
- WebServices (ASP.NET Core API)
- Database PostgreSQL
- R2R API per document processing
- Redis per caching
- RabbitMQ per messaging

### Sezione 2: Workflow di Upload
Il workflow di upload segue questi step:
1. User seleziona file nel WebUI
2. JavaScript valida e invia a WebServices
3. WebServices salva file e crea record DB
4. File viene inviato a R2R per processing
5. R2R processa e restituisce IDs
6. Database viene aggiornato con R2R IDs
7. SignalR notifica WebUI degli aggiornamenti
8. WebUI aggiorna l'interfaccia utente

### Sezione 3: Verifica del Bug Fix
Il bug che abbiamo risolto causava:
- Chiamate ripetute a RestoreProcessingQueueFromDatabaseAsync ogni 30 secondi
- Potenziali modifiche spurie agli status dei documenti
- Interferenze nel workflow di processing

La soluzione implementata:
- Flag statico per eseguire restoration solo una volta
- Riutilizzo delle istanze di servizio nel worker
- Thread-safe lock per prevenire race conditions

## Conclusioni
Questo test dovrebbe dimostrare che il workflow funziona correttamente e che il bug è stato risolto definitivamente.
