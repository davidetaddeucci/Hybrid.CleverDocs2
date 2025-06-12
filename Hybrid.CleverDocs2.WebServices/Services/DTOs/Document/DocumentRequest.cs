using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.DTOs.Document;

public class DocumentRequest
{
    [JsonPropertyName("file")]
    public IFormFile? File { get; set; }

    [JsonPropertyName("raw_text")]
    public string? RawText { get; set; }

    [JsonPropertyName("chunks")]
    public List<DocumentChunkRequest>? Chunks { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    [JsonPropertyName("collection_ids")]
    public List<string>? CollectionIds { get; set; }

    [JsonPropertyName("document_type")]
    public string? DocumentType { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }
}

public class DocumentChunkRequest
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class DocumentListRequest
{
    [JsonPropertyName("ids")]
    public List<string>? Ids { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; } = 0;

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 100;

    [JsonPropertyName("include_summary_embeddings")]
    public bool IncludeSummaryEmbeddings { get; set; } = false;

    [JsonPropertyName("owner_only")]
    public bool OwnerOnly { get; set; } = false;
}

public class DocumentListResponse
{
    [JsonPropertyName("results")]
    public List<DocumentResponse> Results { get; set; } = new();

    [JsonPropertyName("total_entries")]
    public int TotalEntries { get; set; }
}

public class DocumentMetadataRequest
{
    [JsonPropertyName("metadata")]
    public List<MetadataItem> Metadata { get; set; } = new();
}

public class MetadataItem
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public object Value { get; set; } = new();
}

public class DocumentSearchRequest
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("search_settings")]
    public SearchSettings? SearchSettings { get; set; }
}

public class SearchSettings
{
    [JsonPropertyName("filters")]
    public Dictionary<string, object>? Filters { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 10;

    [JsonPropertyName("use_hybrid_search")]
    public bool UseHybridSearch { get; set; } = false;

    [JsonPropertyName("hybrid_settings")]
    public HybridSettings? HybridSettings { get; set; }
}

public class HybridSettings
{
    [JsonPropertyName("full_text_weight")]
    public double FullTextWeight { get; set; } = 1.0;

    [JsonPropertyName("semantic_weight")]
    public double SemanticWeight { get; set; } = 5.0;

    [JsonPropertyName("full_text_limit")]
    public int FullTextLimit { get; set; } = 200;

    [JsonPropertyName("rrf_k")]
    public int RrfK { get; set; } = 50;
}
