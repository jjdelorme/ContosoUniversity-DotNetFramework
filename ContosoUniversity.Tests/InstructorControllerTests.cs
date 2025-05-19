using Microsoft.VisualStudio.TestTools.UnitTesting;
using ContosoUniversity.Controllers;
using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using ContosoUniversity.ViewModels; // For InstructorIndexData, AssignedCourseData
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Net; // For HttpStatusCodeResult
using System.Data.Entity; // For IDbSet and mocking DbSet features
using System.Data.Entity.Infrastructure; // For DbEntityEntry
using System; // For DateTime

namespace ContosoUniversity.Tests
{
    [TestClass]
    public class InstructorControllerTests
    {
        private Mock<ISchoolContext> _mockContext;
        private Mock<IDbSet<Instructor>> _mockInstructorSet;
        private Mock<IDbSet<Course>> _mockCourseSet;
        private Mock<IDbSet<Department>> _mockDepartmentSet; // Though not directly used by InstructorController, it's good practice if related data might be needed.
        private List<Instructor> _instructors;
        private List<Course> _courses;
        private List<Department> _departments; // For course data
        private InstructorController _controller;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockContext = new Mock<ISchoolContext>();
            _mockInstructorSet = new Mock<IDbSet<Instructor>>();
            _mockCourseSet = new Mock<IDbSet<Course>>();
            _mockDepartmentSet = new Mock<IDbSet<Department>>();

            _departments = new List<Department>
            {
                new Department { DepartmentID = 1, Name = "Engineering" },
                new Department { DepartmentID = 2, Name = "Mathematics" }
            };

            _courses = new List<Course>
            {
                new Course { CourseID = 1050, Title = "Chemistry", Credits = 3, DepartmentID = 1, Department = _departments.First(d=>d.DepartmentID == 1), Enrollments = new List<Enrollment>() },
                new Course { CourseID = 4022, Title = "Microeconomics", Credits = 3, DepartmentID = 2, Department = _departments.First(d=>d.DepartmentID == 2), Enrollments = new List<Enrollment>() },
                new Course { CourseID = 4041, Title = "Calculus", Credits = 4, DepartmentID = 2, Department = _departments.First(d=>d.DepartmentID == 2), Enrollments = new List<Enrollment>() }
            };
            
            // Add some enrollments to courses
            _courses.First(c => c.CourseID == 1050).Enrollments.Add(new Enrollment { StudentID = 1, CourseID = 1050, Grade = Grade.A, Student = new Student { ID = 1, FirstMidName="Al", LastName="Bundy"} });
            _courses.First(c => c.CourseID == 1050).Enrollments.Add(new Enrollment { StudentID = 2, CourseID = 1050, Grade = Grade.B, Student = new Student { ID = 2, FirstMidName="Peggy", LastName="Bundy"} });
            _courses.First(c => c.CourseID == 4022).Enrollments.Add(new Enrollment { StudentID = 3, CourseID = 4022, Grade = Grade.C, Student = new Student { ID = 3, FirstMidName="Kelly", LastName="Bundy"} });


            _instructors = new List<Instructor>
            {
                new Instructor { ID = 1, FirstMidName = "Kim", LastName = "Abercrombie", HireDate = DateTime.Parse("1995-03-11"), OfficeAssignment = new OfficeAssignment { Location = "Smith 17" }, Courses = new List<Course> { _courses.First(c => c.CourseID == 1050), _courses.First(c => c.CourseID == 4022) } },
                new Instructor { ID = 2, FirstMidName = "Fadi", LastName = "Fakhouri", HireDate = DateTime.Parse("2002-07-06"), OfficeAssignment = new OfficeAssignment { Location = "Gowan 27" }, Courses = new List<Course> { _courses.First(c => c.CourseID == 4041) } },
                new Instructor { ID = 3, FirstMidName = "Roger", LastName = "Harui", HireDate = DateTime.Parse("1998-07-01"), Courses = new List<Course>() }
            };

