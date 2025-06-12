# Workflow Dettagliati per Ruoli

## Panoramica

Questo documento descrive in dettaglio i workflow operativi per ciascuno dei tre ruoli principali del sistema WebUI multitenant per SciPhi AI R2R: Admin, Company e User. Per ogni ruolo, vengono descritti i percorsi utente, le interazioni principali, i casi d'uso critici e i punti di controllo.

## Workflow per Ruolo Admin

L'Admin è l'amministratore di sistema con accesso completo a tutte le funzionalità e responsabile della gestione globale della piattaforma.

### 1. Onboarding di una Nuova Company

**Descrizione**: Processo di creazione e configurazione di una nuova azienda/tenant nel sistema.

**Flusso**:
1. **Accesso Dashboard Admin**
   - Admin accede al sistema con credenziali di amministratore
   - Sistema verifica l'autenticazione e reindirizza alla dashboard Admin

2. **Creazione Company**
   - Admin naviga alla sezione "Companies Management"
   - Seleziona "Create New Company"
   - Compila il form con:
     - Nome azienda
     - Informazioni di contatto
     - Upload logo aziendale
     - Impostazioni iniziali
   - Sistema valida i dati inseriti
   - Sistema crea nuovo schema database per la Company
   - Sistema genera configurazioni predefinite

3. **Configurazione R2R per Company**
   - Admin configura parametri R2R specifici:
     - Modello LLM predefinito
     - Limiti di documenti per utente
     - Limiti di query per ora
   - Sistema salva configurazioni nel database

4. **Creazione Admin Company**
   - Admin compila form per il primo utente Company (admin aziendale):
     - Nome e cognome
     - Email
     - Username
   - Sistema genera password temporanea
   - Sistema crea l'utente con ruolo "Company"
   - Sistema invia email di invito con credenziali temporanee

5. **Verifica e Attivazione**
   - Admin verifica la corretta creazione della Company
   - Attiva la Company nel sistema
   - Sistema rende disponibile l'accesso per l'admin aziendale

**Punti di Controllo**:
- Validazione dei dati aziendali prima della creazione
- Verifica della corretta creazione dello schema database
- Controllo dell'invio email all'admin aziendale
- Verifica dei permessi assegnati all'admin aziendale

### 2. Monitoraggio e Gestione Sistema

**Descrizione**: Attività di supervisione e manutenzione dell'intero sistema.

**Flusso**:
1. **Accesso Dashboard di Monitoraggio**
   - Admin accede alla sezione "System Monitoring"
   - Sistema carica metriche in tempo reale:
     - Utilizzo risorse (CPU, memoria, disco)
     - Stato delle code
     - Numero di utenti attivi
     - Numero di documenti processati
     - Numero di query eseguite

2. **Analisi Log di Sistema**
   - Admin naviga alla sezione "System Logs"
   - Applica filtri per tipo di log, periodo, severità
   - Sistema visualizza log filtrati
   - Admin esamina log per identificare problemi

3. **Gestione Code**
   - Admin accede alla sezione "Queue Management"
   - Visualizza stato delle diverse code:
     - DocumentUploadQueue
     - DocumentProcessingQueue
     - EntityExtractionQueue
   - Identifica eventuali colli di bottiglia
   - Può intervenire per:
     - Modificare priorità elementi in coda
     - Riavviare elaborazione elementi falliti
     - Cancellare elementi problematici

4. **Configurazione Globale**
   - Admin accede alla sezione "Global Settings"
   - Modifica parametri di sistema:
     - Limiti globali di utilizzo
     - Configurazioni di sicurezza
     - Parametri di connessione R2R
   - Sistema applica modifiche in tempo reale

**Punti di Controllo**:
- Monitoraggio continuo delle metriche di sistema
- Alerting automatico per condizioni anomale
- Verifica dell'impatto delle modifiche di configurazione
- Controllo periodico dei log di sistema

### 3. Gestione Utenti e Permessi

**Descrizione**: Amministrazione degli utenti di sistema e dei loro permessi.

**Flusso**:
1. **Visualizzazione Utenti**
   - Admin accede alla sezione "Users Management"
   - Sistema carica lista di tutti gli utenti con filtri per:
     - Company
     - Ruolo
     - Stato (attivo/inattivo)
   - Admin può cercare utenti specifici

2. **Modifica Utente**
   - Admin seleziona un utente dalla lista
   - Sistema carica dettagli utente
   - Admin può modificare:
     - Informazioni personali
     - Ruolo
     - Stato (attivare/disattivare)
     - Reset password
   - Sistema applica e salva modifiche

3. **Gestione Permessi Speciali**
   - Admin può assegnare permessi speciali a utenti specifici
   - Sistema aggiorna matrice di permessi
   - Sistema registra modifiche nel log di audit

