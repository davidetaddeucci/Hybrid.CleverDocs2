# R2R WebUI Backend API Server

## Panoramica

Il Backend API Server è il componente centrale dell'architettura WebUI multitenant per SciPhi AI R2R, responsabile della gestione delle richieste client, dell'orchestrazione dei servizi, dell'integrazione con R2R API e dell'implementazione della logica di business. Questo componente è sviluppato in Microsoft .NET 9.0 e fornisce un'API RESTful per il frontend Blazor.

## Architettura

Il Backend API Server segue un'architettura a layer con separazione delle responsabilità:

```
┌─────────────────────────────────────────────────────────────────┐
│                        API Controllers                          │
└───────────────────────────────┬─────────────────────────────────┘
                                │
┌───────────────────────────────▼─────────────────────────────────┐
│                        Application Services                     │
└───────────────────────────────┬─────────────────────────────────┘
                                │
┌─────────────┬─────────────────▼─────────────────┬───────────────┐
│  Domain     │                                   │    External   │
│  Services   │       Domain Model                │    Services   │
│             │                                   │               │
└─────────────┴─────────────────┬─────────────────┴───────────────┘
                                │
┌───────────────────────────────▼─────────────────────────────────┐
│                        Infrastructure                           │
└─────────────────────────────────────────────────────────────────┘
```

### Componenti Principali

1. **API Controllers**: Endpoint REST che gestiscono le richieste HTTP
2. **Application Services**: Orchestrazione dei flussi di business e transazioni
3. **Domain Services**: Logica di business core e validazione
4. **Domain Model**: Entità, value objects e regole di business
5. **External Services**: Integrazione con servizi esterni (R2R API)
6. **Infrastructure**: Implementazioni concrete di repository, cache, code, ecc.

## Requisiti

### Requisiti di Sistema

- **Sistema Operativo**: Windows Server 2022 o Linux (Ubuntu 22.04+)
- **Runtime**: .NET 9.0 SDK e Runtime
- **Database**: SQL Server 2022 (o versione compatibile)
- **Cache**: Redis 7.0+ (per caching avanzato)
- **Storage**: Almeno 100GB di spazio disco per logs e file temporanei

### Dipendenze Esterne

- **R2R API Server**: Disponibile su http://192.168.1.4:7272
- **SQL Server**: Disponibile su http://192.168.1.4:1433
- **Redis**: Disponibile su 192.168.1.4:6379

## Struttura del Progetto

```
R2RWebUI.Backend/
├── src/
│   ├── R2RWebUI.API/                # Progetto API principale
│   │   ├── Controllers/             # Controller API
│   │   ├── Middleware/              # Middleware personalizzati
│   │   ├── Filters/                 # Filtri API
│   │   ├── Program.cs               # Entry point
│   │   ├── Startup.cs               # Configurazione applicazione
│   │   └── appsettings.json         # Configurazioni
│   │
│   ├── R2RWebUI.Application/        # Logica applicativa
│   │   ├── Commands/                # Command handlers (CQRS)
│   │   ├── Queries/                 # Query handlers (CQRS)
│   │   ├── DTOs/                    # Data Transfer Objects
│   │   ├── Mappings/                # Mappings tra entità e DTOs
│   │   └── Services/                # Servizi applicativi
│   │
│   ├── R2RWebUI.Domain/             # Logica di dominio
│   │   ├── Entities/                # Entità di dominio
│   │   ├── ValueObjects/            # Value objects
│   │   ├── Interfaces/              # Interfacce di dominio
│   │   ├── Services/                # Servizi di dominio
│   │   └── Exceptions/              # Eccezioni di dominio
│   │
│   ├── R2RWebUI.Infrastructure/     # Implementazioni infrastrutturali
│   │   ├── Data/                    # Accesso dati
│   │   │   ├── Context/             # DbContext e configurazioni
│   │   │   ├── Repositories/        # Implementazioni repository
│   │   │   └── Migrations/          # Migrazioni database
│   │   │
│   │   ├── ExternalServices/        # Servizi esterni
│   │   │   ├── R2R/                 # Client R2R API
│   │   │   └── Email/               # Servizi email
│   │   │
│   │   ├── Caching/                 # Implementazioni cache
│   │   ├── Queuing/                 # Sistema di code
│   │   ├── Identity/                # Autenticazione e autorizzazione
│   │   └── Logging/                 # Configurazione logging
│   │
│   └── R2RWebUI.Shared/             # Componenti condivisi
│       ├── Constants/               # Costanti applicative
│       ├── Extensions/              # Extension methods
│       └── Utilities/               # Utility classes
│
├── tests/                           # Test unitari e di integrazione
│   ├── R2RWebUI.API.Tests/
│   ├── R2RWebUI.Application.Tests/
│   ├── R2RWebUI.Domain.Tests/
│   └── R2RWebUI.Infrastructure.Tests/
│
└── tools/                           # Script e strumenti di supporto
    ├── Database/                    # Script database
    └── Deployment/                  # Script deployment
```

