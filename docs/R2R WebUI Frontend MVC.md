# R2R WebUI Frontend MVC

## Panoramica

Il Frontend MVC è l'interfaccia utente (WebUI) del sistema multitenant per SciPhi AI R2R. È sviluppato in Microsoft .NET 9.0 ASP.NET Core MVC e utilizza Bootstrap 5 per un design moderno e responsivo. L'obiettivo principale è fornire un'esperienza utente intuitiva, accattivante e facile da usare, anche per utenti non tecnici, nascondendo la complessità del sistema sottostante.

## Architettura Frontend

L'architettura del frontend Blazor è basata su componenti riutilizzabili, servizi per la logica di business e comunicazione API, e un sistema di state management per mantenere la coerenza dei dati nell'applicazione.

```
┌─────────────────────────────────────────────────────────────────┐
│                        Blazor Components (Pages & UI)           │
│                        (MudBlazor, Material 3, TailwindCSS)     │
└───────────────────────────────┬─────────────────────────────────┘
                                │
┌───────────────────────────────▼─────────────────────────────────┐
│                        Frontend Services                        │
│                        (Auth, API Clients, State Management)    │
└───────────────────────────────┬─────────────────────────────────┘
                                │
┌───────────────────────────────▼─────────────────────────────────┐
│                        Backend API Server                       │
└─────────────────────────────────────────────────────────────────┘
```

### Componenti Chiave

1.  **Pagine (Pages)**: Componenti Blazor che rappresentano le viste principali dell'applicazione (es. Dashboard, Gestione Collezioni, Chatbot).
2.  **Layouts**: Definiscono la struttura comune delle pagine (es. MainLayout con header, sidebar, footer).
3.  **Componenti Condivisi (Shared Components)**: Elementi UI riutilizzabili (es. tabelle dati, modali, form personalizzati).
4.  **Servizi Frontend**: Classi C# che incapsulano la logica di chiamata API, gestione stato, autenticazione, e altre funzionalità cross-cutting.
5.  **Modelli (Models)**: Classi C# che rappresentano le strutture dati utilizzate nel frontend (DTOs, ViewModels).

## Requisiti

### Requisiti di Sviluppo

*   **.NET 9.0 SDK**: Per compilare ed eseguire l'applicazione Blazor.
*   **Node.js e npm/yarn (Opzionale)**: Per la gestione di dipendenze frontend come TailwindCSS, se utilizzato.
*   **IDE Consigliato**: Visual Studio 2022+ o Visual Studio Code con estensioni C# e Blazor.

### Dipendenze Esterne

*   **Backend API Server**: Deve essere in esecuzione e accessibile per il corretto funzionamento del frontend.
*   **Browser Moderni**: Chrome, Firefox, Edge, Safari (ultime versioni).

## Struttura del Progetto (Esempio Blazor Server)

```
R2RWebUI.Frontend/
├── Pages/                     # Componenti Razor che gestiscono le route
│   ├── Admin/
│   ├── Company/
│   ├── User/
│   ├── Auth/
│   │   └── Login.razor
│   ├── _Host.cshtml           # Pagina host per Blazor Server
│   └── _Layout.cshtml         # Layout principale HTML
│
├── Shared/                    # Componenti Razor riutilizzabili e layout
│   ├── MainLayout.razor
│   ├── NavMenu.razor
│   ├── Components/
│   │   └── ConfirmDialog.razor
│   └── LoginDisplay.razor
│
├── Services/                  # Servizi frontend
│   ├── AuthService.cs
│   ├── ApiClient.cs
│   ├── CollectionService.cs
│   ├── StateManagementService.cs
│   └── NotificationService.cs
│
├── Models/                    # Modelli e DTOs frontend
│   ├── UserProfile.cs
│   ├── CollectionViewModel.cs
│   └── LoginRequest.cs
│
├── wwwroot/
│   ├── css/
│   │   ├── app.css            # Stili globali
│   │   └── tailwind.css       # Se si usa TailwindCSS (generato)
│   ├── js/
│   │   └── interop.js         # JavaScript interop
│   └── favicon.ico
│
├── Properties/
│   └── launchSettings.json
│
├── App.razor                  # Componente root dell'applicazione
├── Program.cs                 # Entry point e configurazione servizi
├── appsettings.json           # Configurazione applicazione
└── R2RWebUI.Frontend.csproj   # File di progetto
```

## Configurazione

### `appsettings.json`

