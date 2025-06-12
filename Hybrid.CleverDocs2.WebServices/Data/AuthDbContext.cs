using Microsoft.EntityFrameworkCore;
using Hybrid.CleverDocs2.WebServices.Data.Models.Auth;

namespace Hybrid.CleverDocs2.WebServices.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.CompanyId);
            entity.HasIndex(e => e.Role);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.Email).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FirstName).IsRequired();
            entity.Property(e => e.LastName).IsRequired();
            entity.Property(e => e.Role).IsRequired().HasDefaultValue("User");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsEmailVerified).HasDefaultValue(false);
            entity.Property(e => e.LoginCount).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Relationships
            entity.HasOne(e => e.Company)
                  .WithMany(c => c.Users)
                  .HasForeignKey(e => e.CompanyId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.RefreshTokens)
                  .WithOne(rt => rt.User)
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.UserSessions)
                  .WithOne(us => us.User)
                  .HasForeignKey(us => us.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Company configuration
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.SubscriptionPlan);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Email).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SubscriptionPlan).HasDefaultValue("Free");
            entity.Property(e => e.MaxUsers).HasDefaultValue(10);
            entity.Property(e => e.MaxDocuments).HasDefaultValue(1000);
            entity.Property(e => e.MaxStorageGb).HasDefaultValue(5);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // RefreshToken configuration
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Expires);
            entity.HasIndex(e => e.Created);

            entity.Property(e => e.Token).IsRequired();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Created).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // UserSession configuration
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasIndex(e => e.SessionToken).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.LastActivity);
            entity.HasIndex(e => e.ExpiresAt);

            entity.Property(e => e.SessionToken).IsRequired();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastActivity).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed default admin company
        var adminCompanyId = "admin-company-001";
        modelBuilder.Entity<Company>().HasData(new Company
        {
            Id = adminCompanyId,
            Name = "CleverDocs Administration",
            Email = "admin@cleverdocs.ai",
            IsActive = true,
            SubscriptionPlan = "Enterprise",
            MaxUsers = 1000,
            MaxDocuments = 100000,
            MaxStorageGb = 1000,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // Seed demo company
        var demoCompanyId = "demo-company-001";
        modelBuilder.Entity<Company>().HasData(new Company
        {
            Id = demoCompanyId,
            Name = "Acme Corporation",
            Email = "admin@acme.com",
            Phone = "+39 02 1234567",
            Address = "Via Roma 123, Milano, Italy",
            Website = "https://acme.com",
            Industry = "Technology",
            Size = "Medium",
            IsActive = true,
            SubscriptionPlan = "Professional",
            MaxUsers = 50,
            MaxDocuments = 10000,
            MaxStorageGb = 100,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // Seed admin user
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = "admin-user-001",
            Email = "admin@cleverdocs.ai",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            FirstName = "System",
            LastName = "Administrator",
            Role = "Admin",
            CompanyId = adminCompanyId,
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // Seed company admin user
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = "company-user-001",
            Email = "company@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("company123"),
            FirstName = "Company",
            LastName = "Manager",
            Role = "Company",
            CompanyId = demoCompanyId,
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // Seed regular user
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = "regular-user-001",
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123"),
            FirstName = "Mario",
            LastName = "Rossi",
            Role = "User",
            CompanyId = demoCompanyId,
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is User || e.Entity is Company)
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is User user)
            {
                user.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Company company)
            {
                company.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}