## Configurazione

### File di Configurazione

Il file principale di configurazione è `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=192.168.1.4,1433;Database=R2RWebUI;User Id=r2r_app;Password=StrongPassword123;TrustServerCertificate=True;",
    "Redis": "192.168.1.4:6379,abortConnect=false,connectTimeout=30000,password=StrongRedisPassword123"
  },
  "R2RApi": {
    "BaseUrl": "http://192.168.1.4:7272",
    "Timeout": 30,
    "MaxRetries": 3,
    "CircuitBreakerThreshold": 5,
    "CircuitBreakerDurationMinutes": 1
  },
  "Authentication": {
    "JwtSecret": "YourSuperSecretKeyWithAtLeast32Characters",
    "JwtIssuer": "r2r-webui",
    "JwtAudience": "r2r-webui-clients",
    "TokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "Queuing": {
    "WorkerCount": 5,
    "BatchSize": 10,
    "PollingIntervalSeconds": 5,
    "MaxRetries": 3,
    "RetryDelaySeconds": 30
  },
  "Caching": {
    "DefaultExpirationMinutes": 60,
    "EntityCacheMinutes": 30,
    "CollectionCacheMinutes": 15,
    "QueryCacheMinutes": 10,
    "R2RDocumentCacheMinutes": 60,
    "R2RSearchCacheMinutes": 5
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/r2r-webui-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 31
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": ["https://r2r-webui.example.com", "http://localhost:5000"]
  }
}
```

### Variabili d'Ambiente

Le configurazioni sensibili possono essere sovrascritte tramite variabili d'ambiente:

```bash
# Connessioni Database
export ConnectionStrings__DefaultConnection="Server=192.168.1.4,1433;Database=R2RWebUI;User Id=r2r_app;Password=StrongPassword123;TrustServerCertificate=True;"
export ConnectionStrings__Redis="192.168.1.4:6379,abortConnect=false,connectTimeout=30000,password=StrongRedisPassword123"

# Configurazione R2R API
export R2RApi__BaseUrl="http://192.168.1.4:7272"

# Sicurezza
export Authentication__JwtSecret="YourSuperSecretKeyWithAtLeast32Characters"
```

### Configurazione per Ambiente

Per ambienti diversi (Development, Staging, Production), utilizzare file di configurazione specifici:

- `appsettings.Development.json`
- `appsettings.Staging.json`
- `appsettings.Production.json`

## Setup e Deployment

### Prerequisiti

1. Installare .NET 9.0 SDK:
   ```bash
   wget https://dotnet.microsoft.com/download/dotnet/9.0 -O dotnet-install.sh
   chmod +x dotnet-install.sh
   ./dotnet-install.sh --channel 9.0
   ```

2. Verificare l'installazione:
   ```bash
   dotnet --version
   ```

### Build

1. Clonare il repository:
   ```bash
   git clone https://your-repository-url/R2RWebUI.git
   cd R2RWebUI
   ```

2. Ripristinare le dipendenze:
   ```bash
   dotnet restore
   ```

3. Compilare il progetto:
   ```bash
   dotnet build --configuration Release
   ```

### Database Setup

1. Applicare le migrazioni:
   ```bash
   cd src/R2RWebUI.API
   dotnet ef database update
   ```

2. Seed dei dati iniziali:
   ```bash
   dotnet run -- --seed
   ```

