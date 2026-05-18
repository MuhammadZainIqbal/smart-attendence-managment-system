using AttendenceManagementSystem.Areas.Identity.Data;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AttendenceManagementSystem.Services
{
    /// <summary>
    /// JWT Token generation service for Multi-Tenant Attendance System.
    /// Generates tokens containing UserId, Email, Role(s), and InstituteId for tenant isolation.
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;

        public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Generates a JWT token with multi-tenant claims.
        /// Token includes: UserId, Email, FullName, InstituteId, and all assigned Roles.
        /// </summary>
        /// <param name="user">The authenticated ApplicationUser</param>
        /// <param name="roles">List of roles assigned to the user</param>
        /// <returns>Signed JWT token string</returns>
        public string GenerateToken(ApplicationUser user, IList<string> roles)
        {
            // Retrieve JWT settings from configuration
            var jwtKey = _configuration["Jwt:Key"] 
                ?? throw new InvalidOperationException("JWT Key not configured in appsettings.json");
            var jwtIssuer = _configuration["Jwt:Issuer"] 
                ?? throw new InvalidOperationException("JWT Issuer not configured in appsettings.json");
            var jwtAudience = _configuration["Jwt:Audience"] 
                ?? throw new InvalidOperationException("JWT Audience not configured in appsettings.json");
            var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60");

            // Create symmetric security key from secret
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Build claims list - CRITICAL for Multi-Tenant Authorization
            var claims = new List<Claim>
            {
                // Standard JWT Claims
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                
                // Custom Claims for Application
                new Claim("UserId", user.Id),
                new Claim("FullName", user.FullName),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                
                // MULTI-TENANT CLAIM - Critical for tenant isolation
                new Claim("InstituteId", user.InstituteId.ToString()),
                
                // Optional: Student-specific claims
                new Claim("RollNumber", user.RollNumber ?? string.Empty)
            };

            // Add role claims - supports multiple roles per user
            foreach (var role in roles)
            {
                // Use ClaimTypes.Role which maps to the standard role claim
                // ASP.NET Core's JWT handler recognizes this for [Authorize(Roles = "...")] 
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Create JWT Security Token
            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            // Serialize token to string
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            _logger.LogInformation(
                "JWT Token generated for User: {UserId}, Email: {Email}, InstituteId: {InstituteId}, Roles: {Roles}, Expires: {Expiry}",
                user.Id, user.Email, user.InstituteId, string.Join(", ", roles), token.ValidTo);

            return tokenString;
        }
    }
}
