using System.Text.Json.Serialization;

namespace Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Document;

public class DocumentEntityResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    [JsonPropertyName("chunk_ids")]
    public List<string> ChunkIds { get; set; } = new();

    [JsonPropertyName("document_id")]
    public string DocumentId { get; set; } = string.Empty;
}

public class DocumentRelationshipResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("predicate")]
    public string Predicate { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("weight")]
    public double? Weight { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    [JsonPropertyName("chunk_ids")]
    public List<string> ChunkIds { get; set; } = new();

    [JsonPropertyName("document_id")]
    public string DocumentId { get; set; } = string.Empty;
}

public class DocumentExtractionRequest
{
    [JsonPropertyName("settings")]
    public ExtractionSettings? Settings { get; set; }
}

public class ExtractionSettings
{
    [JsonPropertyName("entity_types")]
    public List<string>? EntityTypes { get; set; }

    [JsonPropertyName("relationship_types")]
    public List<string>? RelationshipTypes { get; set; }

    [JsonPropertyName("extraction_prompt")]
    public string? ExtractionPrompt { get; set; }
}

public class DocumentExportRequest
{
    [JsonPropertyName("document_ids")]
    public List<string>? DocumentIds { get; set; }

    [JsonPropertyName("columns")]
    public List<string>? Columns { get; set; }
}