### Esecuzione

1. Esecuzione locale:
   ```bash
   cd src/R2RWebUI.API
   dotnet run
   ```

2. Specificare l'ambiente:
   ```bash
   dotnet run --environment Production
   ```

### Deployment

#### Deployment su IIS (Windows)

1. Pubblicare l'applicazione:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. Configurare IIS:
   - Creare un nuovo sito web in IIS
   - Impostare il percorso fisico alla cartella `publish`
   - Configurare il pool di applicazioni per .NET CLR versione "No Managed Code"
   - Installare il modulo ASP.NET Core

#### Deployment su Linux con Systemd

1. Pubblicare l'applicazione:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. Creare un file di servizio systemd:
   ```bash
   sudo nano /etc/systemd/system/r2rwebui.service
   ```

3. Configurare il servizio:
   ```ini
   [Unit]
   Description=R2R WebUI Backend API

   [Service]
   WorkingDirectory=/path/to/publish
   ExecStart=/usr/bin/dotnet /path/to/publish/R2RWebUI.API.dll
   Restart=always
   RestartSec=10
   SyslogIdentifier=r2rwebui
   User=www-data
   Environment=ASPNETCORE_ENVIRONMENT=Production
   Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

   [Install]
   WantedBy=multi-user.target
   ```

4. Abilitare e avviare il servizio:
   ```bash
   sudo systemctl enable r2rwebui
   sudo systemctl start r2rwebui
   ```

#### Deployment con Docker

1. Creare un Dockerfile:
   ```dockerfile
   FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
   WORKDIR /app
   EXPOSE 80
   EXPOSE 443

   FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
   WORKDIR /src
   COPY ["src/R2RWebUI.API/R2RWebUI.API.csproj", "R2RWebUI.API/"]
   COPY ["src/R2RWebUI.Application/R2RWebUI.Application.csproj", "R2RWebUI.Application/"]
   COPY ["src/R2RWebUI.Domain/R2RWebUI.Domain.csproj", "R2RWebUI.Domain/"]
   COPY ["src/R2RWebUI.Infrastructure/R2RWebUI.Infrastructure.csproj", "R2RWebUI.Infrastructure/"]
   COPY ["src/R2RWebUI.Shared/R2RWebUI.Shared.csproj", "R2RWebUI.Shared/"]
   RUN dotnet restore "R2RWebUI.API/R2RWebUI.API.csproj"
   COPY . .
   WORKDIR "/src/src/R2RWebUI.API"
   RUN dotnet build "R2RWebUI.API.csproj" -c Release -o /app/build

   FROM build AS publish
   RUN dotnet publish "R2RWebUI.API.csproj" -c Release -o /app/publish

   FROM base AS final
   WORKDIR /app
   COPY --from=publish /app/publish .
   ENTRYPOINT ["dotnet", "R2RWebUI.API.dll"]
   ```

2. Costruire l'immagine Docker:
   ```bash
   docker build -t r2rwebui-api .
   ```

3. Eseguire il container:
   ```bash
   docker run -d -p 8080:80 --name r2rwebui-api r2rwebui-api
   ```

## API Endpoints

### Autenticazione

| Endpoint | Metodo | Descrizione | Ruoli |
|----------|--------|-------------|-------|
| `/api/auth/login` | POST | Autenticazione utente | Tutti |
| `/api/auth/refresh` | POST | Refresh token | Tutti |
| `/api/auth/logout` | POST | Logout utente | Tutti |

### Companies

| Endpoint | Metodo | Descrizione | Ruoli |
|----------|--------|-------------|-------|
| `/api/companies` | GET | Lista companies | Admin |
| `/api/companies/{id}` | GET | Dettaglio company | Admin, Company |
| `/api/companies` | POST | Crea company | Admin |
| `/api/companies/{id}` | PUT | Aggiorna company | Admin |
| `/api/companies/{id}` | DELETE | Disattiva company | Admin |

### Utenti

