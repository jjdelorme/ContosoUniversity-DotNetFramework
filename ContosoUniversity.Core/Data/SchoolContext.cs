using ContosoUniversity.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Core.Data
{
    public class SchoolContext : IdentityDbContext<IdentityUser>
    {
        public SchoolContext(DbContextOptions<SchoolContext> options) : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Instructor> Instructors { get; set; }
        public DbSet<OfficeAssignment> OfficeAssignments { get; set; }
        public DbSet<CourseAssignment> CourseAssignments { get; set; }
        public DbSet<Person> People { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure table names
            modelBuilder.Entity<Course>().ToTable("Course");
            modelBuilder.Entity<Enrollment>().ToTable("Enrollment");
            modelBuilder.Entity<Student>().ToTable("Student");
            modelBuilder.Entity<Department>().ToTable("Department");
            modelBuilder.Entity<Instructor>().ToTable("Instructor");
            modelBuilder.Entity<OfficeAssignment>().ToTable("OfficeAssignment");
            modelBuilder.Entity<CourseAssignment>().ToTable("CourseAssignment");
            modelBuilder.Entity<Person>().ToTable("Person");

            // Configure many-to-many relationship
            modelBuilder.Entity<CourseAssignment>()
                .HasKey(c => new { c.CourseID, c.InstructorID });

            modelBuilder.Entity<CourseAssignment>()
                .HasOne(ca => ca.Course)
                .WithMany(c => c.CourseAssignments)
                .HasForeignKey(ca => ca.CourseID);

            modelBuilder.Entity<CourseAssignment>()
                .HasOne(ca => ca.Instructor)
                .WithMany(i => i.CourseAssignments)
                .HasForeignKey(ca => ca.InstructorID);

            // Configure one-to-zero-or-one relationship
            modelBuilder.Entity<Instructor>()
                .HasOne(i => i.OfficeAssignment)
                .WithOne(oa => oa.Instructor)
                .HasForeignKey<OfficeAssignment>(oa => oa.InstructorID);

            // Configure Department-Instructor relationship
            modelBuilder.Entity<Department>()
                .HasOne(d => d.Administrator)
                .WithMany()
                .HasForeignKey(d => d.InstructorID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure decimal precision for Budget
            modelBuilder.Entity<Department>()
                .Property(d => d.Budget)
                .HasPrecision(19, 4);

            // Configure TPH (Table Per Hierarchy) inheritance
            modelBuilder.Entity<Person>()
                .HasDiscriminator<string>("Discriminator")
                .HasValue<Student>("Student")
                .HasValue<Instructor>("Instructor");

            // Create stored procedure for Department insert
            modelBuilder.Entity<Department>()
                .Property(p => p.RowVersion)
                .IsConcurrencyToken();
        }

        public override int SaveChanges()
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is Person && (
                    e.State == EntityState.Added
                    || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                if (entityEntry.Entity is Person person)
                {
                    // Trim string properties
                    if (!string.IsNullOrEmpty(person.LastName))
                        person.LastName = person.LastName.Trim();
                    if (!string.IsNullOrEmpty(person.FirstMidName))
                        person.FirstMidName = person.FirstMidName.Trim();
                }
            }

            // Handle RowVersion for SQLite
            var departmentEntries = ChangeTracker
                .Entries<Department>()
                .Where(e => e.State == EntityState.Modified || e.State == EntityState.Added);

            foreach (var entityEntry in departmentEntries)
            {
                entityEntry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            }

            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is Person && (
                    e.State == EntityState.Added
                    || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                if (entityEntry.Entity is Person person)
                {
                    // Trim string properties
                    if (!string.IsNullOrEmpty(person.LastName))
                        person.LastName = person.LastName.Trim();
                    if (!string.IsNullOrEmpty(person.FirstMidName))
                        person.FirstMidName = person.FirstMidName.Trim();
                }
            }

            // Handle RowVersion for SQLite
            var departmentEntries = ChangeTracker
                .Entries<Department>()
                .Where(e => e.State == EntityState.Modified || e.State == EntityState.Added);

            foreach (var entityEntry in departmentEntries)
            {
                entityEntry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}