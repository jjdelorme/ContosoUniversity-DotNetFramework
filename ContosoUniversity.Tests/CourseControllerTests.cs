using Microsoft.VisualStudio.TestTools.UnitTesting;
using ContosoUniversity.Controllers;
using ContosoUniversity.DAL;
using ContosoUniversity.Models;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Net; // For HttpStatusCodeResult
using System.Data.Entity; // For IDbSet and mocking DbSet features
using System.Data.Entity.Infrastructure; // For DbEntityEntry, DatabaseFacade
using System; // For DateTime

namespace ContosoUniversity.Tests
{
    [TestClass]
    public class CourseControllerTests
    {
        private Mock<ISchoolContext> _mockContext;
        private Mock<IDbSet<Course>> _mockCourseSet;
        private Mock<IDbSet<Department>> _mockDepartmentSet;
        private List<Course> _courses;
        private List<Department> _departments;
        private CourseController _controller;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockContext = new Mock<ISchoolContext>();
            _mockCourseSet = new Mock<IDbSet<Course>>();
            _mockDepartmentSet = new Mock<IDbSet<Department>>();

            _departments = new List<Department>
            {
                new Department { DepartmentID = 1, Name = "Engineering", Budget = 350000, StartDate = DateTime.Parse("2007-09-01") },
                new Department { DepartmentID = 2, Name = "Mathematics", Budget = 100000, StartDate = DateTime.Parse("2007-09-01") }
            };

            _courses = new List<Course>
            {
                new Course { CourseID = 1050, Title = "Chemistry", Credits = 3, DepartmentID = 1, Department = _departments.First(d => d.DepartmentID == 1) },
                new Course { CourseID = 4022, Title = "Microeconomics", Credits = 3, DepartmentID = 2, Department = _departments.First(d => d.DepartmentID == 2) },
                new Course { CourseID = 4041, Title = "Macroeconomics", Credits = 3, DepartmentID = 2, Department = _departments.First(d => d.DepartmentID == 2) }
            };

            var coursesData = _courses.AsQueryable();
            _mockCourseSet.As<IQueryable<Course>>().Setup(m => m.Provider).Returns(coursesData.Provider);
            _mockCourseSet.As<IQueryable<Course>>().Setup(m => m.Expression).Returns(coursesData.Expression);
            _mockCourseSet.As<IQueryable<Course>>().Setup(m => m.ElementType).Returns(coursesData.ElementType);
            _mockCourseSet.As<IQueryable<Course>>().Setup(m => m.GetEnumerator()).Returns(() => coursesData.GetEnumerator());
            _mockCourseSet.Setup(m => m.Find(It.IsAny<object[]>()))
                .Returns<object[]>(ids => _courses.FirstOrDefault(c => c.CourseID == (int)ids[0]));
            _mockCourseSet.Setup(m => m.Add(It.IsAny<Course>()))
                .Callback<Course>(c => _courses.Add(c));
            _mockCourseSet.Setup(m => m.Remove(It.IsAny<Course>()))
                .Callback<Course>(c => 
                {
                    var courseToRemove = _courses.FirstOrDefault(course => course.CourseID == c.CourseID);
                    if (courseToRemove != null) _courses.Remove(courseToRemove);
                });


            var departmentsData = _departments.AsQueryable();
            _mockDepartmentSet.As<IQueryable<Department>>().Setup(m => m.Provider).Returns(departmentsData.Provider);
            _mockDepartmentSet.As<IQueryable<Department>>().Setup(m => m.Expression).Returns(departmentsData.Expression);
            _mockDepartmentSet.As<IQueryable<Department>>().Setup(m => m.ElementType).Returns(departmentsData.ElementType);
            _mockDepartmentSet.As<IQueryable<Department>>().Setup(m => m.GetEnumerator()).Returns(() => departmentsData.GetEnumerator());

            _mockContext.Setup(c => c.Courses).Returns(_mockCourseSet.Object);
            _mockContext.Setup(c => c.Departments).Returns(_mockDepartmentSet.Object);
            _mockContext.Setup(c => c.SaveChanges()).Returns(1);

            var mockDbEntry = new Mock<DbEntityEntry<Course>>();
            // It's tricky to mock the State property directly for assignment if it's not virtual or abstract.
            // For many scenarios, simply returning a mock DbEntityEntry is enough.
            // If State needs to be verified, a more complex mock or a test double might be needed.
            _mockContext.Setup(c => c.Entry(It.IsAny<Course>())).Returns(mockDbEntry.Object);

