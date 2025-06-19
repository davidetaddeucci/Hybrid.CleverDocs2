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

namespace Hybrid.CleverDocs2.Tests.Services
{
    public class AuthServiceUserMethodsTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IJwtService> _mockJwtService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly Mock<IUserSyncService> _mockUserSyncService;
        private readonly Mock<IHubContext<CollectionHub>> _mockHubContext;
        private readonly AuthService _authService;

        public AuthServiceUserMethodsTests()
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
        public async Task GetUserByIdAsync_WithValidId_ShouldReturnUser()
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
            await _context.SaveChangesAsync();

            // Act
            var result = await _authService.GetUserByIdAsync(user.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal(user.FirstName, result.FirstName);
            Assert.Equal(user.LastName, result.LastName);
            Assert.Equal(company.Name, result.Company.Name);
        }

        [Fact]
        public async Task GetUserByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var result = await _authService.GetUserByIdAsync(invalidId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserByEmailAsync_WithValidEmail_ShouldReturnUser()
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
            await _context.SaveChangesAsync();

            // Act
            var result = await _authService.GetUserByEmailAsync("test@example.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal(user.FirstName, result.FirstName);
            Assert.Equal(user.LastName, result.LastName);
        }

        [Fact]
        public async Task GetUserByEmailAsync_WithInvalidEmail_ShouldReturnNull()
        {
            // Act
            var result = await _authService.GetUserByEmailAsync("nonexistent@example.com");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task IsEmailAvailableAsync_WithAvailableEmail_ShouldReturnTrue()
        {
            // Act
            var result = await _authService.IsEmailAvailableAsync("available@example.com");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsEmailAvailableAsync_WithTakenEmail_ShouldReturnFalse()
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
                Email = "taken@example.com",
                FirstName = "Test",
                LastName = "User",
                CompanyId = company.Id,
                IsActive = true,
                PasswordHash = "hashedpassword"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _authService.IsEmailAvailableAsync("taken@example.com");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidatePasswordAsync_WithCorrectPassword_ShouldReturnTrue()
        {
            // Arrange
            var company = new Company
            {
                Id = Guid.NewGuid(),
                Name = "Test Company",
                IsActive = true
            };
            await _context.Companies.AddAsync(company);

            var password = "TestPassword123!";
            var hashedPassword = await _authService.HashPasswordAsync(password);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                CompanyId = company.Id,
                IsActive = true,
                PasswordHash = hashedPassword
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _authService.ValidatePasswordAsync(user, password);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ValidatePasswordAsync_WithIncorrectPassword_ShouldReturnFalse()
        {
            // Arrange
            var company = new Company
            {
                Id = Guid.NewGuid(),
                Name = "Test Company",
                IsActive = true
            };
            await _context.Companies.AddAsync(company);

            var correctPassword = "TestPassword123!";
            var incorrectPassword = "WrongPassword123!";
            var hashedPassword = await _authService.HashPasswordAsync(correctPassword);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                CompanyId = company.Id,
                IsActive = true,
                PasswordHash = hashedPassword
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _authService.ValidatePasswordAsync(user, incorrectPassword);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HashPasswordAsync_ShouldReturnHashedPassword()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var hashedPassword = await _authService.HashPasswordAsync(password);

            // Assert
            Assert.NotNull(hashedPassword);
            Assert.NotEmpty(hashedPassword);
            Assert.NotEqual(password, hashedPassword);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