```json
{
  "BackendApiUrl": "http://localhost:5001", // URL del Backend API Server
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### `Program.cs` (Configurazione Servizi)

```csharp
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;
using R2RWebUI.Frontend.Services;

var builder = WebApplication.CreateBuilder(args);

// Aggiungi servizi al container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options => { options.DetailedErrors = true; }); // Dettagli errori in sviluppo

// MudBlazor
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    // Altre configurazioni MudBlazor
});

// Servizi di Autenticazione
builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();
builder.Services.AddAuthorizationCore();

// HttpClient per API Backend
builder.Services.AddHttpClient("BackendApiClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["BackendApiUrl"]);
});

// Registrazione servizi custom
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IApiClient, ApiClient>();
builder.Services.AddScoped<ICollectionService, CollectionService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IStateManagementService, StateManagementService>();

var app = builder.Build();

// Configura pipeline HTTP request.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Autenticazione e Autorizzazione
app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
```

## Setup e Avvio

### Prerequisiti

1.  Installare .NET 9.0 SDK.
2.  Assicurarsi che il Backend API Server sia in esecuzione e accessibile all'URL configurato in `appsettings.json`.

### Build e Avvio

1.  Clonare il repository (se applicabile).
2.  Navigare nella directory `R2RWebUI.Frontend`.
3.  Ripristinare le dipendenze:
    ```bash
    dotnet restore
    ```
4.  (Opzionale, se si usa TailwindCSS) Installare dipendenze npm e compilare CSS:
    ```bash
    npm install
    npm run build:css 
    ```
    (Assicurarsi che `package.json` contenga gli script appropriati per TailwindCSS)

5.  Avviare l'applicazione:
    ```bash
    dotnet run
    ```
6.  Aprire il browser all'URL indicato (solitamente `https://localhost:7XXX` o `http://localhost:5XXX`).

## Core Components e Funzionalità

### Layouts

*   **`MainLayout.razor`**: Layout principale che include `NavMenu` (sidebar), header con `LoginDisplay` e notifiche, e l'area di contenuto (`@Body`).
*   **`AdminLayout.razor`, `CompanyLayout.razor`, `UserLayout.razor` (Opzionale)**: Layout specifici per ruoli, se necessario, per personalizzare la navigazione o elementi UI.

### Pagine Principali (Esempi)

*   **`Pages/Auth/Login.razor`**: Pagina di login.
*   **`Pages/Index.razor`**: Dashboard principale, varia in base al ruolo dell'utente.
*   **`Pages/User/Collections.razor`**: Pagina per la gestione delle collezioni utente.
*   **`Pages/User/Chat.razor`**: Interfaccia chatbot.
*   **`Pages/Admin/Companies.razor`**: Pagina per la gestione delle companies (solo Admin).

### Servizi Frontend

*   **`AuthService.cs`**: Gestisce login, logout, refresh token e interazioni con `ApiAuthenticationStateProvider`.
*   **`ApiClient.cs`**: Wrapper generico per le chiamate HTTP al Backend API, gestendo token JWT e errori comuni.
*   **`CollectionService.cs`, `UserService.cs`, etc.**: Servizi specifici per entità, che utilizzano `ApiClient` per recuperare e manipolare dati.
*   **`StateManagementService.cs`**: Gestisce lo stato globale o condiviso dell'applicazione (es. profilo utente, preferenze) per evitare prop drilling e mantenere coerenza.
*   **`NotificationService.cs`**: Gestisce la visualizzazione di notifiche (snackbar, toast) utilizzando MudBlazor.

### Autenticazione e Autorizzazione

*   **`ApiAuthenticationStateProvider.cs`**: Implementazione custom di `AuthenticationStateProvider` che legge il token JWT da localStorage (o sessionStorage) e notifica Blazor dello stato di autenticazione.
*   Utilizzo di `[Authorize]` attribute e `<AuthorizeView>` component per controllare l'accesso a pagine e componenti in base a ruoli e policy definite nel backend.
*   Intercettori HTTP (o Delegating Handlers) per aggiungere automaticamente il token JWT alle richieste API e gestire il refresh del token in caso di scadenza.

## UI/UX e Theming

L'obiettivo è creare un'interfaccia professionale, intuitiva e accattivante, nascondendo la complessità tecnica.

### Principi Guida UI/UX

