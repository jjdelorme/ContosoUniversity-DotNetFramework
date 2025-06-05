using ContosoUniversityCore.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversityCore.Data
{
    public class SchoolContext : DbContext
    {
        public SchoolContext(DbContextOptions<SchoolContext> options) : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Instructor> Instructors { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<OfficeAssignment> OfficeAssignments { get; set; }
        public DbSet<Person> People { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Course>().ToTable("Course");
            modelBuilder.Entity<Department>().ToTable("Department");
            modelBuilder.Entity<Enrollment>().ToTable("Enrollment");
            // Removed ToTable for Instructor and Student to enable TPH for Person hierarchy
            // modelBuilder.Entity<Instructor>().ToTable("Instructor"); 
            // modelBuilder.Entity<Student>().ToTable("Student");
            modelBuilder.Entity<OfficeAssignment>().ToTable("OfficeAssignment");
            modelBuilder.Entity<Person>().ToTable("Person"); // This will be the base table for Student and Instructor

            modelBuilder.Entity<Course>()
                .HasMany(c => c.Instructors)
                .WithMany(i => i.Courses)
                .UsingEntity<Dictionary<string, object>>(
                    "CourseInstructor",
                    j => j
                        .HasOne<Instructor>()
                        .WithMany()
                        .HasForeignKey("InstructorID")
                        .HasConstraintName("FK_CourseInstructor_Instructor_InstructorID")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j
                        .HasOne<Course>()
                        .WithMany()
                        .HasForeignKey("CourseID")
                        .HasConstraintName("FK_CourseInstructor_Course_CourseID")
                        .OnDelete(DeleteBehavior.Cascade));
            
            // Configure Department.RowVersion as a concurrency token
            modelBuilder.Entity<Department>()
                .Property(d => d.RowVersion)
                .IsConcurrencyToken(); // Application provides value on insert; used for optimistic concurrency checks

            // For OfficeAssignment, InstructorID is PK and FK
            modelBuilder.Entity<OfficeAssignment>()
                .HasKey(oa => oa.InstructorID);
        }
    }
}
