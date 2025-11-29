using System.ComponentModel.DataAnnotations;

namespace AttendenceManagementSystem.Models
{
    /// <summary>
    /// Represents an academic subject/course (e.g., "Calculus", "Data Structures").
    /// Scoped to an Institute.
    /// </summary>
    public class Subject
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty; // e.g., "CS-101", "MATH-201"

        // Multi-Tenancy: Foreign Key to Institute
        [Required]
        public int InstituteId { get; set; }
        public Institute Institute { get; set; } = null!;

        // Navigation Properties
        public ICollection<CourseOffering> CourseOfferings { get; set; } = new List<CourseOffering>();
    }
}