1.  **Semplicità e Chiarezza**: Evitare interfacce sovraccariche. Ogni elemento deve avere uno scopo chiaro.
2.  **Consistenza**: Utilizzare pattern di design e componenti consistenti in tutta l'applicazione.
3.  **Feedback Immediato**: Fornire feedback visivo per le azioni dell'utente (loading spinners, notifiche di successo/errore).
4.  **Navigazione Intuitiva**: Menu chiari, breadcrumbs, e una gerarchia logica delle informazioni.
5.  **Performance Percepite**: Ottimizzare i tempi di caricamento e la reattività dell'interfaccia.
6.  **Accessibilità (WCAG 2.1 AA)**: Assicurare che l'applicazione sia utilizzabile da persone con diverse abilità.
7.  **Mobile-First/Responsive Design**: L'interfaccia deve adattarsi fluidamente a diverse dimensioni di schermo.
8.  **Estetica Moderna e Accattivante**: Utilizzare un design pulito, con spaziature adeguate, tipografia leggibile e un uso ponderato dei colori.

### Utilizzo di MudBlazor

MudBlazor fornisce una vasta gamma di componenti Material Design pronti all'uso. Sfruttare al massimo:

*   **Layout Components**: `MudLayout`, `MudAppBar`, `MudDrawer`, `MudMainContent`.
*   **Navigation**: `MudNavMenu`, `MudNavLink`, `MudTabs`, `MudBreadcrumbs`.
*   **Data Display**: `MudTable` (con paginazione, sorting, filtering), `MudList`, `MudCard`.
*   **Forms & Inputs**: `MudForm`, `MudTextField`, `MudSelect`, `MudCheckBox`, `MudDatePicker`, `MudFileUpload`.
*   **Feedback**: `MudProgressCircular`, `MudProgressLinear`, `MudSnackbar`, `MudAlert`.
*   **Dialogs**: `MudDialog` per conferme e input modali.

### Personalizzazione e Theming

1.  **MudBlazor Theming**: MudBlazor offre un potente sistema di theming.
    *   Creare un tema personalizzato in `Program.cs` o un file separato:
        ```csharp
        // Program.cs
        builder.Services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
            config.MudTheme = new MudTheme()
            {
                Palette = new PaletteLight()
                {
                    Primary = Colors.Blue.Default, // Colore primario
                    Secondary = Colors.Green.Accent4,
                    AppbarBackground = Colors.Blue.Default,
                },
                PaletteDark = new PaletteDark()
                {
                    Primary = Colors.Blue.Lighten1,
                },
                LayoutProperties = new LayoutProperties()
                {
                    DefaultBorderRadius = "6px"
                }
            };
        });
        ```
    *   Utilizzare le variabili CSS di MudBlazor per sovrascrizioni più granulari.

2.  **CSS Personalizzato (`app.css`)**:
    *   Aggiungere stili globali o sovrascrivere stili di MudBlazor quando necessario.
    *   Utilizzare convenzioni BEM o simili per organizzare il CSS.

3.  **Integrazione con Material 3 (M3)**:
    *   MudBlazor è basato su Material Design. Per allinearsi maggiormente a M3, si possono:
        *   Adattare la `Palette` di MudBlazor ai colori e ai token di M3.
        *   Personalizzare `LayoutProperties` (es. `DefaultBorderRadius`) per riflettere lo stile M3.
        *   Utilizzare le linee guida di M3 per tipografia, spaziature e iconografia.
        *   Se MudBlazor non supporta nativamente un componente M3, considerare la creazione di componenti Blazor customizzati che ne imitino l'aspetto e il comportamento, oppure attendere aggiornamenti futuri di MudBlazor.

4.  **Integrazione con TailwindCSS (Opzionale)**:
    *   TailwindCSS può essere usato in combinazione con MudBlazor per utility classes o per stilizzare componenti custom.
    *   **Setup**: 
        1.  Installare TailwindCSS via npm: `npm install -D tailwindcss postcss autoprefixer`
        2.  Creare `tailwind.config.js` e `postcss.config.js`.
        3.  Configurare `tailwind.config.js` per scansionare i file `.razor` e `.html`:
            ```javascript
            module.exports = {
              content: ["./**/*.{razor,html}"],
              theme: {
                extend: {},
              },
              plugins: [],
            }
            ```
        4.  Creare un file CSS di input (es. `Styles/tailwind_input.css`):
            ```css
            @tailwind base;
            @tailwind components;
            @tailwind utilities;
            ```
        5.  Aggiungere uno script a `package.json` per compilare TailwindCSS:
            ```json
            "scripts": {
              "build:css": "tailwindcss -i ./Styles/tailwind_input.css -o ./wwwroot/css/tailwind.css --watch"
            }
            ```
        6.  Includere il CSS generato (`tailwind.css`) in `_Host.cshtml` o `_Layout.cshtml`.
    *   **Utilizzo**: Applicare classi Tailwind direttamente negli elementi HTML dei componenti Razor.
    *   **Attenzione**: Evitare conflitti di stile tra MudBlazor e TailwindCSS. Usare Tailwind principalmente per layout, spaziature, o componenti custom non coperti da MudBlazor. Potrebbe essere necessario configurare un prefisso per le classi Tailwind per evitare collisioni.

