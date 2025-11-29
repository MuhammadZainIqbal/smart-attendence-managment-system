using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AttendenceManagementSystem.Models;
using Microsoft.AspNetCore.Identity;

namespace AttendenceManagementSystem.Areas.Identity.Data;

/// <summary>
/// Custom Identity User with Multi-Tenant support.
/// Extends IdentityUser with additional profile and tenant-scoped fields.
/// </summary>
public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Foreign Key to Institute (Tenant).
    /// Required for all users. Multi-tenancy is enforced at the user level.
    /// </summary>
    [Required]
    public int InstituteId { get; set; }
    public Institute Institute { get; set; } = null!;

    /// <summary>
    /// Tracks whether the user has changed their password after initial creation.
    /// Users created by Admin must change password on first login.
    /// </summary>
    public bool IsPasswordChanged { get; set; } = false;

    /// <summary>
    /// Student Roll Number (Student ID).
    /// Required for users in the Student role.
    /// Must be unique per InstituteId (case-insensitive).
    /// Format is free-text to accommodate various institutional formats (e.g., 2023-CS-101, CS/2023/01, 15492).
    /// </summary>
    [MaxLength(50)]
    public string? RollNumber { get; set; }

    /// <summary>
    /// Foreign Key to Batch (for Students only).
    /// Indicates which batch the student belongs to.
    /// </summary>
    public int? BatchId { get; set; }
    public Batch? Batch { get; set; }

    /// <summary>
    /// Foreign Key to Section (for Students only).
    /// Indicates which section the student belongs to.
    /// </summary>
    public int? SectionId { get; set; }
    public Section? Section { get; set; }

    // Navigation Properties
    // Teacher relationships
    public ICollection<CourseOffering> TaughtCourses { get; set; } = new List<CourseOffering>();
    public ICollection<AttendanceRecord> MarkedAttendances { get; set; } = new List<AttendanceRecord>();

    // Student relationships
    public ICollection<StudentEnrollment> Enrollments { get; set; } = new List<StudentEnrollment>();
}