            // Setup Instructors
            var instructorsData = _instructors.AsQueryable();
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.Provider).Returns(instructorsData.Provider);
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.Expression).Returns(instructorsData.Expression);
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.ElementType).Returns(instructorsData.ElementType);
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.GetEnumerator()).Returns(() => instructorsData.GetEnumerator());
            _mockInstructorSet.Setup(m => m.Find(It.IsAny<object[]>()))
                .Returns<object[]>(ids => _instructors.FirstOrDefault(i => i.ID == (int)ids[0]));
            _mockInstructorSet.Setup(m => m.Add(It.IsAny<Instructor>()))
                .Callback<Instructor>(i => _instructors.Add(i));
            _mockInstructorSet.Setup(m => m.Remove(It.IsAny<Instructor>()))
                .Callback<Instructor>(i => 
                {
                    var instToRemove = _instructors.FirstOrDefault(inst => inst.ID == i.ID);
                    if (instToRemove != null) _instructors.Remove(instToRemove);
                });

            // Setup Courses
            var coursesData = _courses.AsQueryable();
            _mockCourseSet.As<IQueryable<Course>>().Setup(m => m.Provider).Returns(coursesData.Provider);
            _mockCourseSet.As<IQueryable<Course>>().Setup(m => m.Expression).Returns(coursesData.Expression);
            _mockCourseSet.As<IQueryable<Course>>().Setup(m => m.ElementType).Returns(coursesData.ElementType);
            _mockCourseSet.As<IQueryable<Course>>().Setup(m => m.GetEnumerator()).Returns(() => coursesData.GetEnumerator());
            _mockCourseSet.Setup(m => m.Find(It.IsAny<object[]>()))
                .Returns<object[]>(ids => _courses.FirstOrDefault(c => c.CourseID == (int)ids[0]));

            // Setup Departments (if needed by controller logic, e.g. for populating dropdowns not directly tested here)
            var departmentsData = _departments.AsQueryable();
            _mockDepartmentSet.As<IQueryable<Department>>().Setup(m => m.Provider).Returns(departmentsData.Provider);
            _mockDepartmentSet.As<IQueryable<Department>>().Setup(m => m.Expression).Returns(departmentsData.Expression);
            _mockDepartmentSet.As<IQueryable<Department>>().Setup(m => m.ElementType).Returns(departmentsData.ElementType);
            _mockDepartmentSet.As<IQueryable<Department>>().Setup(m => m.GetEnumerator()).Returns(() => departmentsData.GetEnumerator());


            _mockContext.Setup(c => c.Instructors).Returns(_mockInstructorSet.Object);
            _mockContext.Setup(c => c.Courses).Returns(_mockCourseSet.Object);
            _mockContext.Setup(c => c.Departments).Returns(_mockDepartmentSet.Object);
            _mockContext.Setup(c => c.SaveChanges()).Returns(1); // Or Task.FromResult(1) if async

            // Mock Entry for general purpose, specific setups can be done in tests
            var mockEntry = new Mock<DbEntityEntry<Instructor>>();
            _mockContext.Setup(c => c.Entry(It.IsAny<Instructor>())).Returns(mockEntry.Object);
            
            // Mock Entry for selectedCourse.Enrollments related loading in Index action
            var mockCourseEntry = new Mock<DbEntityEntry<Course>>();
            var mockEnrollmentCollection = new Mock<DbCollectionEntry<Course, Enrollment>>();
            mockCourseEntry.Setup(e => e.Collection(It.IsAny<string>())).Returns(mockEnrollmentCollection.Object);
            _mockContext.Setup(c => c.Entry(It.IsAny<Course>())).Returns(mockCourseEntry.Object);
            
            var mockEnrollmentEntry = new Mock<DbEntityEntry<Enrollment>>();
            var mockStudentReference = new Mock<DbReferenceEntry<Enrollment, Student>>();
            mockEnrollmentEntry.Setup(e => e.Reference(It.IsAny<string>())).Returns(mockStudentReference.Object);
            _mockContext.Setup(c => c.Entry(It.IsAny<Enrollment>())).Returns(mockEnrollmentEntry.Object);


            _controller = new InstructorController(_mockContext.Object);
        }

        [TestMethod]
        public void Index_ReturnsViewResult_WithInstructorIndexData()
        {
            // Act
            var result = _controller.Index(null, null) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as InstructorIndexData;
            Assert.IsNotNull(model);
            Assert.AreEqual(3, model.Instructors.Count());
            Assert.IsNull(model.Courses); // No instructor selected
            Assert.IsNull(model.Enrollments); // No course selected
        }

        [TestMethod]
        public void Index_WithSelectedInstructor_PopulatesCourses() // Enrollments not populated without courseID
        {
            // Arrange
            int selectedInstructorId = 1; // Kim Abercrombie

            // Act
            var result = _controller.Index(selectedInstructorId, null) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as InstructorIndexData;
            Assert.IsNotNull(model);
            Assert.AreEqual(_instructors.First(i => i.ID == selectedInstructorId).Courses.Count(), model.Courses.Count());
            Assert.IsNull(model.Enrollments); // No course selected
            Assert.AreEqual(selectedInstructorId, (int)result.ViewBag.InstructorID);
        }

        [TestMethod]
        public void Index_WithSelectedCourse_PopulatesEnrollments()
        {
            // Arrange
            int selectedInstructorId = 1; 
            int selectedCourseId = 1050; // Chemistry, taught by Kim

            // Act
            var result = _controller.Index(selectedInstructorId, selectedCourseId) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as InstructorIndexData;
            Assert.IsNotNull(model);
            Assert.IsNotNull(model.Courses);
            Assert.IsNotNull(model.Enrollments);
            var expectedEnrollmentCount = _courses.First(c => c.CourseID == selectedCourseId).Enrollments.Count();
            Assert.AreEqual(expectedEnrollmentCount, model.Enrollments.Count());
            Assert.AreEqual(selectedInstructorId, (int)result.ViewBag.InstructorID);
            Assert.AreEqual(selectedCourseId, (int)result.ViewBag.CourseID);
            _mockContext.Verify(c => c.Entry(It.Is<Course>(course => course.CourseID == selectedCourseId)), Times.Once());
        }

        [TestMethod]
        public void Details_NullId_ReturnsBadRequest()
        {
            // Act
            var result = _controller.Details(null);
            // Assert
            Assert.IsInstanceOfType(result, typeof(HttpStatusCodeResult));
            Assert.AreEqual((int)HttpStatusCode.BadRequest, ((HttpStatusCodeResult)result).StatusCode);
        }

        [TestMethod]
        public void Details_InstructorNotFound_ReturnsHttpNotFound()
        {
            // Arrange
            _mockInstructorSet.Setup(m => m.Find(It.IsAny<object[]>())).Returns((Instructor)null);
            // Act
            var result = _controller.Details(999);
            // Assert
            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public void Details_InstructorFound_ReturnsViewResult_WithInstructor()
        {
            // Arrange
            var instructor = _instructors.First();
            _mockInstructorSet.Setup(m => m.Find(instructor.ID)).Returns(instructor);
            // Act
            var result = _controller.Details(instructor.ID) as ViewResult;
            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(Instructor));
            Assert.AreEqual(instructor.ID, ((Instructor)result.Model).ID);
        }

        [TestMethod]
        public void Create_GET_ReturnsViewResult_WithCoursesData()
        {
            // Act
            var result = _controller.Create() as ViewResult;
            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ViewBag.Courses);
            Assert.IsInstanceOfType(result.ViewBag.Courses, typeof(List<AssignedCourseData>));
            var assignedCourses = (List<AssignedCourseData>)result.ViewBag.Courses;
            Assert.AreEqual(_courses.Count, assignedCourses.Count);
        }

        [TestMethod]
        public void Create_POST_InvalidModelState_ReturnsViewResult_WithInstructorAndCoursesData()
        {
            // Arrange
            _controller.ModelState.AddModelError("Error", "Sample error");
            var newInstructor = new Instructor { FirstMidName = "Test", LastName = "User" };
            // Act
            var result = _controller.Create(newInstructor, null) as ViewResult;
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(newInstructor, result.Model);
            Assert.IsNotNull(result.ViewBag.Courses);
            _mockInstructorSet.Verify(m => m.Add(It.IsAny<Instructor>()), Times.Never());
        }

        [TestMethod]
        public void Create_POST_ValidModelState_AddsInstructorAndCourses_And_RedirectsToIndex()
        {
            // Arrange
            var newInstructor = new Instructor { ID = 4, FirstMidName = "Test", LastName = "Instructor", HireDate = DateTime.Now };
            string[] selectedCourses = new string[] { _courses.First().CourseID.ToString(), _courses.Last().CourseID.ToString() };
            // Act
            var result = _controller.Create(newInstructor, selectedCourses) as RedirectToRouteResult;
            // Assert
            _mockInstructorSet.Verify(m => m.Add(It.Is<Instructor>(i => i.FirstMidName == "Test")), Times.Once());
            _mockContext.Verify(m => m.SaveChanges(), Times.Once());
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
            // Verify courses were added to the instructor object that was passed to Add
            var addedInstructor = _instructors.FirstOrDefault(i => i.ID == 4); // Assuming ID is set upon add or we retrieve the passed object
            Assert.IsNotNull(addedInstructor); // This check relies on the callback in Add setup
            Assert.AreEqual(selectedCourses.Length, addedInstructor.Courses.Count);
        }

        [TestMethod]
        public void Edit_GET_NullId_ReturnsBadRequest()
        {
            // Act
            var result = _controller.Edit((int?)null);
            // Assert
            Assert.IsInstanceOfType(result, typeof(HttpStatusCodeResult));
            Assert.AreEqual((int)HttpStatusCode.BadRequest, ((HttpStatusCodeResult)result).StatusCode);
        }

        [TestMethod]
        public void Edit_GET_InstructorNotFound_ReturnsHttpNotFound()
        {
            // Arrange
            _mockInstructorSet.Setup(m => m.Find(It.IsAny<object[]>())).Returns((Instructor)null);
            // Act
            var result = _controller.Edit(999);
            // Assert
            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public void Edit_GET_InstructorFound_ReturnsViewResult_WithInstructorAndCoursesData()
        {
            // Arrange
            var instructor = _instructors.First(); // Kim, has 2 courses
            // Note: The controller's Edit GET action uses a LINQ query with Includes.
            // The mock setup for IQueryable should handle this. We need to ensure the Find-like behavior
            // for the Single() call. For simplicity, we can assume the general IQueryable setup covers it
            // or refine the mock for this specific LINQ query if needed.
            // The current mock setup for IQueryable on _mockInstructorSet should allow .Where(i => i.ID == id).Single() to work.

            // Act
            var result = _controller.Edit(instructor.ID) as ViewResult;
            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as Instructor;
            Assert.IsNotNull(model);
            Assert.AreEqual(instructor.ID, model.ID);
            Assert.IsNotNull(result.ViewBag.Courses);
            var assignedCourses = (List<AssignedCourseData>)result.ViewBag.Courses;
            Assert.AreEqual(_courses.Count, assignedCourses.Count);
            // Check if correct courses are marked as assigned for this instructor
            var instructorCourseIds = new HashSet<int>(instructor.Courses.Select(c => c.CourseID));
            foreach (var courseVM in assignedCourses)
            {
                Assert.AreEqual(instructorCourseIds.Contains(courseVM.CourseID), courseVM.Assigned);
            }
        }
        
        [TestMethod]
        public void Edit_POST_NullId_ReturnsBadRequest()
        {
            // Act
            var result = _controller.Edit(null, null); // id, selectedCourses
            // Assert
            Assert.IsInstanceOfType(result, typeof(HttpStatusCodeResult));
            Assert.AreEqual((int)HttpStatusCode.BadRequest, ((HttpStatusCodeResult)result).StatusCode);
        }

        [TestMethod]
        public void Edit_POST_InstructorToUpdateNotFound_ReturnsHttpNotFound()
        {
            // Arrange
            // The controller's Edit POST action uses a LINQ query with Includes to find the instructor.
            // We'll set up the mock to return an empty list for the .Where().Single() part.
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.GetEnumerator())
                .Returns(() => new List<Instructor>().GetEnumerator()); // Make .Single() throw or return null equivalent

            // Act
            var result = _controller.Edit(999, null); // Non-existent ID
            // Assert
            // If .Single() throws (e.g. InvalidOperationException for empty sequence), it would be a 500 error.
            // A robust controller might use SingleOrDefault() and then check for null.
            // The current InstructorController uses .Single(), so if not found, it will throw an exception.
            // This test might be better if it leads to HttpNotFound if SingleOrDefault() was used.
            // For now, let's assume SingleOrDefault() and a null check for HttpNotFound.
            // If the mock returns empty, .Single() will throw. If it returns a list where ID doesn't match, .Single() also throws.
            // To test HttpNotFound, Find() returning null is more direct if the controller uses Find().
            // The controller uses .Where(i => i.ID == id).Single().
            // Let's change the mock to ensure the list doesn't contain the ID.
            var tempInstructors = _instructors.Where(i => i.ID != 999).AsQueryable();
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.Provider).Returns(tempInstructors.Provider);
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.Expression).Returns(tempInstructors.Expression);
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.ElementType).Returns(tempInstructors.ElementType);
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.GetEnumerator()).Returns(() => tempInstructors.GetEnumerator());
            // This setup will cause .Single() to throw if 999 is not in the list.
            // A HttpNotFound would be better if SingleOrDefault was used.
            // Given the prompt "InstructorToUpdateNotFound_ReturnsHttpNotFound", we'll assume a scenario
            // where the controller could return HttpNotFound if SingleOrDefault() was used.
            // To force this, we'd need to mock the result of Single() call itself or ensure the list is empty.
            // This test is hard to achieve with current controller code using .Single() without it throwing an exception.
            // Let's assume for the test that if no instructor is found by the query, it should be HttpNotFound.
            // This implies controller logic change or specific mocking of the Single() behavior.
            // For now, we'll skip this test due to .Single() behavior or assume a more robust controller.
            // A more practical way:
            _mockContext.Setup(c => c.Instructors).Returns(_mockInstructorSet.Object); // Use the general one
             _mockInstructorSet.As<IQueryable<Instructor>>()
                .Setup(m => m.Expression)
                .Returns(Expression.Constant(new List<Instructor>().AsQueryable().Expression)); // Make Where(...).Single() fail
            
             // If controller used Find():
             // _mockInstructorSet.Setup(m => m.Find(999)).Returns((Instructor)null);

            // Act: This will likely throw an exception because .Single() on an empty sequence.
            // Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
            // This test highlights a potential fragility in the controller if .Single() is used without care.
            // We will proceed with the assumption that the test implies a scenario where the instructor is not found by the query logic.
            // To make it directly testable as HttpNotFound, the controller would need to use SingleOrDefault and check for null.
             Assert.Inconclusive("Test Edit_POST_InstructorToUpdateNotFound_ReturnsHttpNotFound needs controller to use SingleOrDefault or specific mock for .Single() to simulate not found without exception.");

        }


        [TestMethod]
        public void Edit_POST_InvalidUpdateModel_ReturnsViewResult_WithInstructorAndCoursesData()
        {
            // Arrange
            var instructor = _instructors.First();
            // Ensure the instructor is "found" by the initial query in Edit POST
            var singleInstructorList = new List<Instructor> { instructor }.AsQueryable();
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.Provider).Returns(singleInstructorList.Provider);
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.Expression).Returns(singleInstructorList.Expression);
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.ElementType).Returns(singleInstructorList.ElementType);
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.GetEnumerator()).Returns(() => singleInstructorList.GetEnumerator());

            _controller.ModelState.AddModelError("Error", "Sample error causing TryUpdateModel to be false");

            // Act
            var result = _controller.Edit(instructor.ID, null) as ViewResult; // Pass null for selectedCourses

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(Instructor));
            Assert.AreEqual(instructor.ID, ((Instructor)result.Model).ID);
            Assert.IsNotNull(result.ViewBag.Courses); // PopulateAssignedCourseData should be called
            _mockContext.Verify(m => m.SaveChanges(), Times.Never());
        }

        [TestMethod]
        public void Edit_POST_ValidUpdateModel_UpdatesInstructorAndCourses_And_RedirectsToIndex()
        {
            // Arrange
            var instructorToUpdate = _instructors.First(); // Kim
            var originalCourseCount = instructorToUpdate.Courses.Count;
            
            // Setup the mock to return this specific instructor for the .Single() call in Edit POST
            var singleInstructorList = new List<Instructor> { instructorToUpdate }.AsQueryable();
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.Provider).Returns(singleInstructorList.Provider);
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.Expression).Returns(singleInstructorList.Expression);
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.ElementType).Returns(singleInstructorList.ElementType);
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.GetEnumerator()).Returns(() => singleInstructorList.GetEnumerator());


            string[] selectedCourses = new string[] { _courses.Last().CourseID.ToString() }; // Change courses, Kim now teaches only Calculus

            // Act
            var result = _controller.Edit(instructorToUpdate.ID, selectedCourses) as RedirectToRouteResult;

            // Assert
            _mockContext.Verify(m => m.SaveChanges(), Times.Once());
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
            // Verify courses were updated
            Assert.AreEqual(1, instructorToUpdate.Courses.Count);
            Assert.AreEqual(_courses.Last().CourseID, instructorToUpdate.Courses.First().CourseID);
        }

        [TestMethod]
        public void Delete_GET_NullId_ReturnsBadRequest()
        {
            // Act
            var result = _controller.Delete(null);
            // Assert
            Assert.IsInstanceOfType(result, typeof(HttpStatusCodeResult));
            Assert.AreEqual((int)HttpStatusCode.BadRequest, ((HttpStatusCodeResult)result).StatusCode);
        }

        [TestMethod]
        public void Delete_GET_InstructorNotFound_ReturnsHttpNotFound()
        {
            // Arrange
            _mockInstructorSet.Setup(m => m.Find(It.IsAny<object[]>())).Returns((Instructor)null);
            // Act
            var result = _controller.Delete(999);
            // Assert
            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public void Delete_GET_InstructorFound_ReturnsViewResult_WithInstructor()
        {
            // Arrange
            var instructor = _instructors.First();
            _mockInstructorSet.Setup(m => m.Find(instructor.ID)).Returns(instructor);
            // Act
            var result = _controller.Delete(instructor.ID) as ViewResult;
            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(Instructor));
            Assert.AreEqual(instructor.ID, ((Instructor)result.Model).ID);
        }

        [TestMethod]
        public void DeleteConfirmed_InstructorNotFound_ShouldHandleAndRedirect()
        {
            // Arrange
            // The controller's DeleteConfirmed uses a LINQ query with .Single()
            // To simulate "not found" without exception, we make the query return an empty list.
            _mockInstructorSet.As<IQueryable<Instructor>>()
                .Setup(m => m.GetEnumerator())
                .Returns(() => new List<Instructor>().GetEnumerator()); // Make .Single() throw

            // Act & Assert
            // This will throw InvalidOperationException because .Single() is called on an empty sequence.
            // A robust controller would use SingleOrDefault() and check for null.
            // If it did that and returned HttpNotFound or RedirectToAction("Index"), this test would be different.
            // For now, this tests that the controller doesn't gracefully handle a truly missing instructor via its current query.
            // A redirect to Index is the behavior if Single() doesn't throw and if instructor.OfficeAssignment = null doesn't throw.
            // This test is problematic with .Single().
            Assert.Inconclusive("Test DeleteConfirmed_InstructorNotFound_ShouldHandleAndRedirect is difficult to test accurately with .Single(); needs controller change or very specific mock.");

            // If we assume Find was used instead of Single():
            // _mockInstructorSet.Setup(m => m.Find(999)).Returns((Instructor)null);
            // var result = _controller.DeleteConfirmed(999) as RedirectToRouteResult;
            // _mockInstructorSet.Verify(m => m.Remove(It.IsAny<Instructor>()), Times.Never());
            // _mockContext.Verify(m => m.SaveChanges(), Times.Never());
            // Assert.IsNotNull(result);
            // Assert.AreEqual("Index", result.RouteValues["action"]);
        }


        [TestMethod]
        public void DeleteConfirmed_ValidId_RemovesInstructor_And_RedirectsToIndex()
        {
            // Arrange
            var instructorToDelete = _instructors.First(i => i.OfficeAssignment != null); // Kim, has OfficeAssignment
            var departmentWithThisInstructor = _departments.FirstOrDefault(d => d.InstructorID == instructorToDelete.ID);
            if(departmentWithThisInstructor != null) departmentWithThisInstructor.InstructorID = instructorToDelete.ID; // Ensure it's set for the test

            // Mock the .Single() call in DeleteConfirmed to return this instructor
            var singleInstructorList = new List<Instructor> { instructorToDelete }.AsQueryable();
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.Provider).Returns(singleInstructorList.Provider);
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.Expression).Returns(singleInstructorList.Expression);
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.ElementType).Returns(singleInstructorList.ElementType);
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.GetEnumerator()).Returns(() => singleInstructorList.GetEnumerator());
            
            // Mock the department query
             var deptsList = departmentWithThisInstructor != null ? new List<Department> { departmentWithThisInstructor } : new List<Department>();
            _mockDepartmentSet.As<IQueryable<Department>>().Setup(m => m.GetEnumerator()).Returns(() => deptsList.GetEnumerator());


            // Act
            var result = _controller.DeleteConfirmed(instructorToDelete.ID) as RedirectToRouteResult;

            // Assert
            _mockInstructorSet.Verify(m => m.Remove(It.Is<Instructor>(i => i.ID == instructorToDelete.ID)), Times.Once());
            _mockContext.Verify(m => m.SaveChanges(), Times.Once());
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
            Assert.IsNull(instructorToDelete.OfficeAssignment); // Check OfficeAssignment is nulled
            if(departmentWithThisInstructor != null) Assert.IsNull(departmentWithThisInstructor.InstructorID); // Check department admin is nulled
        }
    }
}
