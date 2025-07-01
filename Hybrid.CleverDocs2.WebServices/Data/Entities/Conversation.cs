using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hybrid.CleverDocs2.WebServices.Data.Entities
{
    /// <summary>
    /// Represents a conversation entity for chat functionality
    /// Based on R2R Conversations API patterns with collection-based access control
    /// </summary>
    [Table("Conversations")]
    public class Conversation
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// R2R Conversation ID from the external API
        /// </summary>
        [Required]
        [StringLength(255)]
        public string R2RConversationId { get; set; } = string.Empty;

        /// <summary>
        /// Conversation title/name
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Optional conversation description
        /// </summary>
        [StringLength(2000)]
        public string? Description { get; set; }

        /// <summary>
        /// User who owns this conversation
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Company/tenant this conversation belongs to
        /// </summary>
        [Required]
        public Guid CompanyId { get; set; }

        /// <summary>
        /// Collections associated with this conversation (JSON array of collection IDs)
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string CollectionIds { get; set; } = "[]";

        /// <summary>
        /// Conversation status (active, archived, deleted)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "active";

        /// <summary>
        /// Conversation metadata (JSON object for extensibility)
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string? Metadata { get; set; }

        /// <summary>
        /// Chat settings for this conversation (search mode, RAG config, etc.)
        /// Based on R2R hybrid search and agentic RAG configuration
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string? Settings { get; set; }

        /// <summary>
        /// Last message timestamp for sorting
        /// </summary>
        public DateTime LastMessageAt { get; set; }

        /// <summary>
        /// Total message count in this conversation
        /// </summary>
        public int MessageCount { get; set; } = 0;

        /// <summary>
        /// Whether this conversation is pinned/favorited
        /// </summary>
        public bool IsPinned { get; set; } = false;

        /// <summary>
        /// Conversation visibility (private, shared, public)
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Visibility { get; set; } = "private";

        /// <summary>
        /// Shared user IDs (JSON array for shared conversations)
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string? SharedUserIds { get; set; }

        /// <summary>
        /// Conversation tags for organization (JSON array)
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string? Tags { get; set; }

        /// <summary>
        /// Conversation creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("CompanyId")]
        public virtual Company Company { get; set; } = null!;

        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

        // Helper methods for collection management
        public List<string> GetCollectionIdsList()
        {
            if (string.IsNullOrEmpty(CollectionIds) || CollectionIds == "[]")
                return new List<string>();

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(CollectionIds) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        public void SetCollectionIdsList(List<string> collectionIds)
        {
            CollectionIds = System.Text.Json.JsonSerializer.Serialize(collectionIds ?? new List<string>());
        }

        // Helper methods for shared user management
        public List<Guid> GetSharedUserIds()
        {
            if (string.IsNullOrEmpty(SharedUserIds) || SharedUserIds == "[]")
                return new List<Guid>();

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(SharedUserIds) ?? new List<Guid>();
            }
            catch
            {
                return new List<Guid>();
            }
        }

        public void SetSharedUserIds(List<Guid> userIds)
        {
            SharedUserIds = System.Text.Json.JsonSerializer.Serialize(userIds ?? new List<Guid>());
        }

        // Helper methods for tag management
        public List<string> GetTags()
        {
            if (string.IsNullOrEmpty(Tags) || Tags == "[]")
                return new List<string>();

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(Tags) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        public void SetTags(List<string> tags)
        {
            Tags = System.Text.Json.JsonSerializer.Serialize(tags ?? new List<string>());
        }

        // Helper method to check if user has access to conversation
        public bool HasUserAccess(Guid userId)
        {
            // Owner always has access
            if (UserId == userId)
                return true;

            // Check if conversation is shared with user
            if (Visibility == "shared")
            {
                var sharedUsers = GetSharedUserIds();
                return sharedUsers.Contains(userId);
            }

            // Public conversations are accessible to all users in the same company
            if (Visibility == "public")
                return true;

            return false;
        }

        // Helper methods for metadata management
        public Dictionary<string, object> GetMetadata()
        {
            if (string.IsNullOrEmpty(Metadata))
                return new Dictionary<string, object>();

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(Metadata) ?? new Dictionary<string, object>();
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        public void SetMetadata(Dictionary<string, object> metadata)
        {
            Metadata = System.Text.Json.JsonSerializer.Serialize(metadata ?? new Dictionary<string, object>());
        }

        // Helper methods for R2R-based settings management
        public Dictionary<string, object> GetSettings()
        {
            if (string.IsNullOrEmpty(Settings))
                return new Dictionary<string, object>
                {
                    ["searchMode"] = "hybrid", // R2R hybrid search mode
                    ["useVectorSearch"] = true,
                    ["useHybridSearch"] = true,
                    ["semanticWeight"] = 5.0, // R2R default semantic weight
                    ["fullTextWeight"] = 1.0, // R2R default keyword weight
                    ["rrfK"] = 50, // Reciprocal Rank Fusion constant
                    ["maxResults"] = 10,
                    ["relevanceThreshold"] = 0.7,
                    ["includeTitleIfAvailable"] = true,
                    ["useKnowledgeGraph"] = false, // GraphRAG integration
                    ["agenticMode"] = false, // Agentic RAG capabilities
                    ["thinkingBudget"] = 4096, // Token budget for agentic reasoning
                    ["multiStepReasoning"] = false, // Multi-step reasoning
                    ["contextualEnrichment"] = true, // Contextual enrichment
                    ["streamingEnabled"] = true // Streaming responses
                };

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(Settings) ?? new Dictionary<string, object>();
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        public void SetSettings(Dictionary<string, object> settings)
        {
            Settings = System.Text.Json.JsonSerializer.Serialize(settings ?? new Dictionary<string, object>());
        }
    }
}
