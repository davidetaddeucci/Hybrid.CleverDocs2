using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Hybrid.CleverDocs2.WebServices.Data;
using Hybrid.CleverDocs2.WebServices.Data.Entities;
using Hybrid.CleverDocs2.WebServices.Services.Users;
using Hybrid.CleverDocs2.WebServices.Services.Auth;
using Hybrid.CleverDocs2.WebServices.Middleware;
using Hybrid.CleverDocs2.WebServices.Models.Common;
using Hybrid.CleverDocs2.WebServices.Hubs;
using Hybrid.CleverDocs2.WebServices.Extensions;
using Microsoft.AspNetCore.SignalR;

namespace Hybrid.CleverDocs2.WebServices.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;
        private readonly IUserSyncService _userSyncService;
        private readonly IHubContext<CollectionHub> _hubContext;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            ApplicationDbContext context,
            IAuthService authService,
            IUserSyncService userSyncService,
            IHubContext<CollectionHub> hubContext,
            ILogger<UsersController> logger)
        {
            _context = context;
            _authService = authService;
            _userSyncService = userSyncService;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Get all users (Admin can see all, Company can see their users)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<UserDto>>> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] Guid? companyId = null)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userCompanyId = User.FindFirst("CompanyId")?.Value;

                var query = _context.Users.Include(u => u.Company).AsQueryable();

                // Apply role-based filtering
                if (userRole == "Admin")
                {
                    // Admin can see all users, optionally filter by company
                    if (companyId.HasValue)
                    {
                        query = query.Where(u => u.CompanyId == companyId.Value);
                    }
                }
                else
                {
                    // Non-admin can only see users from their company
                    if (Guid.TryParse(userCompanyId, out var parsedCompanyId))
                    {
                        query = query.Where(u => u.CompanyId == parsedCompanyId);
                    }
                    else
                    {
                        return Forbid("Access denied");
                    }
                }

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(u => u.FirstName.Contains(search) || 
                                           u.LastName.Contains(search) ||
                                           u.Email.Contains(search));
                }

                if (isActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == isActive.Value);
                }

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var users = await query
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        Email = u.Email,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Name = u.Name,
                        Bio = u.Bio,
                        ProfilePicture = u.ProfilePicture,
                        Role = u.Role.ToString(),
                        CompanyId = u.CompanyId,
                        CompanyName = u.Company != null ? u.Company.Name : null,
                        IsActive = u.IsActive,
                        IsEmailVerified = u.IsEmailVerified,
                        IsVerified = u.IsVerified,
                        R2RUserId = u.R2RUserId,
                        CreatedAt = u.CreatedAt,
                        LastLoginAt = u.LastLoginAt
                    })
                    .ToListAsync();

                return Ok(PaginatedResponse<UserDto>.SuccessPaginatedResponse(
                    users, page, pageSize, totalCount, $"Retrieved {users.Count} users"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return BadRequest(PaginatedResponse<UserDto>.ErrorResponse("Failed to retrieve users"));
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(Guid id)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userCompanyId = User.FindFirst("CompanyId")?.Value;
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var user = await _context.Users
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound($"User with ID {id} not found");
                }

                // Check access permissions
                if (userRole != "Admin" && 
                    userCompanyId != user.CompanyId.ToString() && 
                    currentUserId != user.Id.ToString())
                {
                    return Forbid("Access denied to this user");
                }

                var userDto = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Name = user.Name,
                    Bio = user.Bio,
                    ProfilePicture = user.ProfilePicture,
                    Role = user.Role.ToString(),
                    CompanyId = user.CompanyId,
                    CompanyName = user.Company?.Name,
                    IsActive = user.IsActive,
                    IsEmailVerified = user.IsEmailVerified,
                    IsVerified = user.IsVerified,
                    R2RUserId = user.R2RUserId,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                };

                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Data = userDto,
                    Message = "User retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", id);
                return BadRequest(ApiResponse<UserDto>.ErrorResponse($"Failed to retrieve user {id}"));
            }
        }

        /// <summary>
        /// Create new user (Admin or Company role)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Company")]
        public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(CreateUserDto request)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userCompanyId = User.FindFirst("CompanyId")?.Value;

                // Validate company access
                if (userRole != "Admin" && userCompanyId != request.CompanyId.ToString())
                {
                    return Forbid("Cannot create user for different company");
                }

                // Verify company exists
                var company = await _context.Companies.FindAsync(request.CompanyId);
                if (company == null)
                {
                    return BadRequest($"Company with ID {request.CompanyId} not found");
                }

                // Check if email already exists
                if (!await _authService.IsEmailAvailableAsync(request.Email))
                {
                    return BadRequest($"Email '{request.Email}' is already in use");
                }

                // Create user using AuthService (basic overload)
                var user = await _authService.RegisterUserAsync(
                    request.Email,
                    request.Password,
                    request.FirstName,
                    request.LastName,
                    request.CompanyId,
                    Enum.Parse<UserRole>(request.Role),
                    User.FindFirst(ClaimTypes.Email)?.Value
                );

                // Update profile with R2R-compatible fields
                if (!string.IsNullOrEmpty(request.Name) || !string.IsNullOrEmpty(request.Bio) || !string.IsNullOrEmpty(request.ProfilePicture))
                {
                    await _authService.UpdateUserProfileAsync(
                        user.Id,
                        name: request.Name,
                        bio: request.Bio,
                        profilePicture: request.ProfilePicture
                    );
                }

                var userDto = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Name = user.Name,
                    Bio = user.Bio,
                    ProfilePicture = user.ProfilePicture,
                    Role = user.Role.ToString(),
                    CompanyId = user.CompanyId,
                    CompanyName = company.Name,
                    IsActive = user.IsActive,
                    IsEmailVerified = user.IsEmailVerified,
                    IsVerified = user.IsVerified,
                    R2RUserId = user.R2RUserId,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                };

                _logger.LogInformation("User created successfully: {UserId} - {Email}", user.Id, user.Email);

                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, 
                    new ApiResponse<UserDto>
                    {
                        Success = true,
                        Data = userDto,
                        Message = "User created successfully"
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return BadRequest(ApiResponse<UserDto>.ErrorResponse("Failed to create user"));
            }
        }

        /// <summary>
        /// Update user (Admin, Company, or user themselves)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(Guid id, UpdateUserDto request)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userCompanyId = User.FindFirst("CompanyId")?.Value;
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var user = await _context.Users
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound($"User with ID {id} not found");
                }

                // Check access permissions
                if (userRole != "Admin" &&
                    userCompanyId != user.CompanyId.ToString() &&
                    currentUserId != user.Id.ToString())
                {
                    return Forbid("Access denied to update this user");
                }

                // Update fields
                if (!string.IsNullOrEmpty(request.FirstName))
                    user.FirstName = request.FirstName;
                if (!string.IsNullOrEmpty(request.LastName))
                    user.LastName = request.LastName;
                if (request.Name != null)
                    user.Name = request.Name;
                if (request.Bio != null)
                    user.Bio = request.Bio;
                if (request.ProfilePicture != null)
                    user.ProfilePicture = request.ProfilePicture;
                if (request.IsActive.HasValue && (userRole == "Admin" || userRole == "Company"))
                    user.IsActive = request.IsActive.Value;

                // Only admin can change role
                if (!string.IsNullOrEmpty(request.Role) && userRole == "Admin")
                {
                    user.Role = Enum.Parse<UserRole>(request.Role);
                }

                await _context.SaveChangesAsync();

                // Update in R2R asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _userSyncService.UpdateR2RUserAsync(user);

                        // Notify via SignalR
                        await _hubContext.Clients.All.SendAsync("UserUpdated", new {
                            UserId = user.Id,
                            R2RUserId = user.R2RUserId,
                            Email = user.Email,
                            Name = user.Name
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to update R2R user for {UserId}", user.Id);
                    }
                });

                var userDto = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Name = user.Name,
                    Bio = user.Bio,
                    ProfilePicture = user.ProfilePicture,
                    Role = user.Role.ToString(),
                    CompanyId = user.CompanyId,
                    CompanyName = user.Company?.Name,
                    IsActive = user.IsActive,
                    IsEmailVerified = user.IsEmailVerified,
                    IsVerified = user.IsVerified,
                    R2RUserId = user.R2RUserId,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                };

                _logger.LogInformation("User updated successfully: {UserId} - {Email}", user.Id, user.Email);

                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Data = userDto,
                    Message = "User updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return BadRequest(ApiResponse<UserDto>.ErrorResponse($"Failed to update user {id}"));
            }
        }

        /// <summary>
        /// Deactivate user (Admin or Company role) - Soft delete
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Company")]
        public async Task<ActionResult<ApiResponse<object>>> DeactivateUser(Guid id)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userCompanyId = User.FindFirst("CompanyId")?.Value;

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound($"User with ID {id} not found");
                }

                // Check access permissions
                if (userRole != "Admin" && userCompanyId != user.CompanyId.ToString())
                {
                    return Forbid("Access denied to deactivate this user");
                }

                if (!user.IsActive)
                {
                    return BadRequest("User is already deactivated");
                }

                // Soft delete - deactivate user
                user.IsActive = false;
                await _context.SaveChangesAsync();

                // Note: We don't delete from R2R immediately, just deactivate
                // R2R cleanup can be done separately if needed

                // Notify via SignalR
                await _hubContext.Clients.All.SendAsync("UserDeactivated", new {
                    UserId = user.Id,
                    Email = user.Email
                });

                _logger.LogInformation("User deactivated successfully: {UserId} - {Email}", user.Id, user.Email);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new { UserId = user.Id, Email = user.Email },
                    Message = "User deactivated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {UserId}", id);
                return BadRequest(ApiResponse<object>.ErrorResponse($"Failed to deactivate user {id}"));
            }
        }
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Bio { get; set; }
        public string? ProfilePicture { get; set; }
        public string Role { get; set; } = string.Empty;
        public Guid CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public bool IsActive { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsVerified { get; set; }
        public string? R2RUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class CreateUserDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Bio { get; set; }
        public string? ProfilePicture { get; set; }
        public string Role { get; set; } = "User";
        public Guid CompanyId { get; set; }
    }

    public class UpdateUserDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Name { get; set; }
        public string? Bio { get; set; }
        public string? ProfilePicture { get; set; }
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
    }
}
