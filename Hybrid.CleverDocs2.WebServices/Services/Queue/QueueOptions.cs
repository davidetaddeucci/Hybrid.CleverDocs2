namespace Hybrid.CleverDocs2.WebServices.Services.Queue;

/// <summary>
/// Configuration options for RabbitMQ
/// </summary>
public class RabbitMQOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public int Heartbeat { get; set; } = 60;
    public int RequestedConnectionTimeout { get; set; } = 30000;
    public int ConnectionPoolSize { get; set; } = 5;
    public int PrefetchCount { get; set; } = 10;
    public int MessageTtl { get; set; } = 86400000; // 24 hours
    public string DeadLetterExchange { get; set; } = "cleverdocs.dlx";
    public int ManagementPort { get; set; } = 15672;
    public bool EnableRateLimiting { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}

/// <summary>
/// R2R queue configuration
/// </summary>
public static class R2RQueueNames
{
    public const string DocumentIngestion = "r2r.document.ingestion";
    public const string EmbeddingGeneration = "r2r.embedding.generation";
    public const string SearchOperation = "r2r.search.operation";
    public const string CollectionOperation = "r2r.collection.operation";
    public const string ConversationOperation = "r2r.conversation.operation";
    public const string GraphOperation = "r2r.graph.operation";
    public const string AuthOperation = "r2r.auth.operation";
}

/// <summary>
/// R2R exchange configuration
/// </summary>
public static class R2RExchangeNames
{
    public const string Main = "r2r.main";
    public const string DeadLetter = "r2r.dlx";
    public const string Retry = "r2r.retry";
}

/// <summary>
/// R2R routing keys
/// </summary>
public static class R2RRoutingKeys
{
    public const string DocumentIngestion = "document.ingestion";
    public const string EmbeddingGeneration = "embedding.generation";
    public const string SearchOperation = "search.operation";
    public const string CollectionOperation = "collection.operation";
    public const string ConversationOperation = "conversation.operation";
    public const string GraphOperation = "graph.operation";
    public const string AuthOperation = "auth.operation";
}
