using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hybrid.CleverDocs2.WebServices.Data.Entities
{
    /// <summary>
    /// Represents a message entity within a conversation
    /// Based on R2R Messages API patterns with threading and citation support
    /// </summary>
    [Table("Messages")]
    public class Message
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// R2R Message ID from the external API
        /// </summary>
        [StringLength(255)]
        public string? R2RMessageId { get; set; }

        /// <summary>
        /// Conversation this message belongs to
        /// </summary>
        [Required]
        public int ConversationId { get; set; }

        /// <summary>
        /// User who created this message
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Message role (user, assistant, system)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Message content/text
        /// </summary>
        [Required]
        [Column(TypeName = "text")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Parent message ID for threading (R2R conversation branching)
        /// </summary>
        public int? ParentMessageId { get; set; }

        /// <summary>
        /// Message metadata (JSON object for extensibility)
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string? Metadata { get; set; }

        /// <summary>
        /// Citations and search results (JSON array)
        /// Based on R2R search results and document references
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string? Citations { get; set; }

        /// <summary>
        /// Message edit history tracking
        /// </summary>
        public bool IsEdited { get; set; } = false;

        /// <summary>
        /// Original message content before any edits
        /// </summary>
        [Column(TypeName = "text")]
        public string? OriginalContent { get; set; }

        /// <summary>
        /// Edit history (JSON array of edit records)
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string? EditHistory { get; set; }

        /// <summary>
        /// Last edit timestamp
        /// </summary>
        public DateTime? LastEditedAt { get; set; }

        /// <summary>
        /// User who last edited this message
        /// </summary>
        public Guid? LastEditedByUserId { get; set; }

        /// <summary>
        /// RAG context used for this message (JSON object)
        /// Includes retrieved documents, search parameters, etc.
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string? RagContext { get; set; }

        /// <summary>
        /// Confidence score for AI-generated responses (0.0 - 1.0)
        /// </summary>
        public double? ConfidenceScore { get; set; }

        /// <summary>
        /// Processing time in milliseconds for AI responses
        /// </summary>
        public int? ProcessingTimeMs { get; set; }

        /// <summary>
        /// Token count for this message
        /// </summary>
        public int? TokenCount { get; set; }

        /// <summary>
        /// Message status (sent, processing, completed, failed)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "sent";

        /// <summary>
        /// Error message if processing failed
        /// </summary>
        [StringLength(2000)]
        public string? ErrorMessage { get; set; }



        /// <summary>
        /// Message creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ConversationId")]
        public virtual Conversation Conversation { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("ParentMessageId")]
        public virtual Message? ParentMessage { get; set; }

        [ForeignKey("LastEditedByUserId")]
        public virtual User? LastEditedByUser { get; set; }

        public virtual ICollection<Message> ChildMessages { get; set; } = new List<Message>();

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

        // Helper methods for citations management
        public List<Dictionary<string, object>> GetCitations()
        {
            if (string.IsNullOrEmpty(Citations))
                return new List<Dictionary<string, object>>();

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(Citations) ?? new List<Dictionary<string, object>>();
            }
            catch
            {
                return new List<Dictionary<string, object>>();
            }
        }

        public void SetCitations(List<Dictionary<string, object>> citations)
        {
            Citations = System.Text.Json.JsonSerializer.Serialize(citations ?? new List<Dictionary<string, object>>());
        }

        // Helper methods for RAG context management
        public Dictionary<string, object> GetRagContext()
        {
            if (string.IsNullOrEmpty(RagContext))
                return new Dictionary<string, object>();

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(RagContext) ?? new Dictionary<string, object>();
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        public void SetRagContext(Dictionary<string, object> ragContext)
        {
            RagContext = System.Text.Json.JsonSerializer.Serialize(ragContext ?? new Dictionary<string, object>());
        }

        // Helper method to check if message is from AI assistant
        public bool IsAssistantMessage => Role.Equals("assistant", StringComparison.OrdinalIgnoreCase);

        // Helper method to check if message is from user
        public bool IsUserMessage => Role.Equals("user", StringComparison.OrdinalIgnoreCase);

        // Helper method to check if message is system message
        public bool IsSystemMessage => Role.Equals("system", StringComparison.OrdinalIgnoreCase);

        // Helper method to get thread depth
        public int GetThreadDepth()
        {
            int depth = 0;
            var current = this.ParentMessage;
            while (current != null)
            {
                depth++;
                current = current.ParentMessage;
            }
            return depth;
        }

        // Helper methods for edit history management
        public List<MessageEditRecord> GetEditHistory()
        {
            if (string.IsNullOrEmpty(EditHistory))
                return new List<MessageEditRecord>();

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<MessageEditRecord>>(EditHistory) ?? new List<MessageEditRecord>();
            }
            catch
            {
                return new List<MessageEditRecord>();
            }
        }

        public void AddEditRecord(string previousContent, Guid editedByUserId, string? editReason = null)
        {
            var editHistory = GetEditHistory();

            // Store original content on first edit
            if (!IsEdited)
            {
                OriginalContent = previousContent;
                IsEdited = true;
            }

            editHistory.Add(new MessageEditRecord
            {
                PreviousContent = previousContent,
                EditedAt = DateTime.UtcNow,
                EditedByUserId = editedByUserId,
                EditReason = editReason
            });

            EditHistory = System.Text.Json.JsonSerializer.Serialize(editHistory);
            LastEditedAt = DateTime.UtcNow;
            LastEditedByUserId = editedByUserId;
        }
    }

    /// <summary>
    /// Represents a single edit record in message history
    /// </summary>
    public class MessageEditRecord
    {
        public string PreviousContent { get; set; } = string.Empty;
        public DateTime EditedAt { get; set; }
        public Guid EditedByUserId { get; set; }
        public string? EditReason { get; set; }
    }
}