| Endpoint | Metodo | Descrizione | Ruoli |
|----------|--------|-------------|-------|
| `/api/users` | GET | Lista utenti | Admin, Company |
| `/api/users/{id}` | GET | Dettaglio utente | Admin, Company, User (solo se stesso) |
| `/api/users` | POST | Crea utente | Admin, Company |
| `/api/users/{id}` | PUT | Aggiorna utente | Admin, Company, User (solo se stesso) |
| `/api/users/{id}` | DELETE | Disattiva utente | Admin, Company |

### Collezioni

| Endpoint | Metodo | Descrizione | Ruoli |
|----------|--------|-------------|-------|
| `/api/collections` | GET | Lista collezioni | Tutti (filtrate per ruolo) |
| `/api/collections/{id}` | GET | Dettaglio collezione | Tutti (con permessi) |
| `/api/collections` | POST | Crea collezione | User, Company, Admin |
| `/api/collections/{id}` | PUT | Aggiorna collezione | User (proprietario), Company, Admin |
| `/api/collections/{id}` | DELETE | Elimina collezione | User (proprietario), Company, Admin |

### Documenti

| Endpoint | Metodo | Descrizione | Ruoli |
|----------|--------|-------------|-------|
| `/api/collections/{collectionId}/documents` | GET | Lista documenti | Tutti (con permessi) |
| `/api/documents/{id}` | GET | Dettaglio documento | Tutti (con permessi) |
| `/api/collections/{collectionId}/documents` | POST | Upload documento | User, Company, Admin |
| `/api/documents/{id}` | DELETE | Elimina documento | User (proprietario), Company, Admin |

### Chatbot

| Endpoint | Metodo | Descrizione | Ruoli |
|----------|--------|-------------|-------|
| `/api/chat/conversations` | GET | Lista conversazioni | Tutti |
| `/api/chat/conversations/{id}` | GET | Dettaglio conversazione | Tutti (proprietario) |
| `/api/chat/conversations` | POST | Crea conversazione | Tutti |
| `/api/chat/conversations/{id}/messages` | POST | Invia messaggio | Tutti (proprietario) |

### Monitoraggio

| Endpoint | Metodo | Descrizione | Ruoli |
|----------|--------|-------------|-------|
| `/api/monitoring/stats` | GET | Statistiche sistema | Admin, Company |
| `/api/monitoring/logs` | GET | Log di sistema | Admin |
| `/api/monitoring/queues` | GET | Stato code | Admin |
| `/api/health` | GET | Health check | Pubblico |

## Sicurezza

### Autenticazione e Autorizzazione

Il sistema utilizza JWT (JSON Web Tokens) per l'autenticazione e un sistema RBAC (Role-Based Access Control) per l'autorizzazione.

#### Flusso di Autenticazione

1. L'utente invia credenziali a `/api/auth/login`
2. Il server verifica le credenziali e genera un JWT + refresh token
3. Il client include il JWT in tutte le richieste successive nell'header `Authorization`
4. Quando il JWT scade, il client può ottenere un nuovo token usando il refresh token

#### Implementazione

```csharp
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // JWT Authentication
    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Configuration["Authentication:JwtIssuer"],
            ValidAudience = Configuration["Authentication:JwtAudience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(Configuration["Authentication:JwtSecret"]))
        };
    });

    // Authorization Policies
    services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAdminRole", policy => 
            policy.RequireRole("Admin"));
            
        options.AddPolicy("RequireCompanyRole", policy => 
            policy.RequireRole("Admin", "Company"));
            
        options.AddPolicy("RequireUserRole", policy => 
            policy.RequireRole("Admin", "Company", "User"));
            
        options.AddPolicy("CanManageCompanies", policy => 
            policy.RequireRole("Admin"));
            
        options.AddPolicy("CanManageUsers", policy => 
            policy.RequireRole("Admin", "Company"));
    });
}
```

### Protezione API

1. **Rate Limiting**: Limitazione delle richieste per prevenire abusi
2. **CORS**: Configurazione Cross-Origin Resource Sharing
3. **Input Validation**: Validazione di tutti gli input client
4. **Output Encoding**: Encoding appropriato di tutti gli output

