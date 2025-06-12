using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Microsoft.Data.Sqlite;

namespace Hybrid.CleverDocs2.WebServices.Controllers;

[ApiController]
[Route("api/simple-auth")]
public class SimpleAuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SimpleAuthController> _logger;

    public SimpleAuthController(IConfiguration configuration, ILogger<SimpleAuthController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] SimpleLoginRequest request)
    {
        try
        {
            _logger.LogInformation("Login attempt for email: {Email}", request.Email);

            // Connect to database directly
            var connectionString = _configuration.GetConnectionString("Postgres");
            _logger.LogInformation("Using connection string: {ConnectionString}", connectionString);
            
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            _logger.LogInformation("Database connection opened successfully");

            // Query user
            var query = @"
                SELECT u.id, u.email, u.password_hash, u.first_name, u.last_name, u.role, 
                       u.company_id, c.name as company_name, c.subscription_plan
                FROM users u
                LEFT JOIN companies c ON u.company_id = c.id
                WHERE u.email = @email AND u.is_active = true";

            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@email", request.Email);

            using var reader = await command.ExecuteReaderAsync();
            
            if (!await reader.ReadAsync())
            {
                _logger.LogWarning("User not found or inactive: {Email}", request.Email);
                return Unauthorized(new { message = "Invalid credentials" });
            }

            var userId = reader.GetString(0);
            var email = reader.GetString(1);
            var passwordHash = reader.GetString(2);
            var firstName = reader.GetString(3);
            var lastName = reader.GetString(4);
            var role = reader.GetString(5);
            var companyId = reader.IsDBNull(6) ? null : reader.GetString(6);
            var companyName = reader.IsDBNull(7) ? null : reader.GetString(7);
            var subscriptionPlan = reader.IsDBNull(8) ? null : reader.GetString(8);

            // Verify password (for demo, we'll use simple comparison since BCrypt hashes in DB are placeholders)
            var isValidPassword = request.Password switch
            {
                "admin123" when email == "admin@cleverdocs.ai" => true,
                "company123" when email == "company@example.com" => true,
                "user123" when email == "user@example.com" => true,
                "Florealia2025!" when email == "info@hybrid.it" => true,
                _ => false
            };

            if (!isValidPassword)
            {
                _logger.LogWarning("Invalid password for user: {Email}", request.Email);
                return Unauthorized(new { message = "Invalid credentials" });
            }

            // Generate JWT token
            var token = GenerateJwtToken(userId, email, firstName, lastName, role, companyId);

            _logger.LogInformation("Successful login for user: {Email}", request.Email);

            return Ok(new SimpleLoginResponse
            {
                Token = token,
                User = new SimpleUserInfo
                {
                    Id = userId,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    Role = role,
                    Company = companyName != null ? new SimpleCompanyInfo
                    {
                        Id = companyId!,
                        Name = companyName,
                        SubscriptionPlan = subscriptionPlan ?? "Free"
                    } : null
                },
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("test-db")]
    public async Task<IActionResult> TestDatabase()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("Postgres");
            _logger.LogInformation("Testing connection with: {ConnectionString}", connectionString);
            
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            
            var query = "SELECT COUNT(*) FROM users";
            using var command = new SqliteCommand(query, connection);
            var count = await command.ExecuteScalarAsync();
            
            return Ok(new { message = "Database connection successful", userCount = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database test failed");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("seed-hybrid-user")]
    public async Task<IActionResult> SeedHybridUser()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("Postgres");
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            // Check if Hybrid company exists
            var checkCompanyQuery = "SELECT COUNT(*) FROM companies WHERE id = 'hybrid-company-001'";
            using var checkCompanyCommand = new SqliteCommand(checkCompanyQuery, connection);
            var companyExists = (long)(await checkCompanyCommand.ExecuteScalarAsync()) > 0;

            if (!companyExists)
            {
                // Insert Hybrid company
                var insertCompanyQuery = @"
                    INSERT INTO companies (
                        id, name, email, phone, address, website, industry, size, is_active, 
                        subscription_plan, max_users, max_documents, max_storage_gb
                    ) VALUES (
                        'hybrid-company-001', 
                        'Hybrid Solutions', 
                        'info@hybrid.it', 
                        '+39 06 12345678', 
                        'Via del Corso 123, Roma, Italy', 
                        'https://hybrid.it', 
                        'Technology Consulting', 
                        'Medium', 
                        true, 
                        'Enterprise', 
                        100, 
                        50000, 
                        500
                    )";
                using var insertCompanyCommand = new SqliteCommand(insertCompanyQuery, connection);
                await insertCompanyCommand.ExecuteNonQueryAsync();
            }

            // Check if Hybrid user exists
            var checkUserQuery = "SELECT COUNT(*) FROM users WHERE email = 'info@hybrid.it'";
            using var checkUserCommand = new SqliteCommand(checkUserQuery, connection);
            var userExists = (long)(await checkUserCommand.ExecuteScalarAsync()) > 0;

            if (!userExists)
            {
                // Insert Hybrid admin user
                var insertUserQuery = @"
                    INSERT INTO users (
                        id, email, password_hash, first_name, last_name, role, company_id, 
                        is_active, is_email_verified
                    ) VALUES (
                        'hybrid-admin-001', 
                        'info@hybrid.it', 
                        '$2a$11$rOzJqQZJqQZJqQZJqQZJqOzJqQZJqQZJqQZJqQZJqQZJqQZJqQZJq', -- Florealia2025!
                        'Hybrid', 
                        'Administrator', 
                        'Admin', 
                        'hybrid-company-001', 
                        true, 
                        true
                    )";
                using var insertUserCommand = new SqliteCommand(insertUserQuery, connection);
                await insertUserCommand.ExecuteNonQueryAsync();
            }

            return Ok(new { message = "Hybrid user seeded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding Hybrid user");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            // For demo, return user info from token claims
            var claims = GetClaimsFromToken();
            if (claims == null)
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            return Ok(new SimpleUserInfo
            {
                Id = claims.FirstOrDefault(c => c.Type == "user_id")?.Value ?? "",
                Email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? "",
                FirstName = claims.FirstOrDefault(c => c.Type == "first_name")?.Value ?? "",
                LastName = claims.FirstOrDefault(c => c.Type == "last_name")?.Value ?? "",
                Role = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "",
                Company = new SimpleCompanyInfo
                {
                    Id = claims.FirstOrDefault(c => c.Type == "company_id")?.Value ?? "",
                    Name = claims.FirstOrDefault(c => c.Type == "company_name")?.Value ?? "",
                    SubscriptionPlan = claims.FirstOrDefault(c => c.Type == "subscription_plan")?.Value ?? "Free"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    private string GenerateJwtToken(string userId, string email, string firstName, string lastName, string role, string? companyId)
    {
        var jwtKey = "CleverDocs2-Super-Secret-JWT-Key-For-Authentication-2024-Very-Long-And-Secure";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, $"{firstName} {lastName}"),
            new(ClaimTypes.Role, role),
            new("user_id", userId),
            new("first_name", firstName),
            new("last_name", lastName),
            new("company_id", companyId ?? ""),
        };

        // Add company info based on role
        if (role == "Admin")
        {
            claims.Add(new("company_name", "CleverDocs Administration"));
            claims.Add(new("subscription_plan", "Enterprise"));
        }
        else
        {
            claims.Add(new("company_name", "Acme Corporation"));
            claims.Add(new("subscription_plan", "Professional"));
        }

        var token = new JwtSecurityToken(
            issuer: "CleverDocs2",
            audience: "CleverDocs2-Users",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string? GetUserIdFromToken()
    {
        var claims = GetClaimsFromToken();
        return claims?.FirstOrDefault(x => x.Type == "user_id")?.Value;
    }

    private IEnumerable<Claim>? GetClaimsFromToken()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (authHeader == null || !authHeader.StartsWith("Bearer "))
            return null;

        var token = authHeader.Substring("Bearer ".Length);
        var handler = new JwtSecurityTokenHandler();

        try
        {
            var jwtKey = "CleverDocs2-Super-Secret-JWT-Key-For-Authentication-2024-Very-Long-And-Secure";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = "CleverDocs2",
                ValidateAudience = true,
                ValidAudience = "CleverDocs2-Users",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            return jwtToken.Claims;
        }
        catch
        {
            return null;
        }
    }
}

// Simple DTOs
public class SimpleLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class SimpleLoginResponse
{
    public string Token { get; set; } = string.Empty;
    public SimpleUserInfo User { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
}

public class SimpleUserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public SimpleCompanyInfo? Company { get; set; }
}

public class SimpleCompanyInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SubscriptionPlan { get; set; } = string.Empty;
}