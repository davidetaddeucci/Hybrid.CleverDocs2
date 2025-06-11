# Integrazione con Redis per Caching Avanzato

## Panoramica

Questo documento descrive l'architettura e l'implementazione del sistema di caching avanzato basato su Redis per la WebUI multitenant di SciPhi AI R2R. L'obiettivo è migliorare le performance, ridurre il carico sul backend e sul servizio R2R API, e garantire una user experience fluida anche in condizioni di elevato traffico.

## Architettura di Caching

L'architettura di caching è progettata per ottimizzare diversi tipi di dati e operazioni, con strategie specifiche per ciascuna categoria di dati.

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│                 │     │                 │     │                 │
│  Frontend       │     │  Backend        │     │  R2R API        │
│  Blazor         │◄────┤  API Server     │────►│  Server         │
│                 │     │                 │     │                 │
└─────────────────┘     └────────┬────────┘     └─────────────────┘
                                 │                       
                                 ▼                       
                        ┌─────────────────┐              
                        │                 │              
                        │  Redis Cache    │              
                        │                 │              
                        └─────────────────┘              
```

### Livelli di Caching

Il sistema implementa una strategia di caching multi-livello:

1. **Caching di Primo Livello (In-Memory)**:
   - Implementato con `IMemoryCache` di .NET
   - Utilizzato per dati ad accesso frequentissimo e di piccole dimensioni
   - Durata breve (secondi/minuti)
   - Locale a ciascuna istanza del server

2. **Caching di Secondo Livello (Redis Distribuito)**:
   - Implementato con `IDistributedCache` e Redis
   - Utilizzato per dati condivisi tra istanze server
   - Durata media/lunga (minuti/ore)
   - Supporta scenari di alta disponibilità e scalabilità orizzontale

3. **Caching di Output (Response Caching)**:
   - Caching delle risposte HTTP complete
   - Implementato con middleware di Response Caching
   - Configurabile per endpoint specifici
   - Supporta varianti basate su header, query string, claims utente

## Implementazione Redis

### Configurazione Redis

```csharp
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Configurazione Redis
    services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = Configuration.GetConnectionString("Redis");
        options.InstanceName = "R2RWebUI_";
    });
    
    // Caching in-memory
    services.AddMemoryCache();
    
    // Response caching
    services.AddResponseCaching(options =>
    {
        options.MaximumBodySize = 1024 * 1024; // 1MB
        options.UseCaseSensitivePaths = false;
    });
    
    // Registrazione servizi di caching
    services.AddSingleton<ICacheService, RedisCacheService>();
    services.AddSingleton<IResponseCacheService, ResponseCacheService>();
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // Middleware di response caching
    app.UseResponseCaching();
    
    // Middleware custom per controllo cache
    app.UseMiddleware<CacheControlMiddleware>();
}
```

### Servizio di Caching

```csharp
public class RedisCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly IOptions<CacheOptions> _options;
    
    public RedisCacheService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<RedisCacheService> logger,
        IOptions<CacheOptions> options)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
        _options = options;
    }
    
    public async Task<T> GetOrSetAsync<T>(
        string key, 
        Func<Task<T>> factory, 
        CacheLevel level = CacheLevel.Distributed, 
        TimeSpan? expiry = null)
    {
        // Strategia di caching a due livelli
        if (level == CacheLevel.Memory || level == CacheLevel.Both)
        {
            // Prova prima la cache in-memory
            if (_memoryCache.TryGetValue(key, out T cachedItem))
            {
                _logger.LogDebug("Cache hit (memory): {Key}", key);
                return cachedItem;
            }
        }
        
        if (level == CacheLevel.Distributed || level == CacheLevel.Both)
        {
            // Prova la cache distribuita
            var cachedValue = await _distributedCache.GetStringAsync(key);
            
            if (!string.IsNullOrEmpty(cachedValue))
            {
                _logger.LogDebug("Cache hit (distributed): {Key}", key);
                var item = JsonSerializer.Deserialize<T>(cachedValue);
                
                // Se richiesto, memorizza anche in cache locale
                if (level == CacheLevel.Both)
                {
                    var localExpiry = expiry ?? TimeSpan.FromMinutes(_options.Value.MemoryCacheMinutes);
                    _memoryCache.Set(key, item, localExpiry);
                }
                
                return item;
            }
        }
        
        // Cache miss, esegui factory
        _logger.LogDebug("Cache miss: {Key}", key);
        var result = await factory();
        
        // Memorizza il risultato in cache
        await SetAsync(key, result, level, expiry);
        
        return result;
    }
    
    public async Task SetAsync<T>(
        string key, 
        T value, 
        CacheLevel level = CacheLevel.Distributed, 
        TimeSpan? expiry = null)
    {
        if (level == CacheLevel.Memory || level == CacheLevel.Both)
        {
            var localExpiry = expiry ?? TimeSpan.FromMinutes(_options.Value.MemoryCacheMinutes);
            _memoryCache.Set(key, value, localExpiry);
        }
        
        if (level == CacheLevel.Distributed || level == CacheLevel.Both)
        {
            var distributedExpiry = expiry ?? TimeSpan.FromMinutes(_options.Value.DistributedCacheMinutes);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = distributedExpiry
            };
            
            await _distributedCache.SetStringAsync(
                key,
                JsonSerializer.Serialize(value),
                options);
        }
        
        _logger.LogDebug("Cache set: {Key} (Level: {Level}, Expiry: {Expiry})", 
            key, level, expiry ?? TimeSpan.FromMinutes(_options.Value.DistributedCacheMinutes));
    }
    
    public async Task RemoveAsync(string key)
    {
        _memoryCache.Remove(key);
        await _distributedCache.RemoveAsync(key);
        
        _logger.LogDebug("Cache removed: {Key}", key);
    }
    
    public async Task RemoveByPrefixAsync(string prefix)
    {
        // Nota: questa implementazione richiede un server Redis che supporti SCAN
        // e l'accesso diretto al database Redis
        
        using var connection = await ConnectionMultiplexer.ConnectAsync(_options.Value.RedisConnectionString);
        var server = connection.GetServer(connection.GetEndPoints().First());
        var db = connection.GetDatabase();
        
        var fullPrefix = $"{_options.Value.InstanceName}{prefix}";
        
        foreach (var key in server.Keys(pattern: $"{fullPrefix}*"))
        {
            await db.KeyDeleteAsync(key);
            
            // Rimuovi anche dalla cache in-memory se presente
            var shortKey = key.ToString().Substring(_options.Value.InstanceName.Length);
            _memoryCache.Remove(shortKey);
        }
        
        _logger.LogDebug("Cache removed by prefix: {Prefix}", prefix);
    }
}
```

### Response Caching Service

```csharp
public class ResponseCacheService : IResponseCacheService
{
    private readonly ICacheService _cacheService;
    private readonly IOptions<CacheOptions> _options;
    
