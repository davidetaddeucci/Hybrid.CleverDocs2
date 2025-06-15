# Architectural Patterns Documentation

## Overview

This document describes the standardized architectural patterns implemented in Hybrid.CleverDocs2 WebServices. These patterns ensure consistency, maintainability, and enterprise-grade quality across the entire codebase.

## 1. Response Pattern

### Standardized API Responses

All API endpoints must use the standardized `ApiResponse<T>` wrapper:

```csharp
// Success response with data
return this.Success(data, "Operation completed successfully");

// Error response
return this.Error("Error message", new List<string> { "Detailed error" }, 400);

// Not found response
return this.NotFound<MyModel>("Resource not found");

// Paginated response
return this.Paginated(items, page, pageSize, totalItems);
```

### Benefits
- Consistent response format across all endpoints
- Automatic correlation ID inclusion
- Standardized error handling
- Built-in metadata support

## 2. Correlation and Logging Pattern

### Correlation Service

Every request gets a unique correlation ID for distributed tracing:

```csharp
public class MyController : ControllerBase
{
    private readonly ICorrelationService _correlationService;
    
    public IActionResult MyAction()
    {
        var correlationId = _correlationService.GetCorrelationId();
        _logger.LogInformationWithContext("Action executed");
        return this.Success(data);
    }
}
```

### Structured Logging

Use context-aware logging methods:

```csharp
_logger.LogInformationWithContext("User {UserId} performed action", userId);
_logger.LogWarningWithContext("Warning occurred in {Method}", nameof(MyMethod));
_logger.LogErrorWithContext(exception, "Error in {Operation}", "UserCreation");
```

### Benefits
- Automatic correlation ID propagation
- User and tenant context in logs
- Structured logging with metadata
- Request/response tracing

## 3. Exception Handling Pattern

### Custom Exceptions

Use specific exception types for different scenarios:

```csharp
// Business logic errors
throw new BusinessException("Invalid operation", "INVALID_OP", details);

// Validation errors
throw new ValidationException("field", "error message");

// Resource not found
throw new NotFoundException("User", userId);

// Access denied
throw new ForbiddenException("Resource", "Action");

// External service errors
throw new ExternalServiceException("R2R", "Connection failed", 502);
```

### Global Exception Handling

All exceptions are automatically caught and converted to standardized responses:

- Development: Full error details
- Production: Sanitized error messages
- Automatic logging with correlation context
- Proper HTTP status codes

### Benefits
- Consistent error responses
- Security-aware error messages
- Automatic logging and correlation
- Type-safe exception handling

## 4. Controller Extensions Pattern

### Authentication and Authorization Helpers

```csharp
public class MyController : ControllerBase
{
    public IActionResult MyAction(Guid companyId, Guid userId)
    {
        // Validate company access
        var accessError = this.ValidateCompanyAccess(companyId);
        if (accessError != null) return accessError;
        
        // Validate resource ownership
        var ownershipError = this.ValidateResourceOwnership(userId);
        if (ownershipError != null) return ownershipError;
        
        // Get current user info
        var currentUserId = this.GetCurrentUserId();
        var isAdmin = this.IsAdmin();
        
        return this.Success(data);
    }
}
```

### Tenant Context Helpers

```csharp
// Add tenant metadata to responses
var metadata = this.AddTenantMetadata();
return this.Success(data, "Success", metadata);

// Check user roles
if (this.IsCompanyAdmin())
{
    // Company admin logic
}
```

### Benefits
- Simplified authentication checks
- Consistent authorization patterns
- Automatic tenant context handling
- Reduced boilerplate code

## 5. Middleware Pipeline Pattern

### Middleware Order

The middleware pipeline is configured in the correct order:

1. **CorrelationMiddleware**: Sets up correlation context
2. **GlobalExceptionMiddleware**: Handles all exceptions
3. **JwtMiddleware**: JWT authentication
4. **TenantResolutionMiddleware**: Tenant context resolution

### Request Flow

```
Request → Correlation → Exception Handling → JWT → Tenant → Controller
```

### Benefits
- Proper request context setup
- Consistent error handling
- Automatic correlation propagation
- Tenant-aware processing

## 6. Validation Pattern

### FluentValidation Integration

```csharp
public class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(8);
        RuleForTenantId(); // Custom rule for tenant validation
    }
}
```

### Automatic Validation

Validation errors are automatically converted to standardized responses:

```json
{
  "success": false,
  "message": "Validation failed",
  "errors": ["Email: Email is required", "Password: Password too short"],
  "statusCode": 422,
  "metadata": {
    "validationErrors": {
      "Email": ["Email is required"],
      "Password": ["Password too short"]
    }
  }
}
```

## 7. Usage Guidelines

### Controller Implementation

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MyController : ControllerBase
{
    private readonly ILogger<MyController> _logger;
    private readonly ICorrelationService _correlationService;
    
    public MyController(ILogger<MyController> logger, ICorrelationService correlationService)
    {
        _logger = logger;
        _correlationService = correlationService;
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        _logger.LogInformationWithContext("Getting resource {Id}", id);
        
        try
        {
            var resource = await _service.GetAsync(id);
            if (resource == null)
            {
                throw new NotFoundException("Resource", id);
            }
            
            var metadata = this.AddTenantMetadata();
            return this.Success(resource, "Resource retrieved successfully", metadata);
        }
        catch (Exception ex)
        {
            _logger.LogErrorWithContext(ex, "Error getting resource {Id}", id);
            throw; // Let global exception handler deal with it
        }
    }
}
```

### Service Implementation

```csharp
public class MyService
{
    private readonly ILogger<MyService> _logger;
    private readonly ICorrelationService _correlationService;
    
    public async Task<MyModel> ProcessAsync(MyRequest request)
    {
        var correlationId = _correlationService.GetCorrelationId();
        _logger.LogInformationWithContext("Processing request with correlation {CorrelationId}", correlationId);
        
        // Business logic here
        
        return result;
    }
}
```

## 8. Testing Patterns

### Unit Tests

```csharp
public class MyControllerTests : BaseUnitTest
{
    [Fact]
    public async Task Get_ValidId_ReturnsSuccess()
    {
        // Arrange
        var controller = CreateController();
        var id = Guid.NewGuid();
        
        // Act
        var result = await controller.Get(id);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<MyModel>>(okResult.Value);
        Assert.True(response.Success);
    }
}
```

### Integration Tests

```csharp
public class MyControllerIntegrationTests : BaseIntegrationTest
{
    [Fact]
    public async Task Get_ValidId_ReturnsCorrectResponse()
    {
        // Arrange
        await SeedTestData();
        var id = TestData.ValidId;
        
        // Act
        var response = await AuthenticatedRequest<ApiResponse<MyModel>>($"/api/my/{id}");
        
        // Assert
        Assert.True(response.Success);
        Assert.NotNull(response.TraceId);
    }
}
```

## 9. Performance Considerations

- Correlation service uses AsyncLocal for thread-safe context
- Middleware overhead is minimal (<5ms per request)
- Structured logging is optimized for performance
- Exception handling includes performance metrics

## 10. Security Considerations

- Error messages are sanitized in production
- Correlation IDs don't expose sensitive information
- User context is properly isolated per request
- Tenant data isolation is enforced at middleware level

## 11. Monitoring and Observability

- All requests include correlation IDs
- Structured logs include user and tenant context
- Performance metrics are automatically collected
- Exception patterns are tracked for alerting
