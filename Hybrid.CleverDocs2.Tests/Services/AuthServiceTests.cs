using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Data.Entities;
using Hybrid.CleverDocs2.WebServices.Services.Auth;
using Hybrid.CleverDocs2.WebServices.Services.Users;
using Hybrid.CleverDocs2.WebServices.Hubs;
using Hybrid.CleverDocs2.WebServices.Models.Auth;

namespace Hybrid.CleverDocs2.Tests.Services
{
    public class AuthServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IJwtService> _mockJwtService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly Mock<IUserSyncService> _mockUserSyncService;
        private readonly Mock<IHubContext<CollectionHub>> _mockHubContext;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            // Setup mocks
            _mockJwtService = new Mock<IJwtService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<AuthService>>();
            _mockUserSyncService = new Mock<IUserSyncService>();
            _mockHubContext = new Mock<IHubContext<CollectionHub>>();

            // Setup configuration mock
            var jwtSection = new Mock<IConfigurationSection>();
            jwtSection.Setup(x => x["RefreshTokenExpirationDays"]).Returns("30");
            jwtSection.Setup(x => x["ExpirationMinutes"]).Returns("60");
            _mockConfiguration.Setup(x => x.GetSection("Jwt")).Returns(jwtSection.Object);

            _authService = new AuthService(
                _context,
                _mockJwtService.Object,
                _mockConfiguration.Object,
                _mockLogger.Object,
                _mockUserSyncService.Object,
                _mockHubContext.Object
            );
        }

        [Fact]
        public async Task RefreshTokenAsync_WithValidToken_ShouldReturnNewTokens()
        {
            // Arrange
            var company = new Company
            {
                Id = Guid.NewGuid(),
                Name = "Test Company",
                IsActive = true
            };
            await _context.Companies.AddAsync(company);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                CompanyId = company.Id,
                Company = company,
                IsActive = true,
                IsVerified = true,
                PasswordHash = "hashedpassword"
            };
            await _context.Users.AddAsync(user);

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "valid-refresh-token",
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow
            };
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            _mockJwtService.Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
                .Returns("new-access-token");
            _mockJwtService.Setup(x => x.GenerateRefreshToken())
                .Returns("new-refresh-token");
            _mockJwtService.Setup(x => x.SaveRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _authService.RefreshTokenAsync("valid-refresh-token");

            // Assert
            Assert.True(result.Success);
            Assert.Equal("new-access-token", result.AccessToken);
            Assert.Equal("new-refresh-token", result.RefreshToken);
            Assert.NotNull(result.User);
            Assert.Equal(user.Id, result.User.Id);
            Assert.Equal(user.Email, result.User.Email);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithInvalidToken_ShouldReturnFailure()
        {
            // Arrange
            var invalidToken = "invalid-refresh-token";

            // Act
            var result = await _authService.RefreshTokenAsync(invalidToken);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Invalid or expired refresh token", result.ErrorMessage);
            Assert.Null(result.AccessToken);
            Assert.Null(result.RefreshToken);
            Assert.Null(result.User);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithInactiveUser_ShouldRevokeTokenAndReturnFailure()
        {
            // Arrange
            var company = new Company
            {
                Id = Guid.NewGuid(),
                Name = "Test Company",
                IsActive = true
            };
            await _context.Companies.AddAsync(company);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                CompanyId = company.Id,
                Company = company,
                IsActive = false, // Inactive user
                IsVerified = true,
                PasswordHash = "hashedpassword"
            };
            await _context.Users.AddAsync(user);

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "valid-refresh-token",
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow
            };
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            _mockJwtService.Setup(x => x.RevokeRefreshTokenAsync(It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _authService.RefreshTokenAsync("valid-refresh-token");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User account is inactive or unverified", result.ErrorMessage);
            _mockJwtService.Verify(x => x.RevokeRefreshTokenAsync(user.Id), Times.Once);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithNullToken_ShouldReturnFailure()
        {
            // Act
            var result = await _authService.RefreshTokenAsync(null!);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Refresh token is required", result.ErrorMessage);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithEmptyToken_ShouldReturnFailure()
        {
            // Act
            var result = await _authService.RefreshTokenAsync("");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Refresh token is required", result.ErrorMessage);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