    public ResponseCacheService(
        ICacheService cacheService,
        IOptions<CacheOptions> options)
    {
        _cacheService = cacheService;
        _options = options;
    }
    
    public async Task<T> CacheResponseAsync<T>(
        string cacheKey, 
        Func<Task<T>> factory, 
        TimeSpan? expiry = null,
        bool bypassCache = false)
    {
        if (bypassCache)
        {
            return await factory();
        }
        
        return await _cacheService.GetOrSetAsync(
            $"response:{cacheKey}",
            factory,
            CacheLevel.Both,
            expiry ?? TimeSpan.FromMinutes(_options.Value.ResponseCacheMinutes));
    }
    
    public async Task RemoveCachedResponseAsync(string cacheKey)
    {
        await _cacheService.RemoveAsync($"response:{cacheKey}");
    }
    
    public async Task RemoveCachedResponsesByPrefixAsync(string prefix)
    {
        await _cacheService.RemoveByPrefixAsync($"response:{prefix}");
    }
}
```

### Middleware Cache Control

```csharp
public class CacheControlMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptions<CacheOptions> _options;
    
    public CacheControlMiddleware(
        RequestDelegate next,
        IOptions<CacheOptions> options)
    {
        _next = next;
        _options = options;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Imposta header Cache-Control in base al path
        var path = context.Request.Path.Value.ToLowerInvariant();
        
        // Imposta cache-control per risorse statiche
        if (path.StartsWith("/static/") || 
            path.EndsWith(".js") || 
            path.EndsWith(".css") || 
            path.EndsWith(".woff2") ||
            path.EndsWith(".png") || 
            path.EndsWith(".jpg") || 
            path.EndsWith(".svg"))
        {
            context.Response.Headers.CacheControl = $"public,max-age={_options.Value.StaticAssetsCacheSeconds}";
        }
        // Imposta cache-control per API
        else if (path.StartsWith("/api/"))
        {
            // API non cacheable di default
            context.Response.Headers.CacheControl = "no-store";
            
            // Eccezioni per API cacheable
            if (path.StartsWith("/api/collections") && context.Request.Method == "GET")
            {
                context.Response.Headers.CacheControl = $"private,max-age={_options.Value.ApiCacheSeconds}";
            }
        }
        // Default per altre risorse
        else
        {
            context.Response.Headers.CacheControl = "no-cache";
        }
        
        await _next(context);
    }
}
```

## Strategie di Caching per Scenari Specifici

### 1. Caching Dati Utente e Sessione

```csharp
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ICacheService _cacheService;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    
    public UserService(
        IUserRepository userRepository,
        ICacheService cacheService,
        ITenantContextAccessor tenantContextAccessor)
    {
        _userRepository = userRepository;
        _cacheService = cacheService;
        _tenantContextAccessor = tenantContextAccessor;
    }
    
    public async Task<UserDto> GetUserByIdAsync(int userId)
    {
        var tenantId = _tenantContextAccessor.GetCurrentTenant()?.Id ?? 0;
        
        return await _cacheService.GetOrSetAsync(
            $"user:{tenantId}:{userId}",
            async () => await _userRepository.GetByIdAsync(userId),
            CacheLevel.Both,
            TimeSpan.FromMinutes(15));
    }
    
    public async Task<IEnumerable<UserDto>> GetUsersByCompanyAsync(int companyId)
    {
        return await _cacheService.GetOrSetAsync(
            $"users:company:{companyId}",
            async () => await _userRepository.GetByCompanyIdAsync(companyId),
            CacheLevel.Distributed,
            TimeSpan.FromMinutes(30));
    }
    
    public async Task UpdateUserAsync(UserDto user)
    {
        await _userRepository.UpdateAsync(user);
        
        // Invalida cache
        var tenantId = _tenantContextAccessor.GetCurrentTenant()?.Id ?? 0;
        await _cacheService.RemoveAsync($"user:{tenantId}:{user.Id}");
        await _cacheService.RemoveAsync($"users:company:{user.CompanyId}");
    }
}
```

### 2. Caching Collezioni e Documenti

```csharp
public class CollectionService : ICollectionService
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly ICacheService _cacheService;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    
    public CollectionService(
        ICollectionRepository collectionRepository,
        ICacheService cacheService,
        ITenantContextAccessor tenantContextAccessor)
    {
        _collectionRepository = collectionRepository;
        _cacheService = cacheService;
        _tenantContextAccessor = tenantContextAccessor;
    }
    
    public async Task<CollectionDto> GetCollectionByIdAsync(int collectionId)
    {
        var tenantId = _tenantContextAccessor.GetCurrentTenant()?.Id ?? 0;
        
        return await _cacheService.GetOrSetAsync(
            $"collection:{tenantId}:{collectionId}",
            async () => await _collectionRepository.GetByIdAsync(collectionId),
            CacheLevel.Both,
            TimeSpan.FromMinutes(30));
    }
    
    public async Task<IEnumerable<CollectionDto>> GetCollectionsByUserAsync(int userId)
    {
        var tenantId = _tenantContextAccessor.GetCurrentTenant()?.Id ?? 0;
        
        return await _cacheService.GetOrSetAsync(
            $"collections:user:{tenantId}:{userId}",
            async () => await _collectionRepository.GetByUserIdAsync(userId),
            CacheLevel.Distributed,
            TimeSpan.FromMinutes(15));
    }
    
    public async Task<CollectionDto> CreateCollectionAsync(CreateCollectionRequest request, int userId, int companyId)
    {
        var collection = await _collectionRepository.CreateAsync(request, userId, companyId);
        
        // Invalida cache delle liste di collezioni
        await _cacheService.RemoveAsync($"collections:user:{companyId}:{userId}");
        await _cacheService.RemoveAsync($"collections:company:{companyId}");
        
        return collection;
    }
    
    public async Task UpdateCollectionAsync(int collectionId, UpdateCollectionRequest request)
    {
        await _collectionRepository.UpdateAsync(collectionId, request);
        
        // Invalida cache
        var tenantId = _tenantContextAccessor.GetCurrentTenant()?.Id ?? 0;
        await _cacheService.RemoveAsync($"collection:{tenantId}:{collectionId}");
        
        // Invalida anche liste potenzialmente impattate
        var collection = await _collectionRepository.GetByIdAsync(collectionId);
        await _cacheService.RemoveAsync($"collections:user:{tenantId}:{collection.UserId}");
        await _cacheService.RemoveAsync($"collections:company:{collection.CompanyId}");
    }
}
```

### 3. Caching Risultati R2R API

```csharp
public class R2RService : IR2RService
{
    private readonly IR2RClient _r2rClient;
    private readonly ICacheService _cacheService;
    private readonly IApiKeyService _apiKeyService;
    