4. **Audit Attività Utenti**
   - Admin accede alla sezione "User Activity"
   - Visualizza log di attività per utente:
     - Login/logout
     - Operazioni critiche eseguite
     - Utilizzo risorse
   - Può esportare report di attività

**Punti di Controllo**:
- Validazione delle modifiche ai ruoli e permessi
- Notifica agli utenti per modifiche significative
- Registrazione di tutte le modifiche nel log di audit
- Verifica periodica degli accessi e delle attività

## Workflow per Ruolo Company

Il ruolo Company rappresenta l'amministratore di una specifica azienda/tenant con accesso alle funzionalità relative alla propria organizzazione.

### 1. Gestione Utenti Aziendali

**Descrizione**: Creazione e gestione degli utenti appartenenti alla propria Company.

**Flusso**:
1. **Accesso Dashboard Company**
   - Company admin accede con le proprie credenziali
   - Sistema verifica l'autenticazione e carica dashboard Company

2. **Visualizzazione Utenti**
   - Company admin naviga alla sezione "Users Management"
   - Sistema carica lista degli utenti della Company
   - Company admin può filtrare e cercare utenti

3. **Creazione Nuovo Utente**
   - Company admin seleziona "Create New User"
   - Compila form con:
     - Nome e cognome
     - Email
     - Username
   - Sistema genera password temporanea
   - Sistema crea utente con ruolo "User"
   - Sistema invia email di invito con credenziali temporanee

4. **Gestione Utenti Esistenti**
   - Company admin può:
     - Modificare informazioni utente
     - Disattivare/riattivare account
     - Resettare password
     - Visualizzare statistiche di utilizzo
   - Sistema applica modifiche e registra nel log

**Punti di Controllo**:
- Verifica che gli utenti creati appartengano solo alla Company corrente
- Controllo dell'invio email agli utenti
- Monitoraggio delle quote di utilizzo per utente
- Registrazione delle modifiche nel log di audit

### 2. Monitoraggio Attività e Utilizzo

**Descrizione**: Supervisione dell'utilizzo delle risorse e delle attività degli utenti della Company.

**Flusso**:
1. **Dashboard di Monitoraggio**
   - Company admin accede alla sezione "Monitoring & Analytics"
   - Sistema carica metriche specifiche della Company:
     - Numero totale di documenti
     - Numero di query eseguite
     - Utilizzo per utente
     - Trend di utilizzo nel tempo

2. **Analisi Collezioni**
   - Company admin naviga alla sezione "Collections Explorer"
   - Sistema carica lista di tutte le collezioni della Company
   - Company admin può:
     - Filtrare per utente, data, dimensione
     - Visualizzare dettagli di ogni collezione
     - Vedere statistiche di utilizzo

3. **Visualizzazione Log**
   - Company admin accede alla sezione "Activity Logs"
   - Sistema carica log filtrati per la Company
   - Company admin può:
     - Filtrare per utente, tipo di attività, periodo
     - Esportare report di attività
     - Identificare pattern di utilizzo

4. **Gestione Quote**
   - Company admin accede alla sezione "Quota Management"
   - Visualizza utilizzo corrente rispetto ai limiti
   - Può allocare quote tra gli utenti
   - Sistema applica nuove allocazioni

**Punti di Controllo**:
- Monitoraggio del rispetto dei limiti globali
- Alerting per utilizzo anomalo o eccessivo
- Verifica periodica dell'efficienza di utilizzo
- Controllo della distribuzione equa delle risorse

### 3. Configurazione Impostazioni Company

**Descrizione**: Personalizzazione delle impostazioni specifiche della Company.

**Flusso**:
1. **Accesso Impostazioni**
   - Company admin naviga alla sezione "Company Settings"
   - Sistema carica configurazioni correnti

2. **Modifica Impostazioni Generali**
   - Company admin può modificare:
     - Informazioni aziendali
     - Logo e branding
     - Preferenze di notifica
     - Impostazioni di visualizzazione
   - Sistema salva modifiche e aggiorna interfaccia

3. **Configurazione R2R**
   - Company admin può personalizzare:
     - Modello LLM preferito
     - Parametri di elaborazione documenti
     - Impostazioni di estrazione entità
   - Sistema valida e applica configurazioni

4. **Gestione API Key**
   - Company admin può:
     - Visualizzare API key corrente (mascherata)
     - Richiedere rotazione API key
     - Configurare restrizioni di utilizzo
   - Sistema gestisce sicurezza delle chiavi

**Punti di Controllo**:
- Validazione delle configurazioni prima dell'applicazione
- Verifica dell'impatto delle modifiche sulle performance
- Sicurezza nella gestione delle API key
- Registrazione delle modifiche nel log di audit

