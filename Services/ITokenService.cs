using AttendenceManagementSystem.Areas.Identity.Data;

namespace AttendenceManagementSystem.Services
{
    /// <summary>
    /// Interface for JWT Token generation service.
    /// Supports Multi-Tenant architecture by including InstituteId in token claims.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Generates a JWT token for the authenticated user.
        /// </summary>
        /// <param name="user">The authenticated ApplicationUser</param>
        /// <param name="roles">List of roles assigned to the user</param>
        /// <returns>JWT token string</returns>
        string GenerateToken(ApplicationUser user, IList<string> roles);
    }
}