    public R2RService(
        IR2RClient r2rClient,
        ICacheService cacheService,
        IApiKeyService apiKeyService)
    {
        _r2rClient = r2rClient;
        _cacheService = cacheService;
        _apiKeyService = apiKeyService;
    }
    
    public async Task<DocumentResponse> GetDocumentAsync(string documentId, int companyId)
    {
        var apiKey = await _apiKeyService.GetApiKeyAsync(companyId, "r2r");
        
        return await _cacheService.GetOrSetAsync(
            $"r2r:document:{documentId}",
            async () => await _r2rClient.GetDocumentAsync(documentId, apiKey),
            CacheLevel.Distributed,
            TimeSpan.FromHours(24)); // Documenti sono relativamente statici
    }
    
    public async Task<CollectionsResponse> GetCollectionsAsync(int companyId)
    {
        var apiKey = await _apiKeyService.GetApiKeyAsync(companyId, "r2r");
        
        return await _cacheService.GetOrSetAsync(
            $"r2r:collections:{companyId}",
            async () => await _r2rClient.GetCollectionsAsync(apiKey),
            CacheLevel.Distributed,
            TimeSpan.FromMinutes(30));
    }
    
    public async Task<ChatCompletionResponse> ChatCompletionAsync(
        ChatCompletionRequest request, 
        int companyId,
        bool bypassCache = false)
    {
        // Per le chat completions, possiamo cachare solo se non è richiesto bypass
        // e se la richiesta non contiene messaggi dell'utente (solo system prompt)
        if (!bypassCache && request.Messages.All(m => m.Role != "user"))
        {
            var cacheKey = $"r2r:chat:{companyId}:{ComputeHash(JsonSerializer.Serialize(request))}";
            
            var apiKey = await _apiKeyService.GetApiKeyAsync(companyId, "r2r");
            
            return await _cacheService.GetOrSetAsync(
                cacheKey,
                async () => await _r2rClient.ChatCompletionAsync(request, apiKey),
                CacheLevel.Distributed,
                TimeSpan.FromHours(1));
        }
        
        // Altrimenti esegui senza cache
        var apiKeyDirect = await _apiKeyService.GetApiKeyAsync(companyId, "r2r");
        return await _r2rClient.ChatCompletionAsync(request, apiKeyDirect);
    }
    
