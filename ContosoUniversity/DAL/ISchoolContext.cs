using ContosoUniversity.Models;
using System.Data.Entity; // For IDbSet
using System; // For IDisposable
using System.Data.Entity.Infrastructure; // For DatabaseFacade and DbEntityEntry

namespace ContosoUniversity.DAL
{
    public interface ISchoolContext : IDisposable
    {
        IDbSet<Course> Courses { get; }
        IDbSet<Department> Departments { get; }
        IDbSet<Enrollment> Enrollments { get; }
        IDbSet<Instructor> Instructors { get; }
        IDbSet<Student> Students { get; }
        IDbSet<OfficeAssignment> OfficeAssignments { get; }
        IDbSet<Person> People { get; }
        int SaveChanges();
        // We need a way to call DbContext.Entry() for updates.
        // Adding a generic GetEntryState method or specific methods per entity might be needed.
        // For now, let's add a generic method to get the entry for an entity.
        // This is often needed for testing updates.
        System.Data.Entity.Infrastructure.DbEntityEntry Entry(object entity);
        DatabaseFacade Database { get; }
    }
}
