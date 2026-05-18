using AttendenceManagementSystem.Areas.Identity.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AttendenceManagementSystem.Controllers
{
    /// <summary>
    /// JWT-Protected API Controller for System Information.
    /// Demonstrates JWT Bearer authentication in a Multi-Tenant context.
    /// This controller ONLY accepts JWT tokens, not cookies.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class SystemInfoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SystemInfoController> _logger;

        public SystemInfoController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<SystemInfoController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/SystemInfo/status
        /// Returns system status and authenticated user information from JWT claims.
        /// Proves that JWT authentication is working correctly.
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            // Extract claims from JWT token
            var userId = User.FindFirstValue("UserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var fullName = User.FindFirstValue("FullName") ?? User.FindFirstValue(ClaimTypes.Name);
            var instituteId = User.FindFirstValue("InstituteId");
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct().ToList();

            _logger.LogInformation("JWT API accessed by User: {UserId}, InstituteId: {InstituteId}", userId, instituteId);

            // Fetch Institute name for display
            string instituteName = "Unknown";
            if (int.TryParse(instituteId, out int instId))
            {
                var institute = await _context.Institutes.FindAsync(instId);
                instituteName = institute?.Name ?? "Unknown";
            }

            // Build response object
            var response = new
            {
                Status = "Online",
                Timestamp = DateTime.UtcNow,
                ServerTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, 
                    TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time")),
                Authentication = new
                {
                    Type = "JWT Bearer Token",
                    IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                    TokenValid = true
                },
                User = new
                {
                    UserId = userId,
                    Email = email,
                    FullName = fullName,
                    Roles = roles
                },
                Tenant = new
                {
                    InstituteId = instituteId,
                    InstituteName = instituteName
                },
                System = new
                {
                    Application = "Present! Attendance Management System",
                    Version = "1.0.0",
                    Framework = ".NET 8.0",
                    Architecture = "Multi-Tenant SaaS"
                }
            };

            return Ok(response);
        }

        /// <summary>
        /// GET: api/SystemInfo/claims
        /// Returns all JWT claims for debugging/demonstration purposes.
        /// </summary>
        [HttpGet("claims")]
        public IActionResult GetClaims()
        {
            var claims = User.Claims.Select(c => new
            {
                Type = c.Type,
                Value = c.Value
            }).ToList();

            return Ok(new
            {
                TotalClaims = claims.Count,
                Claims = claims
            });
        }

        /// <summary>
        /// GET: api/SystemInfo/tenant-stats
        /// Returns tenant-specific statistics. Demonstrates multi-tenant data isolation via JWT claims.
        /// </summary>
        [HttpGet("tenant-stats")]
        public async Task<IActionResult> GetTenantStats()
        {
            var instituteIdClaim = User.FindFirstValue("InstituteId");
            
            if (!int.TryParse(instituteIdClaim, out int instituteId))
            {
                return BadRequest(new { Error = "Invalid InstituteId in token" });
            }

            // Fetch tenant-scoped statistics
            var institute = await _context.Institutes.FindAsync(instituteId);
            var userCount = await _context.Users.CountAsync(u => u.InstituteId == instituteId);
            var batchCount = await _context.Batches.CountAsync(b => b.InstituteId == instituteId);
            var subjectCount = await _context.Subjects.CountAsync(s => s.InstituteId == instituteId);

            return Ok(new
            {
                Tenant = new
                {
                    InstituteId = instituteId,
                    Name = institute?.Name ?? "Unknown",
                    Code = institute?.Code ?? "N/A"
                },
                Statistics = new
                {
                    TotalUsers = userCount,
                    TotalBatches = batchCount,
                    TotalSubjects = subjectCount
                },
                DataIsolation = "All data filtered by InstituteId from JWT claim"
            });
        }
    }
}