            var mockDatabaseFacade = new Mock<DatabaseFacade>(null); // Pass null for DbContext if not needed for interactions
            mockDatabaseFacade.Setup(db => db.ExecuteSqlCommand(It.IsAny<string>(), It.IsAny<object[]>())).Returns(1);
            _mockContext.Setup(c => c.Database).Returns(mockDatabaseFacade.Object);

            _controller = new CourseController(_mockContext.Object);
        }

        [TestMethod]
        public void Index_ReturnsViewResult_WithListOfCourses()
        {
            // Act
            var result = _controller.Index(null) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as IEnumerable<Course>;
            Assert.IsNotNull(model);
            Assert.AreEqual(3, model.Count());
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
        public void Details_CourseNotFound_ReturnsHttpNotFound()
        {
            // Arrange
            _mockCourseSet.Setup(m => m.Find(It.IsAny<object[]>())).Returns((Course)null);

            // Act
            var result = _controller.Details(999); // Non-existent ID

            // Assert
            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public void Details_CourseFound_ReturnsViewResult_WithCourse()
        {
            // Arrange
            var course = _courses.First();
             _mockCourseSet.Setup(m => m.Find(course.CourseID)).Returns(course);


            // Act
            var result = _controller.Details(course.CourseID) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(Course));
            Assert.AreEqual(course.CourseID, ((Course)result.Model).CourseID);
        }

        [TestMethod]
        public void Create_GET_ReturnsViewResult_WithDepartmentsInViewBag()
        {
            // Act
            var result = _controller.Create() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.ViewBag.DepartmentID, typeof(SelectList));
            var selectList = (SelectList)result.ViewBag.DepartmentID;
            Assert.AreEqual(_departments.Count, selectList.Items.Cast<object>().Count());
        }

        [TestMethod]
        public void Create_POST_InvalidModelState_ReturnsViewResult_WithCourseAndDepartmentsInViewBag()
        {
            // Arrange
            _controller.ModelState.AddModelError("Error", "Sample error");
            var newCourse = new Course { Title = "Test Course", Credits = 3, DepartmentID = 1 };

            // Act
            var result = _controller.Create(newCourse) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(newCourse, result.Model);
            Assert.IsInstanceOfType(result.ViewBag.DepartmentID, typeof(SelectList));
            _mockCourseSet.Verify(m => m.Add(It.IsAny<Course>()), Times.Never());
        }

