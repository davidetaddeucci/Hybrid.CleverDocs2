using Microsoft.EntityFrameworkCore;
using Hybrid.CleverDocs2.WebServices.Data.Entities;
using Hybrid.CleverDocs2.WebServices.Models.Auth;

namespace Hybrid.CleverDocs2.WebServices.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }



        // Core entities
        public DbSet<Company> Companies { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<Collection> Collections { get; set; } = null!;
        public DbSet<CollectionDocument> CollectionDocuments { get; set; } = null!;
        public DbSet<DocumentChunk> DocumentChunks { get; set; } = null!;
        public DbSet<IngestionJob> IngestionJobs { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        // Auth entities
        public DbSet<TokenBlacklist> TokenBlacklists { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        // Dashboard widgets
        public DbSet<UserDashboardWidget> UserDashboardWidgets { get; set; } = null!;
        public DbSet<WidgetTemplate> WidgetTemplates { get; set; } = null!;

        // Chat entities
        public DbSet<Conversation> Conversations { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure entity relationships and constraints
            ConfigureCompany(modelBuilder);
            ConfigureUser(modelBuilder);
            ConfigureDocument(modelBuilder);
            ConfigureCollection(modelBuilder);
            ConfigureDocumentChunk(modelBuilder);
            ConfigureAuditLog(modelBuilder);
            ConfigureAuth(modelBuilder);
            ConfigureConversation(modelBuilder);
            ConfigureMessage(modelBuilder);
        }

        private static void ConfigureCompany(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Company>(entity =>
            {
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.ContactEmail).IsUnique();
                
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }

        private static void ConfigureUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => new { e.CompanyId, e.Email }).IsUnique();
                
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Company)
                    .WithMany(e => e.Users)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureDocument(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasIndex(e => e.FileHash);
                entity.HasIndex(e => new { e.CompanyId, e.UserId });
                entity.HasIndex(e => e.R2RDocumentId);

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Configure JSON properties for PostgreSQL jsonb compatibility
                entity.Property(e => e.Tags)
                    .HasColumnType("jsonb");

                entity.Property(e => e.Metadata)
                    .HasColumnType("jsonb");

                entity.HasOne(e => e.Company)
                    .WithMany(e => e.Documents)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.User)
                    .WithMany(e => e.Documents)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureCollection(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Collection>(entity =>
            {
                entity.HasIndex(e => new { e.CompanyId, e.Name }).IsUnique();
                entity.HasIndex(e => e.R2RCollectionId);
                
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Company)
                    .WithMany(e => e.Collections)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.User)
                    .WithMany(e => e.Collections)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CollectionDocument>(entity =>
            {
                entity.HasIndex(e => new { e.CollectionId, e.DocumentId }).IsUnique();
                
                entity.Property(e => e.AddedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Collection)
                    .WithMany(e => e.CollectionDocuments)
                    .HasForeignKey(e => e.CollectionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Document)
                    .WithMany(e => e.CollectionDocuments)
                    .HasForeignKey(e => e.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureDocumentChunk(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DocumentChunk>(entity =>
            {
                entity.HasIndex(e => new { e.DocumentId, e.Sequence }).IsUnique();
                entity.HasIndex(e => e.R2RChunkId);
                
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Document)
                    .WithMany(e => e.Chunks)
                    .HasForeignKey(e => e.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureAuditLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasIndex(e => new { e.CompanyId, e.CreatedAt });
                entity.HasIndex(e => new { e.EntityType, e.EntityId });
                entity.HasIndex(e => e.UserId);
                
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(e => e.Company)
                    .WithMany(e => e.AuditLogs)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.User)
                    .WithMany(e => e.AuditLogs)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private static void ConfigureAuth(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TokenBlacklist>(entity =>
            {
                entity.HasIndex(e => e.TokenHash).IsUnique();
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => e.UserId);

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => new { e.UserId, e.ExpiresAt });

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }

        private static void ConfigureConversation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.HasIndex(e => e.R2RConversationId).IsUnique();
                entity.HasIndex(e => new { e.CompanyId, e.UserId });
                entity.HasIndex(e => new { e.UserId, e.LastMessageAt });
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.Visibility);

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.LastMessageAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Configure JSON properties for PostgreSQL jsonb compatibility
                entity.Property(e => e.CollectionIds)
                    .HasColumnType("jsonb");

                entity.Property(e => e.Metadata)
                    .HasColumnType("TEXT");

                entity.Property(e => e.SharedUserIds)
                    .HasColumnType("TEXT");

                entity.Property(e => e.Tags)
                    .HasColumnType("TEXT");

                entity.Property(e => e.Settings)
                    .HasColumnType("jsonb");

                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureMessage(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasIndex(e => e.R2RMessageId);
                entity.HasIndex(e => new { e.ConversationId, e.CreatedAt });
                entity.HasIndex(e => e.ParentMessageId);
                entity.HasIndex(e => e.Role);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.IsEdited);
                entity.HasIndex(e => e.LastEditedAt);

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Configure JSON properties for cross-database compatibility
                entity.Property(e => e.Metadata)
                    .HasColumnType("TEXT");

                entity.Property(e => e.Citations)
                    .HasColumnType("TEXT");

                entity.Property(e => e.RagContext)
                    .HasColumnType("TEXT");

                entity.Property(e => e.EditHistory)
                    .HasColumnType("TEXT");

                entity.HasOne(e => e.Conversation)
                    .WithMany(e => e.Messages)
                    .HasForeignKey(e => e.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ParentMessage)
                    .WithMany(e => e.ChildMessages)
                    .HasForeignKey(e => e.ParentMessageId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.LastEditedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.LastEditedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
