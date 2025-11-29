using AttendenceManagementSystem.Areas.Identity.Data;
using AttendenceManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AttendenceManagementSystem.Areas.Identity.Data;

/// <summary>
/// Multi-Tenant ApplicationDbContext with Global Query Filters.
/// Ensures data isolation between Institutes (Tenants).
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    // Store the current InstituteId for the request (set by middleware/filter)
    public int? CurrentInstituteId { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets for all entities
    public DbSet<Institute> Institutes { get; set; }
    public DbSet<Batch> Batches { get; set; }
    public DbSet<Section> Sections { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<CourseOffering> CourseOfferings { get; set; }
    public DbSet<ClassSchedule> ClassSchedules { get; set; }
    public DbSet<StudentEnrollment> StudentEnrollments { get; set; }
    public DbSet<AttendanceRecord> AttendanceRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // =====================================================
        // FLUENT API CONFIGURATION: Foreign Keys & Relationships
        // =====================================================

        // Institute Configuration
        builder.Entity<Institute>(entity =>
        {
            entity.HasIndex(i => i.Code).IsUnique();
            entity.Property(i => i.Code).IsRequired();
        });

        // ApplicationUser Configuration
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasOne(u => u.Institute)
                .WithMany(i => i.Users)
                .HasForeignKey(u => u.InstituteId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete
        });

        // Batch Configuration
        builder.Entity<Batch>(entity =>
        {
            entity.HasOne(b => b.Institute)
                .WithMany(i => i.Batches)
                .HasForeignKey(b => b.InstituteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Section Configuration
        builder.Entity<Section>(entity =>
        {
            entity.HasOne(s => s.Institute)
                .WithMany(i => i.Sections)
                .HasForeignKey(s => s.InstituteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Subject Configuration
        builder.Entity<Subject>(entity =>
        {
            entity.HasOne(s => s.Institute)
                .WithMany(i => i.Subjects)
                .HasForeignKey(s => s.InstituteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // CourseOffering Configuration
        builder.Entity<CourseOffering>(entity =>
        {
            entity.HasOne(co => co.Institute)
                .WithMany(i => i.CourseOfferings)
                .HasForeignKey(co => co.InstituteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(co => co.Teacher)
                .WithMany(u => u.TaughtCourses)
                .HasForeignKey(co => co.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(co => co.Subject)
                .WithMany(s => s.CourseOfferings)
                .HasForeignKey(co => co.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(co => co.Section)
                .WithMany(s => s.CourseOfferings)
                .HasForeignKey(co => co.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(co => co.Batch)
                .WithMany(b => b.CourseOfferings)
                .HasForeignKey(co => co.BatchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ClassSchedule Configuration
        builder.Entity<ClassSchedule>(entity =>
        {
            entity.HasOne(cs => cs.Institute)
                .WithMany(i => i.ClassSchedules)
                .HasForeignKey(cs => cs.InstituteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(cs => cs.CourseOffering)
                .WithMany(co => co.ClassSchedules)
                .HasForeignKey(cs => cs.CourseOfferingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // StudentEnrollment Configuration
        builder.Entity<StudentEnrollment>(entity =>
        {
            entity.HasOne(se => se.Institute)
                .WithMany(i => i.StudentEnrollments)
                .HasForeignKey(se => se.InstituteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(se => se.Student)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(se => se.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(se => se.CourseOffering)
                .WithMany(co => co.StudentEnrollments)
                .HasForeignKey(se => se.CourseOfferingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // AttendanceRecord Configuration
        builder.Entity<AttendanceRecord>(entity =>
        {
            entity.HasOne(ar => ar.Institute)
                .WithMany(i => i.AttendanceRecords)
                .HasForeignKey(ar => ar.InstituteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ar => ar.StudentEnrollment)
                .WithMany(se => se.AttendanceRecords)
                .HasForeignKey(ar => ar.StudentEnrollmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ar => ar.MarkedByTeacher)
                .WithMany(u => u.MarkedAttendances)
                .HasForeignKey(ar => ar.MarkedByTeacherId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =====================================================
        // GLOBAL QUERY FILTERS: Multi-Tenancy Data Isolation + Soft Delete
        // =====================================================
        // These filters ensure that queries automatically filter by InstituteId
        // when CurrentInstituteId is set (via middleware or service).

        // NOTE: ApplicationUser does NOT have a global filter
        // Identity framework needs direct access to users for authentication
        // We manually filter users in our queries when needed
        
        builder.Entity<Batch>().HasQueryFilter(b => b.InstituteId == CurrentInstituteId);
        builder.Entity<Section>().HasQueryFilter(s => s.InstituteId == CurrentInstituteId);
        builder.Entity<Subject>().HasQueryFilter(s => s.InstituteId == CurrentInstituteId);
        builder.Entity<CourseOffering>().HasQueryFilter(co => co.InstituteId == CurrentInstituteId);
        
        // ClassSchedule: Multi-tenancy + Soft Delete filter
        builder.Entity<ClassSchedule>().HasQueryFilter(cs => 
            cs.InstituteId == CurrentInstituteId && !cs.IsDeleted);
        
        builder.Entity<StudentEnrollment>().HasQueryFilter(se => se.InstituteId == CurrentInstituteId);
        builder.Entity<AttendanceRecord>().HasQueryFilter(ar => ar.InstituteId == CurrentInstituteId);
    }
}
