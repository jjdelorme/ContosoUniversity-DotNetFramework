using ContosoUniversityCore.Models;
using System;
using System.Linq;
using System.Collections.Generic;

namespace ContosoUniversityCore.Data
{
    public static class DbInitializer
    {
        public static void Initialize(SchoolContext context)
        {
            context.Database.EnsureCreated();

            // Look for any students.
            if (context.Students.Any())
            {
                return;   // DB has been seeded
            }

            var students = new Student[]
            {
                new Student{FirstMidName="Carson",LastName="Alexander",EnrollmentDate=DateTime.Parse("2019-09-01")},
                new Student{FirstMidName="Meredith",LastName="Alonso",EnrollmentDate=DateTime.Parse("2017-09-01")},
                new Student{FirstMidName="Arturo",LastName="Anand",EnrollmentDate=DateTime.Parse("2018-09-01")},
                new Student{FirstMidName="Gytis",LastName="Barzdukas",EnrollmentDate=DateTime.Parse("2017-09-01")},
                new Student{FirstMidName="Yan",LastName="Li",EnrollmentDate=DateTime.Parse("2017-09-01")},
                new Student{FirstMidName="Peggy",LastName="Justice",EnrollmentDate=DateTime.Parse("2016-09-01")},
                new Student{FirstMidName="Laura",LastName="Norman",EnrollmentDate=DateTime.Parse("2018-09-01")},
                new Student{FirstMidName="Nino",LastName="Olivetto",EnrollmentDate=DateTime.Parse("2019-09-01")}
            };
            foreach (Student s in students)
            {
                context.Students.Add(s);
            }
            context.SaveChanges();

            var instructors = new Instructor[]
            {
                new Instructor { FirstMidName = "Kim",     LastName = "Abercrombie", HireDate = DateTime.Parse("1995-03-11") },
                new Instructor { FirstMidName = "Fadi",    LastName = "Fakhouri",    HireDate = DateTime.Parse("2002-07-06") },
                new Instructor { FirstMidName = "Roger",   LastName = "Harui",       HireDate = DateTime.Parse("1998-07-01") },
                new Instructor { FirstMidName = "Candace", LastName = "Kapoor",      HireDate = DateTime.Parse("2001-01-15") },
                new Instructor { FirstMidName = "Roger",   LastName = "Zheng",       HireDate = DateTime.Parse("2004-02-12") }
            };
            foreach (Instructor i in instructors)
            {
                context.Instructors.Add(i);
            }
            context.SaveChanges();

            var departments = new Department[]
            {
                new Department { Name = "English",     Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = instructors.Single( i => i.LastName == "Abercrombie").ID, RowVersion = Guid.NewGuid().ToByteArray() },
                new Department { Name = "Mathematics", Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = instructors.Single( i => i.LastName == "Fakhouri").ID, RowVersion = Guid.NewGuid().ToByteArray() },
                new Department { Name = "Engineering", Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = instructors.Single( i => i.LastName == "Harui").ID, RowVersion = Guid.NewGuid().ToByteArray() },
                new Department { Name = "Economics",   Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = instructors.Single( i => i.LastName == "Kapoor").ID, RowVersion = Guid.NewGuid().ToByteArray() }
            };
            foreach (Department d in departments)
            {
                context.Departments.Add(d);
            }
            context.SaveChanges();
            
            // Retrieve Department IDs for course assignment
            int englishDeptID = departments.Single(d => d.Name == "English").DepartmentID;
            int mathDeptID = departments.Single(d => d.Name == "Mathematics").DepartmentID;
            int engineeringDeptID = departments.Single(d => d.Name == "Engineering").DepartmentID;
            int economicsDeptID = departments.Single(d => d.Name == "Economics").DepartmentID;

            var courses = new Course[]
            {
                new Course{CourseID=1050,Title="Chemistry",Credits=3,DepartmentID=engineeringDeptID},
                new Course{CourseID=4022,Title="Microeconomics",Credits=3,DepartmentID=economicsDeptID},
                new Course{CourseID=4041,Title="Macroeconomics",Credits=3,DepartmentID=economicsDeptID},
                new Course{CourseID=1045,Title="Calculus",Credits=4,DepartmentID=mathDeptID},
                new Course{CourseID=3141,Title="Trigonometry",Credits=4,DepartmentID=mathDeptID},
                new Course{CourseID=2021,Title="Composition",Credits=3,DepartmentID=englishDeptID},
                new Course{CourseID=2042,Title="Literature",Credits=4,DepartmentID=englishDeptID}
            };
            foreach (Course c in courses)
            {
                context.Courses.Add(c);
            }
            context.SaveChanges();

            var officeAssignments = new OfficeAssignment[]
            {
                new OfficeAssignment { InstructorID = instructors.Single( i => i.LastName == "Fakhouri").ID, Location = "Smith 17" },
                new OfficeAssignment { InstructorID = instructors.Single( i => i.LastName == "Harui").ID, Location = "Gowan 27" },
                new OfficeAssignment { InstructorID = instructors.Single( i => i.LastName == "Kapoor").ID, Location = "Thompson 304" },
            };
            foreach (OfficeAssignment o in officeAssignments)
            {
                context.OfficeAssignments.Add(o);
            }
            context.SaveChanges();

            // Course-Instructor relationships (many-to-many)
            // EF Core 7+ handles this via skip navigations if Course.Instructors and Instructor.Courses are populated.
            // For explicit join table seeding, one would add to the join entity if it were defined.
            // Here, we'll assign instructors to courses directly.
            
            var chemistry = courses.Single(c => c.Title == "Chemistry");
            chemistry.Instructors = new List<Instructor> { instructors.Single(i => i.LastName == "Kapoor"), instructors.Single(i => i.LastName == "Harui") };
            
            var microeconomics = courses.Single(c => c.Title == "Microeconomics");
            microeconomics.Instructors = new List<Instructor> { instructors.Single(i => i.LastName == "Zheng") };

            var macroeconomics = courses.Single(c => c.Title == "Macroeconomics");
            macroeconomics.Instructors = new List<Instructor> { instructors.Single(i => i.LastName == "Zheng") };

            var calculus = courses.Single(c => c.Title == "Calculus");
            calculus.Instructors = new List<Instructor> { instructors.Single(i => i.LastName == "Fakhouri") };

            var trigonometry = courses.Single(c => c.Title == "Trigonometry");
            trigonometry.Instructors = new List<Instructor> { instructors.Single(i => i.LastName == "Harui") };
            
            var composition = courses.Single(c => c.Title == "Composition");
            composition.Instructors = new List<Instructor> { instructors.Single(i => i.LastName == "Abercrombie") };

            var literature = courses.Single(c => c.Title == "Literature");
            literature.Instructors = new List<Instructor> { instructors.Single(i => i.LastName == "Abercrombie") };
            
            context.SaveChanges();


            var enrollments = new Enrollment[]
            {
                new Enrollment{StudentID=students.Single(s => s.LastName == "Alexander").ID,CourseID=courses.Single(c=>c.Title=="Chemistry").CourseID,Grade=Grade.A},
                new Enrollment{StudentID=students.Single(s => s.LastName == "Alexander").ID,CourseID=courses.Single(c=>c.Title=="Microeconomics").CourseID,Grade=Grade.C},
                new Enrollment{StudentID=students.Single(s => s.LastName == "Alexander").ID,CourseID=courses.Single(c=>c.Title=="Macroeconomics").CourseID,Grade=Grade.B},
                new Enrollment{StudentID=students.Single(s => s.LastName == "Alonso").ID,CourseID=courses.Single(c=>c.Title=="Calculus").CourseID,Grade=Grade.B},
                new Enrollment{StudentID=students.Single(s => s.LastName == "Alonso").ID,CourseID=courses.Single(c=>c.Title=="Trigonometry").CourseID,Grade=Grade.B},
                new Enrollment{StudentID=students.Single(s => s.LastName == "Alonso").ID,CourseID=courses.Single(c=>c.Title=="Composition").CourseID,Grade=Grade.B},
                new Enrollment{StudentID=students.Single(s => s.LastName == "Anand").ID,CourseID=courses.Single(c=>c.Title=="Chemistry").CourseID},
                new Enrollment{StudentID=students.Single(s => s.LastName == "Anand").ID,CourseID=courses.Single(c=>c.Title=="Microeconomics").CourseID,Grade=Grade.B},
                new Enrollment{StudentID=students.Single(s => s.LastName == "Barzdukas").ID,CourseID=courses.Single(c=>c.Title=="Chemistry").CourseID,Grade=Grade.B},
                new Enrollment{StudentID=students.Single(s => s.LastName == "Li").ID,CourseID=courses.Single(c=>c.Title=="Composition").CourseID,Grade=Grade.B},
                new Enrollment{StudentID=students.Single(s => s.LastName == "Justice").ID,CourseID=courses.Single(c=>c.Title=="Literature").CourseID,Grade=Grade.B}
            };
            foreach (Enrollment e in enrollments)
            {
                var enrollmentInDataBase = context.Enrollments.Where(
                    s =>
                            s.Student.ID == e.StudentID &&
                            s.Course.CourseID == e.CourseID).SingleOrDefault();
                if (enrollmentInDataBase == null)
                {
                    context.Enrollments.Add(e);
                }
            }
            context.SaveChanges();
        }
    }
}