```csharp
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Rate Limiting
    services.AddRateLimiting(Configuration);
    
    // CORS
    services.AddCors(options =>
    {
        options.AddPolicy("DefaultPolicy", builder =>
        {
            builder.WithOrigins(Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>())
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials();
        });
    });
    
    // API Security
    services.AddControllers(options =>
    {
        options.Filters.Add(new ValidateModelAttribute());
    });
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // Security Headers
    app.UseSecurityHeaders(policies =>
        policies
            .AddDefaultSecurityHeaders()
            .AddStrictTransportSecurityMaxAgeIncludeSubDomains(maxAgeInSeconds: 60 * 60 * 24 * 365) // 1 year
            .AddXssProtectionBlock()
            .AddContentTypeOptionsNoSniff()
            .AddReferrerPolicyStrictOriginWhenCrossOrigin()
            .RemoveServerHeader()
    );
    
    app.UseRateLimiting();
    app.UseCors("DefaultPolicy");
}
```

## Gestione Errori

### Middleware di Gestione Eccezioni

```csharp
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = new ErrorResponse();
        
        switch (exception)
        {
            case ValidationException validationEx:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = "Validation error";
                response.Errors = validationEx.Errors;
                break;
                
            case NotFoundException notFoundEx:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                response.Message = notFoundEx.Message;
                break;
                
            case UnauthorizedAccessException:
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                response.Message = "Forbidden";
                break;
                
            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                response.Message = "An error occurred. Please try again later.";
                response.TraceId = Activity.Current?.Id ?? context.TraceIdentifier;
                break;
        }
        
        await context.Response.WriteAsJsonAsync(response);
    }
}
```

### Logging

Il sistema utilizza Serilog per il logging strutturato:

```csharp
// Program.cs
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/r2r-webui-.log", rollingInterval: RollingInterval.Day))
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        });
```

## Monitoraggio e Diagnostica

### Health Checks

```csharp
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddHealthChecks()
        .AddSqlServer(Configuration.GetConnectionString("DefaultConnection"))
        .AddRedis(Configuration.GetConnectionString("Redis"))
        .AddUrlGroup(new Uri($"{Configuration["R2RApi:BaseUrl"]}/v3/health"), name: "r2r-api")
        .AddCheck<QueueHealthCheck>("queue-health");
        
    services.AddHealthChecksUI()
        .AddInMemoryStorage();
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
        
        endpoints.MapHealthChecksUI();
    });
}
```

### Metriche

```csharp
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddMetrics();
    
    services.AddMetricsTrackingMiddleware();
    
    services.AddMetricsReportingHostedService();
    
    services.AddMetricsEndpoints();
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseMetricsAllMiddleware();
    
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapMetrics();
    });
}
```

## Integrazione con R2R API

### Client R2R

```csharp
public class R2RClient : IR2RClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<R2RClient> _logger;
    private readonly CircuitBreakerPolicy _circuitBreaker;
    private readonly RetryPolicy _retryPolicy;
    
    public R2RClient(HttpClient httpClient, IOptions<R2ROptions> options, ILogger<R2RClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Configura l'HttpClient
        _httpClient.BaseAddress = new Uri(options.Value.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(options.Value.Timeout);
        
        // Configura le policy di resilienza
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                options.Value.MaxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception, "Retry {RetryCount} after {TimeSpan}s delay due to: {Message}",
                        retryCount, timeSpan.TotalSeconds, exception.Message);
                });
                
        _circuitBreaker = Policy
            .Handle<HttpRequestException>()
            .CircuitBreakerAsync(
                options.Value.CircuitBreakerThreshold,
                TimeSpan.FromMinutes(options.Value.CircuitBreakerDurationMinutes),
                onBreak: (ex, breakDelay) =>
                {
                    _logger.LogError(ex, "Circuit breaker opened for {BreakDelay}m", breakDelay.TotalMinutes);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker reset");
                });
    }
    
    public async Task<DocumentResponse> CreateDocumentAsync(DocumentRequest request, string apiKey, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithResilienceAsync<DocumentResponse>(
            async () =>
            {
                var content = new MultipartFormDataContent();
                // Aggiungi file e metadati al content
                
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                var response = await _httpClient.PostAsync("/v3/documents", content, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                return await response.Content.ReadFromJsonAsync<DocumentResponse>();
            });
    }
    
    // Altri metodi per le diverse API di R2R
    
    private async Task<T> ExecuteWithResilienceAsync<T>(Func<Task<T>> action)
    {
        return await _retryPolicy.WrapAsync(_circuitBreaker).ExecuteAsync(action);
    }
}
```

