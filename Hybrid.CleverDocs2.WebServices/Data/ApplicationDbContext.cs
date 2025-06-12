using Microsoft.EntityFrameworkCore;
using Hybrid.CleverDocs2.WebServices.Data.Entities;

namespace Hybrid.CleverDocs2.WebServices.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<IngestionJob> IngestionJobs { get; set; } = null!;
        public DbSet<DocumentChunk> DocumentChunks { get; set; } = null!;
    }
}
