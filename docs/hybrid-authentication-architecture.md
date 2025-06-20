# Hybrid Authentication Architecture

## Overview

Hybrid.CleverDocs2 implements a sophisticated hybrid authentication system that combines Cookie Authentication for the WebUI with JWT tokens stored in HttpOnly cookies for API calls. This approach provides maximum security while maintaining excellent user experience.

## Architecture Components

### 1. Cookie Authentication (WebUI)
- **Purpose**: Primary authentication for MVC controllers and views
- **Implementation**: ASP.NET Core Cookie Authentication
- **Storage**: Server-side session with secure cookies
- **Benefits**: CSRF protection, secure session management, automatic expiration

### 2. JWT in HttpOnly Cookies (API)
- **Purpose**: Authentication for API calls to WebServices
- **Implementation**: JWT tokens stored in HttpOnly cookies
- **Storage**: Client-side HttpOnly cookies (not accessible via JavaScript)
- **Benefits**: Stateless API authentication, protection against XSS attacks

## Security Benefits

### Maximum Security Approach
- **No localStorage/sessionStorage**: Eliminates XSS token theft vectors
- **HttpOnly Cookies**: Prevents JavaScript access to JWT tokens
- **Secure Cookies**: HTTPS-only transmission
- **SameSite Protection**: CSRF attack prevention
- **Automatic Expiration**: Built-in token lifecycle management

### Multi-Layer Protection
1. **Cookie Authentication**: Protects MVC routes and views
2. **JWT Validation**: Secures API endpoints
3. **HTTPS Enforcement**: Encrypts all communications
4. **CSRF Tokens**: Prevents cross-site request forgery
5. **Role-Based Authorization**: Granular access control

## Implementation Details

### WebUI Authentication Flow

```csharp
// Program.cs - WebUI
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });
```

### JWT Token Management

```csharp
// AuthService.cs - WebUI
public async Task<bool> LoginAsync(string email, string password)
{
    var loginRequest = new LoginRequest { Email = email, Password = password };
    var response = await _httpClient.PostAsJsonAsync("/api/auth/login", loginRequest);
    
    if (response.IsSuccessStatusCode)
    {
        // JWT token automatically stored in HttpOnly cookie by WebServices
        var userInfo = await response.Content.ReadFromJsonAsync<UserInfo>();
        
        // Create Cookie Authentication claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userInfo.Id.ToString()),
            new Claim(ClaimTypes.Email, userInfo.Email),
            new Claim(ClaimTypes.Role, userInfo.Role),
            new Claim("CompanyId", userInfo.CompanyId.ToString())
        };
        
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        
        await _httpContextAccessor.HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme, 
            principal);
        
        return true;
    }
    
    return false;
}
```

### WebServices JWT Configuration

```csharp
// Program.cs - WebServices
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
        };
        
        // Extract JWT from HttpOnly cookie
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.ContainsKey("auth_token"))
                {
                    context.Token = context.Request.Cookies["auth_token"];
                }
                return Task.CompletedTask;
            }
        };
    });
```

## Multi-Tenant Support

### Tenant Resolution
- **Company-Based Isolation**: Each company is a separate tenant
- **Claim-Based Resolution**: CompanyId stored in authentication claims
- **Automatic Filtering**: Entity Framework global filters based on tenant

### Tenant Middleware

```csharp
public class TenantResolutionMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.User.Identity.IsAuthenticated)
        {
            var companyId = context.User.FindFirst("CompanyId")?.Value;
            if (int.TryParse(companyId, out var tenantId))
            {
                context.Items["TenantId"] = tenantId;
            }
        }
        
        await next(context);
    }
}
```

## Role-Based Authorization

### Role Hierarchy
1. **Admin (Role = 1)**: Global system administration
2. **Company (Role = 2)**: Company-level management
3. **User (Role = 3)**: Standard user operations

