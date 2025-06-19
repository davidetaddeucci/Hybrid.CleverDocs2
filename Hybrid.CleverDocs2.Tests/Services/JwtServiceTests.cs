using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Data.Entities;
using Hybrid.CleverDocs2.WebServices.Services.Auth;
using Hybrid.CleverDocs2.WebServices.Models.Auth;

namespace Hybrid.CleverDocs2.Tests.Services
{
    public class JwtServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<JwtService>> _mockLogger;
        private readonly JwtService _jwtService;

        public JwtServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            // Setup mocks
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<JwtService>>();

            // Setup JWT configuration
            var jwtSection = new Mock<IConfigurationSection>();
            jwtSection.Setup(x => x["SecretKey"]).Returns("ThisIsAVeryLongSecretKeyForTestingPurposesOnly123456789");
            jwtSection.Setup(x => x["Issuer"]).Returns("TestIssuer");
            jwtSection.Setup(x => x["Audience"]).Returns("TestAudience");
            jwtSection.Setup(x => x["ExpirationMinutes"]).Returns("60");
            jwtSection.Setup(x => x["RefreshTokenExpirationDays"]).Returns("30");
            _mockConfiguration.Setup(x => x.GetSection("Jwt")).Returns(jwtSection.Object);

            _jwtService = new JwtService(
                _mockConfiguration.Object,
                _context,
                _mockLogger.Object
            );
        }

        [Fact]
        public void GenerateAccessToken_WithValidUser_ShouldReturnToken()
        {
            // Arrange
            var company = new Company
            {
                Id = Guid.NewGuid(),
                Name = "Test Company",
                IsActive = true
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                CompanyId = company.Id,
                Company = company,
                Role = UserRole.User,
                IsActive = true,
                IsVerified = true
            };

            // Act
            var token = _jwtService.GenerateAccessToken(user);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            Assert.Contains(".", token); // JWT tokens contain dots
        }

        [Fact]
        public void GenerateRefreshToken_ShouldReturnUniqueTokens()
        {
            // Act
            var token1 = _jwtService.GenerateRefreshToken();
            var token2 = _jwtService.GenerateRefreshToken();

            // Assert
            Assert.NotNull(token1);
            Assert.NotNull(token2);
            Assert.NotEmpty(token1);
            Assert.NotEmpty(token2);
            Assert.NotEqual(token1, token2);
        }

        [Fact]
        public async Task SaveRefreshTokenAsync_ShouldSaveTokenToDatabase()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var token = "test-refresh-token";
            var expiration = TimeSpan.FromDays(30);

            // Act
            await _jwtService.SaveRefreshTokenAsync(userId, token, expiration);

            // Assert
            var savedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token && rt.UserId == userId);

            Assert.NotNull(savedToken);
            Assert.Equal(userId, savedToken.UserId);
            Assert.Equal(token, savedToken.Token);
            Assert.True(savedToken.IsActive);
            Assert.True(savedToken.ExpiresAt > DateTime.UtcNow.AddDays(29));
        }

        [Fact]
        public async Task GetRefreshTokenAsync_WithValidUserId_ShouldReturnToken()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var token = "test-refresh-token";

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = token,
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow
            };

            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            // Act
            var result = await _jwtService.GetRefreshTokenAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(token, result);
        }

        [Fact]
        public async Task GetRefreshTokenAsync_WithInvalidUserId_ShouldReturnNull()
        {
            // Arrange
            var invalidUserId = Guid.NewGuid();

            // Act
            var result = await _jwtService.GetRefreshTokenAsync(invalidUserId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RevokeRefreshTokenAsync_ShouldDeactivateUserTokens()
        {
            // Arrange
            var userId = Guid.NewGuid();
            
            var refreshToken1 = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "token1",
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow
            };

            var refreshToken2 = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "token2",
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow
            };
            
            await _context.RefreshTokens.AddRangeAsync(refreshToken1, refreshToken2);
            await _context.SaveChangesAsync();

            // Act
            await _jwtService.RevokeRefreshTokenAsync(userId);

            // Assert
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId)
                .ToListAsync();

            Assert.All(tokens, token => Assert.False(token.IsActive));
        }

        [Fact]
        public async Task BlacklistTokenAsync_ShouldAddTokenToBlacklist()
        {
            // Arrange
            var token = "test-access-token";

            // Act
            await _jwtService.BlacklistTokenAsync(token);

            // Assert
            var blacklistedToken = await _context.TokenBlacklists
                .FirstOrDefaultAsync(bt => bt.TokenHash != null);

            Assert.NotNull(blacklistedToken);
            Assert.NotNull(blacklistedToken.TokenHash);
            Assert.True(blacklistedToken.CreatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public async Task IsTokenBlacklistedAsync_WithBlacklistedToken_ShouldReturnTrue()
        {
            // Arrange
            var token = "blacklisted-token";

            var blacklistedToken = new TokenBlacklist
            {
                Id = Guid.NewGuid(),
                TokenHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(token)),
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                CreatedAt = DateTime.UtcNow
            };

            await _context.TokenBlacklists.AddAsync(blacklistedToken);
            await _context.SaveChangesAsync();

            // Act
            var result = await _jwtService.IsTokenBlacklistedAsync(token);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsTokenBlacklistedAsync_WithValidToken_ShouldReturnFalse()
        {
            // Act
            var result = await _jwtService.IsTokenBlacklistedAsync("valid-token");

            // Assert
            Assert.False(result);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