### 4. Esplorazione e Interrogazione Collezioni

**Descrizione**: Accesso e interrogazione di tutte le collezioni di documenti della Company.

**Flusso**:
1. **Accesso Collezioni**
   - Company admin naviga alla sezione "Collections Explorer"
   - Sistema carica lista di tutte le collezioni della Company

2. **Esplorazione Documenti**
   - Company admin seleziona una collezione
   - Sistema carica lista documenti nella collezione
   - Company admin può:
     - Visualizzare dettagli documento
     - Scaricare contenuto originale
     - Vedere metadati e statistiche

3. **Interrogazione Chatbot**
   - Company admin accede all'interfaccia chatbot
   - Seleziona una o più collezioni da interrogare
   - Formula domande in linguaggio naturale
   - Sistema elabora query attraverso R2R
   - Sistema visualizza risposte con citazioni

4. **Analisi Knowledge Graph**
   - Company admin può visualizzare knowledge graph delle collezioni
   - Esplora entità e relazioni estratte
   - Naviga connessioni tra documenti
   - Identifica pattern e insights

**Punti di Controllo**:
- Verifica dei permessi di accesso alle collezioni
- Monitoraggio dell'utilizzo delle query
- Validazione delle richieste al chatbot
- Controllo della qualità delle risposte

## Workflow per Ruolo User

Il ruolo User rappresenta l'utente finale che utilizza il sistema per gestire le proprie collezioni di documenti e interagire con il chatbot.

### 1. Gestione Collezioni Personali

**Descrizione**: Creazione e gestione delle proprie collezioni di documenti.

**Flusso**:
1. **Accesso Dashboard User**
   - User accede con le proprie credenziali
   - Sistema verifica l'autenticazione e carica dashboard User

2. **Visualizzazione Collezioni**
   - User naviga alla sezione "My Collections"
   - Sistema carica lista delle collezioni dell'utente
   - User può filtrare e cercare collezioni

3. **Creazione Nuova Collezione**
   - User seleziona "Create New Collection"
   - Compila form con:
     - Nome collezione
     - Descrizione
     - Eventuali tag o categorie
   - Sistema crea collezione vuota
   - Sistema reindirizza alla vista dettaglio

4. **Gestione Collezione**
   - User può:
     - Modificare nome e descrizione
     - Eliminare collezione (con conferma)
     - Condividere collezione con altri utenti
     - Visualizzare statistiche di utilizzo
   - Sistema applica modifiche e aggiorna vista

**Punti di Controllo**:
- Validazione dei dati della collezione
- Verifica dei limiti di numero collezioni
- Controllo dei permessi di condivisione
- Conferma per operazioni distruttive

### 2. Upload e Gestione Documenti

**Descrizione**: Caricamento e gestione dei documenti nelle collezioni.

**Flusso**:
1. **Accesso Collezione**
   - User seleziona una collezione dalla lista
   - Sistema carica dettagli collezione e lista documenti

2. **Upload Documenti**
   - User seleziona "Upload Documents"
   - Sistema mostra interfaccia di upload con:
     - Drag & drop area
     - Selezione file multipli
     - Indicazione formati supportati
   - User seleziona file da caricare
   - Sistema verifica:
     - Formato file valido
     - Dimensione entro i limiti
     - Quota utente disponibile
   - Sistema inizia upload e mostra progresso

3. **Monitoraggio Elaborazione**
   - Sistema mostra stato elaborazione:
     - Upload completato
     - In coda per elaborazione
     - Elaborazione in corso
     - Elaborazione completata/fallita
   - User può visualizzare dettagli avanzamento
   - Sistema notifica al completamento

4. **Gestione Documenti**
   - User può:
     - Visualizzare dettagli documento
     - Scaricare documento originale
     - Eliminare documento (con conferma)
     - Vedere metadati estratti
   - Sistema applica operazioni e aggiorna vista

**Punti di Controllo**:
- Validazione dei file prima dell'upload
- Verifica delle quote e limiti utente
- Monitoraggio dello stato di elaborazione
- Gestione errori durante l'elaborazione

### 3. Interrogazione Chatbot

**Descrizione**: Interazione con il chatbot per interrogare le collezioni di documenti.

**Flusso**:
1. **Accesso Chatbot**
   - User naviga alla sezione "Chatbot"
   - Sistema carica interfaccia conversazionale
   - Sistema carica lista collezioni disponibili

2. **Selezione Collezioni**
   - User seleziona una o più collezioni da interrogare
   - Sistema verifica permessi di accesso
   - Sistema prepara contesto per la conversazione