### Registrazione del Client

```csharp
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.Configure<R2ROptions>(Configuration.GetSection("R2RApi"));
    
    services.AddHttpClient<IR2RClient, R2RClient>()
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = 
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
}
```

## Sistema di Code

### Implementazione Queue Manager

```csharp
public class QueueManager : IQueueManager
{
    private readonly IQueueStorage _queueStorage;
    private readonly ILogger<QueueManager> _logger;
    private readonly IOptions<QueueOptions> _options;
    private readonly ITenantService _tenantService;
    private readonly IThrottlingService _throttlingService;
    
    public QueueManager(
        IQueueStorage queueStorage,
        ILogger<QueueManager> logger,
        IOptions<QueueOptions> options,
        ITenantService tenantService,
        IThrottlingService throttlingService)
    {
        _queueStorage = queueStorage;
        _logger = logger;
        _options = options;
        _tenantService = tenantService;
        _throttlingService = throttlingService;
    }
    
    public async Task<string> EnqueueDocumentUploadAsync(DocumentUploadRequest request, int userId, int companyId)
    {
        // Verifica limiti di throttling
        if (!await _throttlingService.CheckThrottlingLimitAsync(companyId, ThrottlingType.DocumentUpload))
        {
            throw new ThrottlingException("Limite di upload documenti raggiunto");
        }
        
        // Crea l'elemento in coda
        var queueItem = new QueueItem
        {
            QueueName = QueueNames.DocumentUpload,
            ItemType = typeof(DocumentUploadRequest).Name,
            ItemData = JsonSerializer.Serialize(request),
            Status = QueueItemStatus.Pending,
            Priority = await CalculatePriorityAsync(companyId, QueueNames.DocumentUpload),
            CompanyId = companyId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        
        // Salva in coda
        var queueItemId = await _queueStorage.SaveQueueItemAsync(queueItem);
        
        _logger.LogInformation("Document upload request enqueued with ID {QueueItemId} for company {CompanyId}", queueItemId, companyId);
        
        return queueItemId;
    }
    
    // Altri metodi per gestire diverse tipologie di elementi in coda
}
```

### Implementazione Worker Service

```csharp
public class DocumentProcessingWorker : BackgroundService
{
    private readonly IQueueStorage _queueStorage;
    private readonly IR2RClient _r2rClient;
    private readonly ILogger<DocumentProcessingWorker> _logger;
    private readonly IOptions<WorkerOptions> _options;
    private readonly IServiceScopeFactory _scopeFactory;
    
    public DocumentProcessingWorker(
        IQueueStorage queueStorage,
        IR2RClient r2rClient,
        ILogger<DocumentProcessingWorker> logger,
        IOptions<WorkerOptions> options,
        IServiceScopeFactory scopeFactory)
    {
        _queueStorage = queueStorage;
        _r2rClient = r2rClient;
        _logger = logger;
        _options = options;
        _scopeFactory = scopeFactory;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Document Processing Worker starting");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Ottieni elementi in coda da processare
                var queueItems = await _queueStorage.GetPendingItemsAsync(
                    QueueNames.DocumentProcessing,
                    _options.Value.BatchSize,
                    stoppingToken);
                
                if (queueItems.Any())
                {
                    _logger.LogInformation("Processing {Count} document processing requests", queueItems.Count);
                    
                    // Processa in parallelo con limite di concorrenza
                    await Parallel.ForEachAsync(
                        queueItems,
                        new ParallelOptions
                        {
                            MaxDegreeOfParallelism = _options.Value.MaxConcurrency,
                            CancellationToken = stoppingToken
                        },
                        async (item, ct) => await ProcessQueueItemAsync(item, ct));
                }
                else
                {
                    // Nessun elemento da processare, attendi
                    await Task.Delay(_options.Value.PollingInterval, stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in document processing worker");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
    
    private async Task ProcessQueueItemAsync(QueueItem item, CancellationToken cancellationToken)
    {
        // Implementazione elaborazione elemento in coda
    }
}
```