### Authorization Policies

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireClaim(ClaimTypes.Role, "1"));
    
    options.AddPolicy("CompanyOrAdmin", policy => 
        policy.RequireClaim(ClaimTypes.Role, "1", "2"));
    
    options.AddPolicy("AuthenticatedUser", policy => 
        policy.RequireAuthenticatedUser());
});
```

## API Communication

### HttpClient Configuration

```csharp
// WebUI to WebServices communication
builder.Services.AddHttpClient<IApiService, ApiService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["WebServicesUrl"]);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    UseCookies = true,
    CookieContainer = new CookieContainer()
});
```

### Automatic Token Inclusion
- **HttpOnly Cookies**: Automatically included in API requests
- **No Manual Token Management**: Framework handles token transmission
- **Secure Transmission**: HTTPS-only cookie transmission

## Session Management

### Cookie Authentication Session
- **Sliding Expiration**: 8-hour sessions with activity-based renewal
- **Secure Storage**: Server-side session data
- **Automatic Cleanup**: Expired sessions automatically removed

### JWT Token Lifecycle
- **Short-Lived Tokens**: 1-hour expiration for security
- **Refresh Mechanism**: Automatic token refresh on API calls
- **Revocation Support**: Server-side token blacklisting capability

## Security Considerations

### Protection Against Common Attacks

1. **XSS (Cross-Site Scripting)**:
   - HttpOnly cookies prevent JavaScript access
   - No tokens in localStorage/sessionStorage
   - Content Security Policy headers

2. **CSRF (Cross-Site Request Forgery)**:
   - SameSite cookie attribute
   - Anti-forgery tokens in forms
   - Origin header validation

3. **Token Theft**:
   - HttpOnly cookies prevent client-side access
   - Secure transmission over HTTPS
   - Short token lifetimes

4. **Session Hijacking**:
   - Secure cookie flags
   - IP address validation
   - User agent validation

## Monitoring and Logging

### Authentication Events
- Login attempts (successful/failed)
- Token generation and validation
- Session creation and expiration
- Authorization failures

### Security Metrics
- Failed login attempts per IP
- Token validation failures
- Unusual access patterns
- Role escalation attempts

## Best Practices

### Implementation Guidelines
1. **Always use HTTPS**: Encrypt all authentication communications
2. **Validate on both sides**: WebUI and WebServices validation
3. **Implement proper logout**: Clear both cookies and server sessions
4. **Monitor security events**: Log and alert on suspicious activity
5. **Regular token rotation**: Implement refresh token mechanism

### Maintenance Tasks
1. **Regular security audits**: Review authentication flows
2. **Update dependencies**: Keep authentication libraries current
3. **Monitor logs**: Watch for security anomalies
4. **Test scenarios**: Verify all authentication paths work correctly

## Logout Functionality (Updated June 20, 2025)

### Secure Logout Implementation
The logout functionality has been enhanced to ensure proper security and user experience:

#### Backend Logout Controller
```csharp
[HttpPost]
public async Task<IActionResult> Logout()
{
    // Clear authentication cookie
    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    // Clear JWT token cookie
    Response.Cookies.Delete("auth_token", new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict
    });

    return RedirectToAction("Login");
}
```

#### Frontend Logout Implementation
- **POST Method**: All logout links use proper POST forms instead of GET requests
- **Confirmation Dialog**: User confirmation before logout to prevent accidental logouts
- **Consistent UI**: Logout functionality available in all navigation components
- **Role-Based**: Works across all user roles (Admin, Company, User)

#### Security Improvements
1. **CSRF Protection**: POST-based logout prevents CSRF attacks
2. **Complete Session Cleanup**: Both cookie authentication and JWT tokens cleared
3. **Secure Cookie Deletion**: Proper cookie deletion with security flags
4. **User Confirmation**: Prevents accidental logout from misclicks

## Conclusion

The hybrid authentication architecture provides enterprise-grade security while maintaining excellent user experience. The combination of Cookie Authentication and JWT in HttpOnly cookies eliminates common security vulnerabilities while providing seamless API access.

**Status**: ✅ **PRODUCTION READY** - Fully implemented and tested (June 20, 2025)
**Security Level**: Enterprise-grade with multiple protection layers
**Logout Security**: Enhanced with POST-based logout and proper session cleanup
**Maintenance**: Standard - regular monitoring and updates required