    private string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}
```

### 4. Caching Throttling e Rate Limiting

```csharp
public class ThrottlingService : IThrottlingService
{
    private readonly ICacheService _cacheService;
    private readonly ICompanySettingsService _companySettingsService;
    private readonly ILogger<ThrottlingService> _logger;
    
    public ThrottlingService(
        ICacheService cacheService,
        ICompanySettingsService companySettingsService,
        ILogger<ThrottlingService> logger)
    {
        _cacheService = cacheService;
        _companySettingsService = companySettingsService;
        _logger = logger;
    }
    
    public async Task<bool> CheckThrottlingLimitAsync(int companyId, ThrottlingType throttlingType)
    {
        // Ottieni limiti dalle impostazioni (con cache)
        var settings = await _companySettingsService.GetCompanySettingsAsync(companyId);
        
        int limit;
        string cacheKey;
        TimeSpan period;
        
        switch (throttlingType)
        {
            case ThrottlingType.DocumentUpload:
                limit = settings.MaxDocumentsPerHour;
                cacheKey = $"throttle:doc_upload:{companyId}";
                period = TimeSpan.FromHours(1);
                break;
                
            case ThrottlingType.ChatCompletion:
                limit = settings.MaxChatCompletionsPerHour;
                cacheKey = $"throttle:chat_completion:{companyId}";
                period = TimeSpan.FromHours(1);
                break;
                
            default:
                throw new ArgumentOutOfRangeException(nameof(throttlingType));
        }
        
        // Implementazione con Redis Sorted Set per sliding window rate limiting
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var windowStart = now - (long)period.TotalSeconds;
        
        using var connection = await ConnectionMultiplexer.ConnectAsync(_cacheService.GetConnectionString());
        var db = connection.GetDatabase();
        
        // Aggiungi timestamp corrente al sorted set
        await db.SortedSetAddAsync(cacheKey, now.ToString(), now);
        
        // Rimuovi elementi fuori dalla finestra
        await db.SortedSetRemoveRangeByScoreAsync(cacheKey, 0, windowStart);
        
        // Imposta TTL per pulizia automatica
        await db.KeyExpireAsync(cacheKey, period.Add(TimeSpan.FromMinutes(5)));
        
        // Conta elementi nel set
        var count = await db.SortedSetLengthAsync(cacheKey);
        
        if (count > limit)
        {
            _logger.LogWarning("Throttling limit reached for company {CompanyId}, type {ThrottlingType}, limit {Limit}", 
                companyId, throttlingType, limit);
            return false;
        }
        
        return true;
    }
}
```

## Configurazione Redis Avanzata

### Configurazione High Availability

```json
{
  "Redis": {
    "ConnectionString": "192.168.1.4:6379,abortConnect=false,connectTimeout=30000,password=StrongRedisPassword123",
    "InstanceName": "R2RWebUI_",
    "DefaultDatabase": 0,
    "ConnectRetry": 3,
    "ConnectTimeout": 5000,
    "SyncTimeout": 5000,
    "ResponseTimeout": 5000,
    "KeepAlive": 60,
    "ConfigCheckSeconds": 60,
    "AbortOnConnectFail": false
  }
}
```

### Configurazione Sentinel per Alta Disponibilità

```csharp
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Configurazione Redis con Sentinel
    services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
        var sentinelConfig = new ConfigurationOptions
        {
            CommandMap = CommandMap.Sentinel,
            AbortOnConnectFail = false
        };
        
        // Aggiungi endpoint Sentinel
        sentinelConfig.EndPoints.Add("192.168.1.10:26379");
        sentinelConfig.EndPoints.Add("192.168.1.11:26379");
        sentinelConfig.EndPoints.Add("192.168.1.12:26379");
        
        // Connetti a Sentinel
        var sentinelConnection = ConnectionMultiplexer.Connect(sentinelConfig);
        
        // Ottieni master da Sentinel
        var masterConfig = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            Password = Configuration["Redis:Password"],
            DefaultDatabase = int.Parse(Configuration["Redis:DefaultDatabase"]),
            ConnectRetry = int.Parse(Configuration["Redis:ConnectRetry"]),
            ConnectTimeout = int.Parse(Configuration["Redis:ConnectTimeout"]),
            SyncTimeout = int.Parse(Configuration["Redis:SyncTimeout"]),
            ResponseTimeout = int.Parse(Configuration["Redis:ResponseTimeout"]),
            KeepAlive = int.Parse(Configuration["Redis:KeepAlive"])
        };
        
        foreach (var sentinel in sentinelConnection.GetServers())
        {
            var masterInfo = sentinel.SentinelMasters().First();
            var masterEndpoint = $"{masterInfo["ip"]}:{masterInfo["port"]}";
            masterConfig.EndPoints.Add(masterEndpoint);
        }
        
        return ConnectionMultiplexer.Connect(masterConfig);
    });
    
    // Registra IDistributedCache con implementazione Redis
    services.AddSingleton<IDistributedCache>(sp =>
    {
        var connection = sp.GetRequiredService<IConnectionMultiplexer>();
        var options = sp.GetRequiredService<IOptions<RedisCacheOptions>>();
        return new RedisCache(options);
    });
}
```

### Configurazione Redis Cluster

```csharp
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Configurazione Redis Cluster
    services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
        var clusterConfig = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            Password = Configuration["Redis:Password"],
            DefaultDatabase = int.Parse(Configuration["Redis:DefaultDatabase"]),
            ConnectRetry = int.Parse(Configuration["Redis:ConnectRetry"]),
            ConnectTimeout = int.Parse(Configuration["Redis:ConnectTimeout"]),
            SyncTimeout = int.Parse(Configuration["Redis:SyncTimeout"]),
            ResponseTimeout = int.Parse(Configuration["Redis:ResponseTimeout"]),
            KeepAlive = int.Parse(Configuration["Redis:KeepAlive"])
        };
        
        // Aggiungi endpoint cluster
        foreach (var endpoint in Configuration.GetSection("Redis:ClusterEndpoints").Get<string[]>())
        {
            clusterConfig.EndPoints.Add(endpoint);
        }
        
        return ConnectionMultiplexer.Connect(clusterConfig);
    });
}
```

## Monitoraggio e Diagnostica Redis

### Metriche Redis

```csharp
public class RedisCacheMetrics : IHostedService, IDisposable
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisCacheMetrics> _logger;
    private readonly IMetricsRegistry _metricsRegistry;
    private Timer _timer;
    
    public RedisCacheMetrics(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisCacheMetrics> logger,
        IMetricsRegistry metricsRegistry)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _logger = logger;
        _metricsRegistry = metricsRegistry;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Redis Cache Metrics service starting");
        
        _timer = new Timer(CollectMetrics, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        
        return Task.CompletedTask;
    }
    
    private void CollectMetrics(object state)
    {
        try
        {
            foreach (var endpoint in _connectionMultiplexer.GetEndPoints())
            {
                var server = _connectionMultiplexer.GetServer(endpoint);
                
                if (!server.IsConnected)
                {
                    continue;
                }
                
                var info = server.Info();
                var memory = info.SelectMany(i => i.Where(x => x.Key == "used_memory")).FirstOrDefault();
                var clients = info.SelectMany(i => i.Where(x => x.Key == "connected_clients")).FirstOrDefault();
                var ops = info.SelectMany(i => i.Where(x => x.Key == "instantaneous_ops_per_sec")).FirstOrDefault();
                var hitRate = info.SelectMany(i => i.Where(x => x.Key == "keyspace_hits")).FirstOrDefault();
                var missRate = info.SelectMany(i => i.Where(x => x.Key == "keyspace_misses")).FirstOrDefault();
                
                if (memory.Value != null)
                {
                    _metricsRegistry.Gauge("redis_memory_used", double.Parse(memory.Value), new Dictionary<string, string> { { "endpoint", endpoint.ToString() } });
                }
                
                if (clients.Value != null)
                {
                    _metricsRegistry.Gauge("redis_connected_clients", double.Parse(clients.Value), new Dictionary<string, string> { { "endpoint", endpoint.ToString() } });
                }
                
                if (ops.Value != null)
                {
                    _metricsRegistry.Gauge("redis_ops_per_sec", double.Parse(ops.Value), new Dictionary<string, string> { { "endpoint", endpoint.ToString() } });
                }
                
                if (hitRate.Value != null && missRate.Value != null)
                {
                    var hits = double.Parse(hitRate.Value);
                    var misses = double.Parse(missRate.Value);
                    var total = hits + misses;
                    
                    if (total > 0)
                    {
                        _metricsRegistry.Gauge("redis_hit_rate", hits / total * 100, new Dictionary<string, string> { { "endpoint", endpoint.ToString() } });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting Redis metrics");
        }
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Redis Cache Metrics service stopping");
        
        _timer?.Change(Timeout.Infinite, 0);
        
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        _timer?.Dispose();
    }
}
```

### Health Check Redis

```csharp
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Health check Redis
    services.AddHealthChecks()
        .AddRedis(
            Configuration.GetConnectionString("Redis"),
            name: "redis-cache",
            tags: new[] { "cache", "redis" },
            timeout: TimeSpan.FromSeconds(5));
}
```

## Best Practices e Considerazioni

### 1. Gestione Chiavi Cache

- **Namespace Gerarchico**: Utilizzare prefissi gerarchici per organizzare le chiavi (es. `user:123:profile`)
- **Inclusione Tenant ID**: Includere sempre l'ID tenant nelle chiavi per garantire isolamento dati
- **Hashing per Chiavi Lunghe**: Utilizzare hash per chiavi basate su contenuti complessi
- **TTL Appropriati**: Impostare TTL in base alla volatilità dei dati

### 2. Invalidazione Cache

- **Invalidazione Selettiva**: Invalidare solo le chiavi necessarie, non l'intera cache
- **Invalidazione a Cascata**: Considerare dipendenze tra dati per invalidazione coerente
- **Versioning delle Chiavi**: Utilizzare versioning per invalidazione atomica (es. `user:123:profile:v2`)
- **Pubblicazione Eventi**: Utilizzare Redis Pub/Sub per notificare invalidazioni tra istanze

### 3. Ottimizzazione Memoria

- **Compressione Dati**: Comprimere dati di grandi dimensioni prima del caching
- **Serializzazione Efficiente**: Utilizzare formati binari come MessagePack invece di JSON
- **Memorizzazione Selettiva**: Cachare solo i dati necessari, non interi oggetti
- **Monitoraggio Utilizzo**: Implementare alert per utilizzo memoria elevato

### 4. Resilienza

- **Circuit Breaker**: Implementare circuit breaker per gestire fallimenti Redis
- **Fallback Graceful**: Prevedere fallback a database o cache in-memory in caso di errori
- **Retry con Backoff**: Implementare retry con backoff esponenziale per operazioni fallite
- **Timeout Appropriati**: Impostare timeout adeguati per evitare blocchi

## Esempi di Implementazione nei Controller

### Controller con Response Caching

```csharp
[ApiController]
[Route("api/collections")]
public class CollectionsController : ControllerBase
{
    private readonly ICollectionService _collectionService;
    private readonly IResponseCacheService _responseCacheService;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    
    public CollectionsController(
        ICollectionService collectionService,
        IResponseCacheService responseCacheService,
        ITenantContextAccessor tenantContextAccessor)
    {
        _collectionService = collectionService;
        _responseCacheService = responseCacheService;
        _tenantContextAccessor = tenantContextAccessor;
    }
    
    [HttpGet]
    [Authorize(Policy = "RequireUserRole")]
    [ResponseCache(Duration = 60)] // Cache lato client
    public async Task<IActionResult> GetCollections([FromQuery] bool refresh = false)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.Name).Value);
        var tenantId = _tenantContextAccessor.GetCurrentTenant()?.Id ?? 0;
        
        // Usa response caching con possibilità di bypass
        var collections = await _responseCacheService.CacheResponseAsync(
            $"collections:{tenantId}:{userId}",
            async () => await _collectionService.GetCollectionsByUserAsync(userId),
            TimeSpan.FromMinutes(5),
            refresh);
            
        return Ok(collections);
    }
    
    [HttpGet("{id}")]
    [Authorize(Policy = "RequireUserRole")]
    [ResponseCache(Duration = 60)] // Cache lato client
    public async Task<IActionResult> GetCollection(int id, [FromQuery] bool refresh = false)
    {
        var tenantId = _tenantContextAccessor.GetCurrentTenant()?.Id ?? 0;
        
        // Usa response caching con possibilità di bypass
        var collection = await _responseCacheService.CacheResponseAsync(
            $"collection:{tenantId}:{id}",
            async () => await _collectionService.GetCollectionByIdAsync(id),
            TimeSpan.FromMinutes(10),
            refresh);
            
        if (collection == null)
        {
            return NotFound();
        }
        
        return Ok(collection);
    }
    
    [HttpPost]
    [Authorize(Policy = "RequireUserRole")]
    public async Task<IActionResult> CreateCollection(CreateCollectionRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.Name).Value);
        var tenantId = _tenantContextAccessor.GetCurrentTenant()?.Id ?? 0;
        
        var collection = await _collectionService.CreateCollectionAsync(request, userId, tenantId);
        
        // Invalida cache delle liste di collezioni
        await _responseCacheService.RemoveCachedResponseAsync($"collections:{tenantId}:{userId}");
        
        return CreatedAtAction(nameof(GetCollection), new { id = collection.Id }, collection);
    }
    
    [HttpPut("{id}")]
    [Authorize(Policy = "RequireUserRole")]
    public async Task<IActionResult> UpdateCollection(int id, UpdateCollectionRequest request)
    {
        await _collectionService.UpdateCollectionAsync(id, request);
        
        // Invalida cache
        var tenantId = _tenantContextAccessor.GetCurrentTenant()?.Id ?? 0;
        await _responseCacheService.RemoveCachedResponseAsync($"collection:{tenantId}:{id}");
        
        // Invalida anche liste potenzialmente impattate
        var userId = int.Parse(User.FindFirst(ClaimTypes.Name).Value);
        await _responseCacheService.RemoveCachedResponseAsync($"collections:{tenantId}:{userId}");
        
        return NoContent();
    }
    
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireUserRole")]
    public async Task<IActionResult> DeleteCollection(int id)
    {
        await _collectionService.DeleteCollectionAsync(id);
        
        // Invalida cache
        var tenantId = _tenantContextAccessor.GetCurrentTenant()?.Id ?? 0;
        await _responseCacheService.RemoveCachedResponseAsync($"collection:{tenantId}:{id}");
        
        // Invalida anche liste potenzialmente impattate
        var userId = int.Parse(User.FindFirst(ClaimTypes.Name).Value);
        await _responseCacheService.RemoveCachedResponseAsync($"collections:{tenantId}:{userId}");
        
        return NoContent();
    }
}
```

### Controller Chatbot con Caching

```csharp
[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IR2RService _r2rService;
    private readonly IThrottlingService _throttlingService;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    
    public ChatController(
        IChatService chatService,
        IR2RService r2rService,
        IThrottlingService throttlingService,
        ITenantContextAccessor tenantContextAccessor)
    {
        _chatService = chatService;
        _r2rService = r2rService;
        _throttlingService = throttlingService;
        _tenantContextAccessor = tenantContextAccessor;
    }
    
    [HttpPost("completions")]
    [Authorize(Policy = "RequireUserRole")]
    public async Task<IActionResult> ChatCompletion(ChatCompletionRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.Name).Value);
        var tenantId = _tenantContextAccessor.GetCurrentTenant()?.Id ?? 0;
        
        // Verifica limiti di throttling
        if (!await _throttlingService.CheckThrottlingLimitAsync(tenantId, ThrottlingType.ChatCompletion))
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "Chat completion limit reached" });
        }
        
        // Determina se bypassare la cache
        var bypassCache = request.Messages.Count > 1; // Bypass se non è solo system prompt
        
        // Ottieni risposta (con potenziale caching)
        var response = await _r2rService.ChatCompletionAsync(request, tenantId, bypassCache);
        
        // Salva conversazione
        if (request.ConversationId.HasValue)
        {
            await _chatService.SaveMessageAsync(new SaveMessageRequest
            {
                ConversationId = request.ConversationId.Value,
                Role = "user",
                Content = request.Messages.Last().Content
            });
            
            await _chatService.SaveMessageAsync(new SaveMessageRequest
            {
                ConversationId = request.ConversationId.Value,
                Role = "assistant",
                Content = response.Choices.First().Message.Content
            });
        }
        
        return Ok(response);
    }
    
    [HttpGet("conversations")]
    [Authorize(Policy = "RequireUserRole")]
    public async Task<IActionResult> GetConversations()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.Name).Value);
        var conversations = await _chatService.GetConversationsByUserIdAsync(userId);
        return Ok(conversations);
    }
    
    [HttpGet("conversations/{id}")]
    [Authorize(Policy = "RequireUserRole")]
    public async Task<IActionResult> GetConversation(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.Name).Value);
        var conversation = await _chatService.GetConversationByIdAsync(id, userId);
        
        if (conversation == null)
        {
            return NotFound();
        }
        
        return Ok(conversation);
    }
    
    [HttpPost("conversations")]
    [Authorize(Policy = "RequireUserRole")]
    public async Task<IActionResult> CreateConversation(CreateConversationRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.Name).Value);
        var conversation = await _chatService.CreateConversationAsync(request, userId);
        return CreatedAtAction(nameof(GetConversation), new { id = conversation.Id }, conversation);
    }
}
```

## Conclusioni

L'integrazione di Redis come sistema di caching avanzato nella WebUI multitenant per SciPhi AI R2R offre significativi vantaggi in termini di performance, scalabilità e user experience. L'architettura multi-livello di caching permette di ottimizzare diversi tipi di dati e operazioni, riducendo il carico sul backend e sul servizio R2R API.

Le strategie di caching implementate sono progettate per garantire:

1. **Performance elevate** anche con carichi di lavoro intensi
2. **Scalabilità orizzontale** attraverso caching distribuito
3. **Resilienza** con meccanismi di fallback e circuit breaker
4. **Isolamento dati** tra tenant attraverso namespace di chiavi
5. **Monitoraggio** completo dell'utilizzo e delle performance

L'uso di Redis come backbone per caching, rate limiting e gestione sessioni crea un'infrastruttura robusta e flessibile, capace di supportare le esigenze di un sistema enterprise-grade.
