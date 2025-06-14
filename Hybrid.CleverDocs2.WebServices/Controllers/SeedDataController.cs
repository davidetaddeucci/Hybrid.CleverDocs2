using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Data.Entities;
using Hybrid.CleverDocs2.WebServices.Services.Auth;

namespace Hybrid.CleverDocs2.WebServices.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeedDataController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;
        private readonly ILogger<SeedDataController> _logger;

        public SeedDataController(
            ApplicationDbContext context,
            IAuthService authService,
            ILogger<SeedDataController> logger)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("create-test-data")]
        public async Task<IActionResult> CreateTestData()
        {
            try
            {
                // 1. Crea Company Hybrid IT (Admin di sistema)
                var hybridCompany = await CreateCompanyIfNotExists(
                    "Hybrid IT",
                    "info@hybrid.it",
                    "+39 123 456 7890",
                    "https://hybrid.it",
                    "Via Roma 1, Milano, Italy",
                    "Hybrid IT - System Administration Company"
                );

                // 2. Crea Company Microsis srl
                var microsisCompany = await CreateCompanyIfNotExists(
                    "Microsis srl",
                    "info@microsis.it",
                    "+39 987 654 3210",
                    "https://microsis.it",
                    "Via Firenze 10, Roma, Italy",
                    "Microsis srl - Technology Solutions"
                );

                // 3. Crea Admin di sistema (Hybrid IT)
                await CreateUserIfNotExists(
                    "info@hybrid.it",
                    "Florealia2025!",
                    "Admin",
                    "System",
                    hybridCompany.Id,
                    UserRole.Admin
                );

                // 4. Crea Company Admin (Microsis)
                await CreateUserIfNotExists(
                    "info@microsis.it",
                    "Maremmabona1!",
                    "Admin",
                    "Microsis",
                    microsisCompany.Id,
                    UserRole.Company
                );

                // 5. Crea User 1 (Microsis)
                await CreateUserIfNotExists(
                    "r.antoniucci@microsis.it",
                    "Maremmabona1!",
                    "Roberto",
                    "Antoniucci",
                    microsisCompany.Id,
                    UserRole.User
                );

                // 6. Crea User 2 (Microsis)
                await CreateUserIfNotExists(
                    "m.bevilacqua@microsis.it",
                    "Maremmabona1!",
                    "Marco",
                    "Bevilacqua",
                    microsisCompany.Id,
                    UserRole.User
                );

                return Ok(new
                {
                    Success = true,
                    Message = "Test data created successfully",
                    Companies = new[]
                    {
                        new { Name = hybridCompany.Name, Id = hybridCompany.Id },
                        new { Name = microsisCompany.Name, Id = microsisCompany.Id }
                    },
                    Users = new[]
                    {
                        "info@hybrid.it (Admin)",
                        "info@microsis.it (Company Admin)",
                        "r.antoniucci@microsis.it (User)",
                        "m.bevilacqua@microsis.it (User)"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating test data");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error creating test data",
                    Error = ex.Message
                });
            }
        }

        private async Task<Company> CreateCompanyIfNotExists(
            string name, string contactEmail, string contactPhone, 
            string website, string address, string description)
        {
            var existingCompany = await _context.Companies
                .FirstOrDefaultAsync(c => c.ContactEmail == contactEmail);

            if (existingCompany != null)
            {
                _logger.LogInformation("Company {Name} already exists", name);
                return existingCompany;
            }

            var company = new Company
            {
                Id = Guid.NewGuid(),
                Name = name,
                ContactEmail = contactEmail,
                ContactPhone = contactPhone,
                Website = website,
                Address = address,
                Description = description,
                IsActive = true,
                MaxUsers = 50,
                MaxDocuments = 10000,
                MaxCollections = 500,
                MaxStorageBytes = 10737418240, // 10GB
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "SeedData"
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created company: {Name} with ID: {Id}", name, company.Id);
            return company;
        }

        private async Task CreateUserIfNotExists(
            string email, string password, string firstName, string lastName,
            Guid companyId, UserRole role)
        {
            var existingUser = await _authService.GetUserByEmailAsync(email);
            if (existingUser != null)
            {
                _logger.LogInformation("User {Email} already exists", email);
                return;
            }

            var user = await _authService.RegisterUserAsync(
                email, password, firstName, lastName, companyId, role, "SeedData");

            // Mark email as verified for test users
            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = "SeedData";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Created user: {Email} with role: {Role}", email, role);
        }

        [HttpGet("test-passwords")]
        public async Task<IActionResult> TestPasswords()
        {
            var passwords = new[]
            {
                "Florealia2025!",
                "Maremmabona1!"
            };

            var results = new List<object>();

            foreach (var password in passwords)
            {
                var hash = await _authService.HashPasswordAsync(password);
                results.Add(new
                {
                    Password = password,
                    Hash = hash,
                    Length = hash.Length
                });
            }

            return Ok(results);
        }
    }
}
