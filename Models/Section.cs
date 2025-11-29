using System.ComponentModel.DataAnnotations;

namespace AttendenceManagementSystem.Models
{
    /// <summary>
    /// Represents a class section (e.g., "CS-A", "BIO-B").
    /// Scoped to an Institute.
    /// </summary>
    public class Section
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // Multi-Tenancy: Foreign Key to Institute
        [Required]
        public int InstituteId { get; set; }
        public Institute Institute { get; set; } = null!;

        // Navigation Properties
        public ICollection<CourseOffering> CourseOfferings { get; set; } = new List<CourseOffering>();
    }
}