        [TestMethod]
        public void Create_POST_ValidModelState_AddsCourse_And_RedirectsToIndex()
        {
            // Arrange
            var newCourse = new Course { CourseID = 100, Title = "New Course", Credits = 4, DepartmentID = 1 };

            // Act
            var result = _controller.Create(newCourse) as RedirectToRouteResult;

            // Assert
            _mockCourseSet.Verify(m => m.Add(It.Is<Course>(c => c.Title == "New Course")), Times.Once());
            _mockContext.Verify(m => m.SaveChanges(), Times.Once());
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
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
        public void Edit_GET_CourseNotFound_ReturnsHttpNotFound()
        {
            // Arrange
            _mockCourseSet.Setup(m => m.Find(It.IsAny<object[]>())).Returns((Course)null);
            // Act
            var result = _controller.Edit(999); // Non-existent ID
            // Assert
            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public void Edit_GET_CourseFound_ReturnsViewResult_WithCourseAndDepartmentsInViewBag()
        {
            // Arrange
            var course = _courses.First();
            _mockCourseSet.Setup(m => m.Find(course.CourseID)).Returns(course);

            // Act
            var result = _controller.Edit(course.CourseID) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(Course));
            Assert.AreEqual(course.CourseID, ((Course)result.Model).CourseID);
            Assert.IsInstanceOfType(result.ViewBag.DepartmentID, typeof(SelectList));
        }

        [TestMethod]
        public void Edit_POST_InvalidModelState_ReturnsViewResult_WithCourseAndDepartmentsInViewBag()
        {
            // Arrange
            var course = _courses.First();
            // The controller's EditPost(int? id) first calls Find(id).
            // Then TryUpdateModel(courseToUpdate, ...)
            // If ModelState is invalid, TryUpdateModel is often not called, or if it is, it fails.
            // The controller should return the view with the original course and populated dropdown.
             _mockCourseSet.Setup(m => m.Find(course.CourseID)).Returns(course); // Ensure Find returns the course
            _controller.ModelState.AddModelError("Error", "Sample error");

            // Act
            // The CourseController.EditPost(int? id) takes an ID, finds the course, then TryUpdateModel.
            // We're testing the case where TryUpdateModel fails due to invalid model state.
            // We don't pass the course object directly to EditPost as it's not the action's signature.
            var result = _controller.EditPost(course.CourseID) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(course, result.Model); // Should be the original course model
            Assert.IsInstanceOfType(result.ViewBag.DepartmentID, typeof(SelectList));
            _mockContext.Verify(m => m.SaveChanges(), Times.Never());
        }
        
        [TestMethod]
        public void Edit_POST_ValidModel_UpdatesCourse_And_RedirectsToIndex()
        {
            // Arrange
            var course = _courses.First(); // CourseID = 1050
            _mockCourseSet.Setup(m => m.Find(course.CourseID)).Returns(course);
            // Simulate TryUpdateModel being successful. The controller will then call SaveChanges.

            // Act
            // The controller's EditPost(int? id) is called.
            // TryUpdateModel is assumed to be successful by the controller logic if ModelState is valid.
            var result = _controller.EditPost(course.CourseID) as RedirectToRouteResult;

            // Assert
            _mockContext.Verify(m => m.SaveChanges(), Times.Once());
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
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
        public void Delete_GET_CourseNotFound_ReturnsHttpNotFound()
        {
            // Arrange
            _mockCourseSet.Setup(m => m.Find(It.IsAny<object[]>())).Returns((Course)null);
            // Act
            var result = _controller.Delete(999); // Non-existent ID
            // Assert
            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public void Delete_GET_CourseFound_ReturnsViewResult_WithCourse()
        {
            // Arrange
            var course = _courses.First();
            _mockCourseSet.Setup(m => m.Find(course.CourseID)).Returns(course);

            // Act
            var result = _controller.Delete(course.CourseID) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(Course));
            Assert.AreEqual(course.CourseID, ((Course)result.Model).CourseID);
        }

        [TestMethod]
        public void DeleteConfirmed_CourseNotFound_ShouldNotThrow_And_RedirectsToIndex()
        {
            // Arrange
            _mockCourseSet.Setup(m => m.Find(It.IsAny<object[]>())).Returns((Course)null);
            
            // Act
            // The CourseController.DeleteConfirmed(int id) calls Find(id) then Remove(course).
            // If course is null, Remove(null) would throw ArgumentNullException for EF.
            // A robust controller should check for null. Assuming current controller might throw or handle.
            // If it handles by not calling Remove(null), then SaveChanges shouldn't be called.
            var result = _controller.DeleteConfirmed(999) as RedirectToRouteResult;

            // Assert
            _mockCourseSet.Verify(m => m.Remove(It.IsAny<Course>()), Times.Never()); // Or specific check for Remove(null)
            _mockContext.Verify(m => m.SaveChanges(), Times.Never());
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
        }

        [TestMethod]
        public void DeleteConfirmed_ValidId_RemovesCourse_And_RedirectsToIndex()
        {
            // Arrange
            var courseToDelete = _courses.First(); // ID = 1050
            _mockCourseSet.Setup(m => m.Find(courseToDelete.CourseID)).Returns(courseToDelete);

            // Act
            var result = _controller.DeleteConfirmed(courseToDelete.CourseID) as RedirectToRouteResult;

            // Assert
            _mockCourseSet.Verify(m => m.Remove(It.Is<Course>(c => c.CourseID == courseToDelete.CourseID)), Times.Once());
            _mockContext.Verify(m => m.SaveChanges(), Times.Once());
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
            Assert.IsFalse(_courses.Any(c => c.CourseID == courseToDelete.CourseID));
        }

        [TestMethod]
        public void UpdateCourseCredits_GET_ReturnsViewResult()
        {
            // Act
            var result = _controller.UpdateCourseCredits() as ViewResult;
            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void UpdateCourseCredits_POST_CallsExecuteSqlCommand_And_ReturnsView()
        {
            // Arrange
            int multiplier = 2;
            // The controller's UpdateCourseCredits(int? multiplier) calls:
            // db.Database.ExecuteSqlCommand("UPDATE Course SET Credits = Credits * {0}", multiplier);
            // And then returns View(); It does not call SaveChanges() in the provided code snippet for CourseController.
            // Nor does it redirect.

            // Act
            var result = _controller.UpdateCourseCredits(multiplier) as ViewResult;

            // Assert
            _mockContext.Verify(m => m.Database.ExecuteSqlCommand(It.IsAny<string>(), multiplier), Times.Once());
            // _mockContext.Verify(m => m.SaveChanges(), Times.Never()); // Original controller does not call SaveChanges here.
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ViewBag.RowsAffected); // Check if ViewBag.RowsAffected is set
        }
    }
}
