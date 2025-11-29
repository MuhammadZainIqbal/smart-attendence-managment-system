using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace AttendenceManagementSystem.Services
{
    /// <summary>
    /// Seeds the default roles (Admin, Teacher, Student) into the database on application startup.
    /// Does NOT create any default users (users are created via Sign Up or Admin dashboard).
    /// </summary>
    public static class RoleSeeder
    {
        private static readonly string[] Roles = new string[]
        {
            "Admin",
            "Teacher",
            "Student"
        };

        /// <summary>
        /// Ensures all required roles exist in the database.
        /// </summary>
        public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}