3. **Conversazione**
   - User inserisce domanda in linguaggio naturale
   - Sistema invia query a R2R
   - Sistema mostra indicatore di elaborazione
   - Sistema visualizza risposta con:
     - Testo generato
     - Citazioni e fonti
     - Link ai documenti originali
   - User può continuare la conversazione con domande correlate

4. **Gestione Conversazioni**
   - User può:
     - Salvare conversazione con titolo
     - Riprendere conversazioni precedenti
     - Esportare conversazione in formato testo/PDF
     - Condividere conversazione (se permesso)
   - Sistema mantiene storico conversazioni

**Punti di Controllo**:
- Verifica dei permessi sulle collezioni selezionate
- Controllo delle quote di query per ora
- Monitoraggio della qualità delle risposte
- Validazione delle richieste prima dell'invio a R2R

### 4. Gestione Profilo e Preferenze

**Descrizione**: Personalizzazione del profilo utente e delle preferenze di utilizzo.

**Flusso**:
1. **Accesso Profilo**
   - User seleziona il proprio nome/avatar nell'header
   - Sistema mostra menu dropdown
   - User seleziona "Profile Settings"
   - Sistema carica pagina profilo

2. **Modifica Informazioni Personali**
   - User può modificare:
     - Nome e cognome
     - Immagine profilo
     - Informazioni di contatto
   - Sistema valida e salva modifiche

3. **Gestione Password**
   - User seleziona "Change Password"
   - Inserisce password attuale e nuova password
   - Sistema verifica password attuale
   - Sistema aggiorna password e notifica

4. **Preferenze Applicazione**
   - User può personalizzare:
     - Tema (chiaro/scuro)
     - Layout dashboard
     - Notifiche
     - Lingua interfaccia
   - Sistema applica preferenze immediatamente

5. **Visualizzazione Utilizzo**
   - User può vedere:
     - Statistiche di utilizzo
     - Limiti e quote disponibili
     - Storico attività
   - Sistema fornisce visualizzazione chiara dei dati

**Punti di Controllo**:
- Validazione dei dati personali
- Sicurezza nella modifica password
- Persistenza delle preferenze utente
- Privacy dei dati di utilizzo

## Casi d'Uso Critici e Gestione Errori

### 1. Superamento Quote e Limiti

**Scenario**: Un utente raggiunge il limite di documenti o query consentite.

**Gestione**:
1. Sistema rileva il superamento della quota
2. Mostra messaggio chiaro all'utente con:
   - Limite raggiunto
   - Quota attuale
   - Quando la quota si resetterà
   - Opzioni disponibili (es. contattare admin)
3. Disabilita temporaneamente le funzionalità limitate
4. Notifica all'admin Company del superamento
5. Registra l'evento nei log di sistema

### 2. Fallimento Elaborazione Documento

**Scenario**: L'elaborazione di un documento fallisce nel sistema R2R.

**Gestione**:
1. Sistema rileva il fallimento
2. Applica policy di retry con backoff esponenziale
3. Dopo i tentativi massimi:
   - Marca il documento come "Failed"
   - Registra dettaglio errore nei log
   - Notifica all'utente con messaggio specifico
4. Offre opzioni all'utente:
   - Ritentare manualmente
   - Modificare il documento e ricaricare
   - Contattare supporto
5. Fornisce ID errore per troubleshooting

### 3. Problemi di Connessione con R2R

**Scenario**: Il server R2R diventa temporaneamente non disponibile.

**Gestione**:
1. Sistema rileva timeout o errori di connessione
2. Attiva circuit breaker per prevenire sovraccarico
3. Mostra stato sistema nella dashboard
4. Per richieste in corso:
   - Salva in coda persistente
   - Notifica utenti del ritardo
   - Riprova automaticamente quando disponibile
5. Per nuove richieste:
   - Mostra avviso di servizio degradato
   - Offre funzionalità limitate ove possibile
6. Notifica admin di sistema del problema

### 4. Gestione Concorrenza Elevata

**Scenario**: Molti utenti caricano documenti simultaneamente.

**Gestione**:
1. Sistema attiva throttling dinamico
2. Prioritizza richieste in base a:
   - Ruolo utente
   - Criticità operazione
   - Tempo di attesa
3. Mostra tempi stimati di attesa agli utenti
4. Implementa code fair-share tra tenant
5. Scala risorse ove possibile
6. Fornisce feedback in tempo reale sullo stato

## Conclusioni

I workflow dettagliati per ciascun ruolo forniscono una guida completa per l'implementazione delle funzionalità del sistema, garantendo che tutti i percorsi utente siano chiaramente definiti e che i casi d'uso critici siano adeguatamente gestiti. Questa documentazione serve come base per lo sviluppo dell'interfaccia utente e della logica di business, assicurando un'esperienza coerente e robusta per tutti gli utenti del sistema.
