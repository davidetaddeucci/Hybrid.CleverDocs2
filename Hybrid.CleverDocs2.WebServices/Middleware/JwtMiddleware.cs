using System.Security.Claims;
using Hybrid.CleverDocs2.WebServices.Services.Auth;

namespace Hybrid.CleverDocs2.WebServices.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtMiddleware> _logger;

        public JwtMiddleware(RequestDelegate next, ILogger<JwtMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IJwtService jwtService)
        {
            var token = ExtractTokenFromRequest(context.Request);

            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    // Validate token
                    var principal = jwtService.ValidateToken(token);
                    
                    if (principal != null)
                    {
                        // Check if token is blacklisted
                        var isBlacklisted = await jwtService.IsTokenBlacklistedAsync(token);
                        
                        if (!isBlacklisted)
                        {
                            // Set user context
                            context.User = principal;
                            
                            // Add token to context for potential blacklisting on logout
                            context.Items["AccessToken"] = token;
                            
                            _logger.LogDebug("JWT token validated successfully for user {UserId}", 
                                principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                        }
                        else
                        {
                            _logger.LogWarning("Blacklisted token used: {Token}", token[..10] + "...");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Invalid JWT token: {Token}", token[..10] + "...");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating JWT token");
                }
            }

            await _next(context);
        }

        private static string? ExtractTokenFromRequest(HttpRequest request)
        {
            // Check Authorization header
            var authHeader = request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader["Bearer ".Length..].Trim();
            }

            // Check query parameter (for WebSocket connections, etc.)
            if (request.Query.TryGetValue("access_token", out var tokenFromQuery))
            {
                return tokenFromQuery.FirstOrDefault();
            }

            // Check cookie (if using cookie-based auth)
            if (request.Cookies.TryGetValue("access_token", out var tokenFromCookie))
            {
                return tokenFromCookie;
            }

            return null;
        }
    }
}