### Registrazione Worker Services

```csharp
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.Configure<WorkerOptions>(Configuration.GetSection("Queuing"));
    
    services.AddHostedService<DocumentUploadWorker>();
    services.AddHostedService<DocumentProcessingWorker>();
    services.AddHostedService<EntityExtractionWorker>();
}
```

## Caching con Redis

### Implementazione Cache Service

```csharp
public class ResponseCacheService : IResponseCacheService
{
    private readonly IDistributedCache _cache;
    private readonly IOptions<CacheOptions> _options;
    
    public ResponseCacheService(IDistributedCache cache, IOptions<CacheOptions> options)
    {
        _cache = cache;
        _options = options;
    }
    
    public async Task<T> CacheResponseAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan? expiry = null)
    {
        var cachedResponse = await _cache.GetStringAsync(cacheKey);
        
        if (cachedResponse != null)
        {
            return JsonSerializer.Deserialize<T>(cachedResponse);
        }
        
        var response = await factory();
        
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(_options.Value.DefaultExpirationMinutes)
        };
        
        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(response),
            options);
            
        return response;
    }
    
    public async Task RemoveAsync(string cacheKey)
    {
        await _cache.RemoveAsync(cacheKey);
    }
    
    public async Task RemoveByPrefixAsync(string prefix)
    {
        // Implementazione per rimuovere tutte le chiavi con un determinato prefisso
    }
}
```

### Registrazione Cache Services

```csharp
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.Configure<CacheOptions>(Configuration.GetSection("Caching"));
    
    services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = Configuration.GetConnectionString("Redis");
        options.InstanceName = "R2RWebUI_";
    });
    
    services.AddSingleton<IResponseCacheService, ResponseCacheService>();
    services.AddSingleton<IApplicationCacheService, ApplicationCacheService>();
}
```

## Multitenancy

### Implementazione Tenant Resolver

```csharp
public class TenantResolver : ITenantResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantRepository _tenantRepository;
    
    public TenantResolver(IHttpContextAccessor httpContextAccessor, ITenantRepository tenantRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantRepository = tenantRepository;
    }
    
    public async Task<TenantContext> ResolveTenantAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        if (httpContext == null)
        {
            return null;
        }
        
        // Ottieni tenant ID da claim utente
        var tenantIdClaim = httpContext.User.FindFirst("TenantId");
        
        if (tenantIdClaim == null || !int.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            return null;
        }
        
        // Carica tenant dal repository
        var tenant = await _tenantRepository.GetByIdAsync(tenantId);
        
        if (tenant == null || !tenant.IsActive)
        {
            return null;
        }
        
        return new TenantContext
        {
            Id = tenant.Id,
            Name = tenant.Name,
            ConnectionString = tenant.ConnectionString,
            Settings = tenant.Settings
        };
    }
}
```

### Middleware Tenant

```csharp
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    
    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context, ITenantResolver tenantResolver, ITenantContextAccessor tenantContextAccessor)
    {
        var tenant = await tenantResolver.ResolveTenantAsync();
        
        if (tenant != null)
        {
            tenantContextAccessor.SetCurrentTenant(tenant);
        }
        
        await _next(context);
    }
}
```

### Registrazione Multitenancy

```csharp
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddScoped<ITenantResolver, TenantResolver>();
    services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
    
    services.AddDbContext<AppDbContext>((serviceProvider, options) =>
    {
        var tenantContextAccessor = serviceProvider.GetRequiredService<ITenantContextAccessor>();
        var tenant = tenantContextAccessor.GetCurrentTenant();
        
        var connectionString = tenant?.ConnectionString ?? Configuration.GetConnectionString("DefaultConnection");
        
        options.UseSqlServer(connectionString);
    });
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseMiddleware<TenantMiddleware>();
}
```

## Troubleshooting

### Problemi Comuni e Soluzioni

#### 1. Errori di Connessione al Database

**Sintomi**:
- Errori "Connection refused" o timeout
- Eccezioni SqlException

**Soluzioni**:
- Verificare che SQL Server sia in esecuzione
- Controllare la stringa di connessione
- Verificare firewall e regole di rete
- Controllare credenziali e permessi utente

