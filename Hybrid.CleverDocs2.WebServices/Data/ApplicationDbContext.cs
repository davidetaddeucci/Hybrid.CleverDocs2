using Microsoft.EntityFrameworkCore;
using Hybrid.CleverDocs2.WebServices.Data.Entities;

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

        // Dashboard widgets
        public DbSet<UserDashboardWidget> UserDashboardWidgets { get; set; } = null!;
        public DbSet<WidgetTemplate> WidgetTemplates { get; set; } = null!;

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
    }
}