## State Management

Per applicazioni Blazor complesse, una strategia di state management è cruciale.

*   **Servizi Scoped/Singleton**: Per stati semplici o condivisi localmente.
*   **Cascading Parameters**: Per passare dati attraverso la gerarchia dei componenti.
*   **Librerie di State Management (Opzionale)**: Per scenari più complessi, considerare librerie come:
    *   **Fluxor**: Implementazione di Flux per Blazor.
    *   **Blazor-State**: MediatR-based state management.
    *   Approccio custom basato su `ObservableCollection` e `INotifyPropertyChanged` in servizi singleton/scoped, con eventi per notificare i componenti degli aggiornamenti.

L'approccio scelto dovrebbe bilanciare semplicità e potenza in base alle necessità dell'applicazione.

## Comunicazione API

*   Utilizzare `HttpClient` (configurato via `IHttpClientFactory`) per le chiamate al Backend API.
*   Creare servizi client tipizzati per incapsulare la logica di chiamata API (es. `CollectionService.cs`).
*   Gestire centralmente l'aggiunta del token JWT alle richieste e la gestione degli errori (es. 401 Unauthorized per refresh token, 403 Forbidden).
*   Utilizzare DTO (Data Transfer Objects) per definire i contratti tra frontend e backend.

## Testing

*   **Test Unitari**: Utilizzare bUnit per testare la logica dei componenti Blazor in isolamento. Mockare i servizi dipendenti.
*   **Test di Integrazione**: Testare l'interazione tra componenti e servizi (es. chiamate API mockate).
*   **Test End-to-End (E2E)**: Utilizzare strumenti come Playwright o Selenium per testare i flussi utente completi attraverso l'interfaccia.

## Build e Deployment

### Build per Produzione

```bash
dotnet publish -c Release -o ./publish
```

### Opzioni di Deployment

1.  **Blazor Server**:
    *   Hostare su IIS, Azure App Service, Linux con Kestrel/NGINX.
    *   Richiede una connessione SignalR persistente.
2.  **Blazor WebAssembly (WASM)**:
    *   Hostare come file statici su qualsiasi web server (NGINX, Apache, Azure Static Web Apps, GitHub Pages).
    *   L'applicazione viene eseguita interamente nel browser del client.
    *   Considerare AOT (Ahead-of-Time) compilation per migliorare le performance di runtime (aumenta la dimensione del download iniziale).

La scelta tra Blazor Server e WASM dipende dai requisiti specifici di performance, interattività, deployment e infrastruttura.

## Troubleshooting

*   **Errori di Connessione API**: Verificare URL backend, CORS, e log del browser/server.
*   **Problemi di Autenticazione**: Controllare gestione token JWT, `AuthenticationStateProvider`, e log.
*   **Errori Componenti MudBlazor**: Consultare la documentazione di MudBlazor, verificare versioni e configurazioni.
*   **Problemi di Stato**: Utilizzare Blazor DevTools o logging per tracciare cambiamenti di stato e re-render dei componenti.
*   **Errori JavaScript Interop**: Assicurarsi che i file JS siano caricati correttamente e che le chiamate interop siano corrette.

## Riferimenti

*   [Documentazione Architettura Sistema](../architettura_sistema.md)
*   [README Backend API Server](../README_backend_api_server.md)
*   [Workflow Dettagliati per Ruoli](../workflow_ruoli_dettagliati.md)
*   [Guida Template Grafico](../guida_template_grafico.md) (da creare)
*   [Documentazione Ufficiale Blazor](https://docs.microsoft.com/aspnetcore/blazor/)
*   [Documentazione MudBlazor](https://mudblazor.com/)
*   [Linee Guida Material Design 3](https://m3.material.io/)
*   [Documentazione TailwindCSS](https://tailwindcss.com/docs)

## Contribuzione

Seguire le linee guida di sviluppo definite nel progetto (branching strategy, code style, pull requests).

## Licenza

Copyright (c) 2025 R2R WebUI Team. Tutti i diritti riservati.