#### 2. Errori di Connessione a Redis

**Sintomi**:
- Errori "Connection refused" o timeout
- Eccezioni RedisConnectionException

**Soluzioni**:
- Verificare che Redis sia in esecuzione
- Controllare la stringa di connessione
- Verificare firewall e regole di rete
- Controllare password Redis

#### 3. Errori di Connessione a R2R API

**Sintomi**:
- Errori HTTP 5xx o timeout
- Circuit breaker aperto

**Soluzioni**:
- Verificare che R2R API sia in esecuzione
- Controllare URL base configurato
- Verificare API key e autorizzazioni
- Controllare log R2R per errori specifici

#### 4. Problemi di Performance

**Sintomi**:
- Latenza elevata nelle risposte API
- Utilizzo CPU/memoria elevato
- Timeout nelle richieste

**Soluzioni**:
- Verificare indici database
- Controllare query lente nei log
- Verificare configurazione cache
- Aumentare worker per code
- Scalare orizzontalmente le istanze API

### Logging e Diagnostica

#### Visualizzazione Log

```bash
# Visualizza log applicazione
tail -f logs/r2r-webui-*.log

# Filtra log per errori
grep -i error logs/r2r-webui-*.log

# Filtra log per tenant specifico
grep -i "tenantid: 123" logs/r2r-webui-*.log
```

#### Diagnostica Database

```sql
-- Verifica connessioni attive
SELECT * FROM sys.dm_exec_connections;

-- Verifica query in esecuzione
SELECT * FROM sys.dm_exec_requests;

-- Verifica query lente
SELECT * FROM sys.dm_exec_query_stats
ORDER BY total_elapsed_time DESC;
```

#### Diagnostica Redis

```bash
# Connessione a Redis CLI
redis-cli -h 192.168.1.4 -p 6379 -a StrongRedisPassword123

# Statistiche Redis
INFO

# Monitoraggio comandi in tempo reale
MONITOR

# Statistiche memoria
MEMORY STATS
```

## Riferimenti

### Documentazione Correlata

- [Architettura del Sistema](../architettura_sistema.md)
- [Modello Dati](../modello_dati.md)
- [Autenticazione e Autorizzazione](../autenticazione_autorizzazione.md)
- [Integrazione R2R e Code](../integrazione_r2r_code.md)
- [Integrazione Redis](../integrazione_redis.md)
- [Scalabilità e Robustezza](../scalabilita_robustezza.md)

### Risorse Esterne

- [Documentazione SciPhi AI R2R](https://r2r-docs.sciphi.ai/documentation/overview)
- [API SciPhi AI R2R](https://r2r-docs.sciphi.ai/api-and-sdks/introduction)
- [Documentazione .NET 9.0](https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9)
- [Documentazione SQL Server](https://docs.microsoft.com/en-us/sql/sql-server)
- [Documentazione Redis](https://redis.io/documentation)

## Contribuzione

### Linee Guida per lo Sviluppo

1. **Branching Strategy**:
   - `main`: Branch di produzione, sempre stabile
   - `develop`: Branch di sviluppo principale
   - `feature/*`: Branch per nuove funzionalità
   - `bugfix/*`: Branch per correzioni bug
   - `release/*`: Branch per preparazione release

2. **Workflow di Sviluppo**:
   - Creare branch feature/bugfix da develop
   - Sviluppare e testare localmente
   - Creare pull request verso develop
   - Code review e approvazione
   - Merge in develop
   - Release periodiche da develop a main

3. **Standard di Codice**:
   - Seguire [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
   - Utilizzare [StyleCop](https://github.com/DotNetAnalyzers/StyleCop) per analisi statica
   - Documentare API con commenti XML
   - Scrivere test unitari per nuove funzionalità

### Processo di Build e Test

```bash
# Ripristina dipendenze
dotnet restore

# Build
dotnet build

# Esegui test
dotnet test

# Analisi codice
dotnet format --verify-no-changes

# Pubblica
dotnet publish -c Release
```

## Licenza

Copyright (c) 2025 R2R WebUI Team. Tutti i diritti riservati.
