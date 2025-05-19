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
using System.Data.Entity.Infrastructure; // For DbEntityEntry, DatabaseFacade, DbUpdateConcurrencyException
using System; // For DateTime
using System.Threading.Tasks; // For async controller actions

namespace ContosoUniversity.Tests
{
    [TestClass]
    public class DepartmentControllerTests
    {
        private Mock<ISchoolContext> _mockContext;
        private Mock<IDbSet<Department>> _mockDepartmentSet;
        private Mock<IDbSet<Instructor>> _mockInstructorSet; // For ViewBag.InstructorID in Create/Edit GET
        private List<Department> _departments;
        private List<Instructor> _instructors;
        private DepartmentController _controller;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockContext = new Mock<ISchoolContext>();
            _mockDepartmentSet = new Mock<IDbSet<Department>>();
            _mockInstructorSet = new Mock<IDbSet<Instructor>>();

            _instructors = new List<Instructor>
            {
                new Instructor { ID = 1, FirstMidName = "Kim", LastName = "Abercrombie", HireDate = DateTime.Parse("1995-03-11") },
                new Instructor { ID = 2, FirstMidName = "Fadi", LastName = "Fakhouri", HireDate = DateTime.Parse("2002-07-06") }
            };
            
            var instructorsData = _instructors.AsQueryable();
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.Provider).Returns(instructorsData.Provider);
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.Expression).Returns(instructorsData.Expression);
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.ElementType).Returns(instructorsData.ElementType);
            _mockInstructorSet.As<IQueryable<Instructor>>().Setup(m => m.GetEnumerator()).Returns(() => instructorsData.GetEnumerator());
             _mockInstructorSet.Setup(m => m.Find(It.IsAny<object[]>()))
                .Returns<object[]>(ids => _instructors.FirstOrDefault(i => i.ID == (int)ids[0]));


            _departments = new List<Department>
            {
                new Department { DepartmentID = 1, Name = "Engineering", Budget = 350000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = 1, Administrator = _instructors.First(i=>i.ID==1), RowVersion = new byte[] { 1, 0, 0, 0 } },
                new Department { DepartmentID = 2, Name = "Mathematics", Budget = 100000, StartDate = DateTime.Parse("2007-09-01"), InstructorID = 2, Administrator = _instructors.First(i=>i.ID==2), RowVersion = new byte[] { 1, 0, 0, 0 } },
                new Department { DepartmentID = 3, Name = "English", Budget = 120000, StartDate = DateTime.Parse("2007-09-01"), RowVersion = new byte[] { 1, 0, 0, 0 } }
            };

            var departmentsData = _departments.AsQueryable();
            _mockDepartmentSet.As<IQueryable<Department>>().Setup(m => m.Provider).Returns(departmentsData.Provider);
            _mockDepartmentSet.As<IQueryable<Department>>().Setup(m => m.Expression).Returns(departmentsData.Expression);
            _mockDepartmentSet.As<IQueryable<Department>>().Setup(m => m.ElementType).Returns(departmentsData.ElementType);
            _mockDepartmentSet.As<IQueryable<Department>>().Setup(m => m.GetEnumerator()).Returns(() => departmentsData.GetEnumerator());
            _mockDepartmentSet.Setup(m => m.FindAsync(It.IsAny<object[]>()))
                .Returns<object[]>(ids => Task.FromResult(_departments.FirstOrDefault(d => d.DepartmentID == (int)ids[0])));
            _mockDepartmentSet.Setup(m => m.Add(It.IsAny<Department>()))
                .Callback<Department>(d => _departments.Add(d));
            _mockDepartmentSet.Setup(m => m.Remove(It.IsAny<Department>()))
                .Callback<Department>(d => 
                {
                    var depToRemove = _departments.FirstOrDefault(dep => dep.DepartmentID == d.DepartmentID);
                    if (depToRemove != null) _departments.Remove(depToRemove);
                });
            
            // Mock for raw SQL query in Details action
            _mockDepartmentSet.Setup(m => m.SqlQuery(It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns<string, object[]>((query, parameters) => 
                {
                    var id = (int)parameters[0];
                    var result = _departments.Where(d => d.DepartmentID == id).ToList();
                    // SqlQuery is expected to return a DbRawSqlQuery<Department>, which can be enumerated.
                    // For simplicity in mocking, we'll wrap our List in a mock that behaves similarly for ToListAsync/SingleOrDefaultAsync.
                    var mockDbRawSqlQuery = new Mock<DbRawSqlQuery<Department>>();
                    mockDbRawSqlQuery.Setup(x => x.GetEnumerator()).Returns(result.GetEnumerator());
                    mockDbRawSqlQuery.Setup(x => x.SingleOrDefaultAsync()).Returns(Task.FromResult(result.SingleOrDefault()));
                    mockDbRawSqlQuery.Setup(x => x.ToListAsync()).Returns(Task.FromResult(result));
                    return mockDbRawSqlQuery.Object;
                });


            _mockContext.Setup(c => c.Departments).Returns(_mockDepartmentSet.Object);
            _mockContext.Setup(c => c.Instructors).Returns(_mockInstructorSet.Object); // For ViewBag
            _mockContext.Setup(c => c.SaveChangesAsync()).Returns(Task.FromResult(1));

            var mockEntry = new Mock<DbEntityEntry<Department>>();
            var mockPropertyValues = new Mock<DbPropertyValues>();
            mockPropertyValues.Setup(p => p["RowVersion"]).Returns(new byte[] { 1, 2, 3, 4 });
            mockEntry.Setup(e => e.OriginalValues).Returns(mockPropertyValues.Object);
            mockEntry.SetupProperty(e => e.State, EntityState.Unchanged); // Allow State to be set and get
            
            _mockContext.Setup(c => c.Entry(It.IsAny<Department>())).Returns(mockEntry.Object);
            
            var mockDatabaseFacade = new Mock<DatabaseFacade>(null);
            _mockContext.Setup(c => c.Database).Returns(mockDatabaseFacade.Object);

            _controller = new DepartmentController(_mockContext.Object);
        }

        [TestMethod]
        public async Task Index_ReturnsViewResult_WithListOfDepartments()
        {
            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as IEnumerable<Department>;
            Assert.IsNotNull(model);
            Assert.AreEqual(3, model.Count());
        }

        [TestMethod]
        public async Task Details_NullId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.Details(null);

            // Assert
            Assert.IsInstanceOfType(result, typeof(HttpStatusCodeResult));
            Assert.AreEqual((int)HttpStatusCode.BadRequest, ((HttpStatusCodeResult)result).StatusCode);
        }

        [TestMethod]
        public async Task Details_DepartmentNotFound_ReturnsHttpNotFound()
        {
            // Arrange
            // Modifying the SqlQuery mock to return an empty list for a specific non-existent ID
             _mockDepartmentSet.Setup(m => m.SqlQuery(It.IsAny<string>(), 999))
                .Returns(() => {
                    var emptyResult = new List<Department>().AsQueryable();
                    var mockDbRawSqlQuery = new Mock<DbRawSqlQuery<Department>>();
                    mockDbRawSqlQuery.Setup(x => x.SingleOrDefaultAsync()).Returns(Task.FromResult(emptyResult.SingleOrDefault()));
                    return mockDbRawSqlQuery.Object;
                });


            // Act
            var result = await _controller.Details(999);

            // Assert
            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public async Task Details_DepartmentFound_ReturnsViewResult_WithDepartment()
        {
            // Arrange
            var department = _departments.First();
            // The general SqlQuery mock in TestInitialize should handle finding the department

            // Act
            var result = await _controller.Details(department.DepartmentID) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(Department));
            Assert.AreEqual(department.DepartmentID, ((Department)result.Model).DepartmentID);
        }

        [TestMethod]
        public void Create_GET_ReturnsViewResult()
        {
            // Act
            var result = _controller.Create() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.ViewBag.InstructorID, typeof(SelectList));
        }

        [TestMethod]
        public async Task Create_POST_InvalidModelState_ReturnsViewResult_WithDepartment()
        {
            // Arrange
            _controller.ModelState.AddModelError("Error", "Sample error");
            var newDepartment = new Department { Name = "Test Dept", Budget = 1000, StartDate = DateTime.Now };

            // Act
            var result = await _controller.Create(newDepartment) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(newDepartment, result.Model);
            _mockDepartmentSet.Verify(m => m.Add(It.IsAny<Department>()), Times.Never());
            Assert.IsInstanceOfType(result.ViewBag.InstructorID, typeof(SelectList));
        }

        [TestMethod]
        public async Task Create_POST_ValidModelState_AddsDepartment_And_RedirectsToIndex()
        {
            // Arrange
            var newDepartment = new Department { DepartmentID = 4, Name = "Physics", Budget = 75000, StartDate = DateTime.Now, InstructorID = 1 };

            // Act
            var result = await _controller.Create(newDepartment) as RedirectToRouteResult;

            // Assert
            _mockDepartmentSet.Verify(m => m.Add(It.Is<Department>(d => d.Name == "Physics")), Times.Once());
            _mockContext.Verify(m => m.SaveChangesAsync(), Times.Once());
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
        }
        
        [TestMethod]
        public async Task Edit_GET_NullId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.Edit((int?)null);
            // Assert
            Assert.IsInstanceOfType(result, typeof(HttpStatusCodeResult));
            Assert.AreEqual((int)HttpStatusCode.BadRequest, ((HttpStatusCodeResult)result).StatusCode);
        }

        [TestMethod]
        public async Task Edit_GET_DepartmentNotFound_ReturnsHttpNotFound()
        {
            // Arrange
            _mockDepartmentSet.Setup(m => m.FindAsync(It.IsAny<object[]>())).Returns(Task.FromResult((Department)null));
            // Act
            var result = await _controller.Edit(999); // Non-existent ID
            // Assert
            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public async Task Edit_GET_DepartmentFound_ReturnsViewResult_WithDepartment()
        {
            // Arrange
            var department = _departments.First();
            _mockDepartmentSet.Setup(m => m.FindAsync(department.DepartmentID)).Returns(Task.FromResult(department));

            // Act
            var result = await _controller.Edit(department.DepartmentID) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(Department));
            Assert.AreEqual(department.DepartmentID, ((Department)result.Model).DepartmentID);
            Assert.IsInstanceOfType(result.ViewBag.InstructorID, typeof(SelectList));
        }

        [TestMethod]
        public async Task Edit_POST_DepartmentToUpdateNotFound_ReturnsHttpNotFound()
        {
            // Arrange
            _mockDepartmentSet.Setup(m => m.FindAsync(It.IsAny<object[]>())).Returns(Task.FromResult((Department)null));
            byte[] rowVersion = new byte[] { 1, 0, 0, 0 }; // Example row version

            // Act
            var result = await _controller.Edit(999, rowVersion); // Non-existent ID

            // Assert
            // The controller logic for a not found department during POST Edit:
            // var departmentToUpdate = await db.Departments.FindAsync(id);
            // if (departmentToUpdate == null) { ... return View(deletedDepartment); }
            // This means it won't be HttpNotFoundResult, but a ViewResult with a potentially new Department model.
            // Let's test this specific behavior.
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var viewResult = (ViewResult)result;
            Assert.IsInstanceOfType(viewResult.Model, typeof(Department)); // It's a 'deletedDepartment'
            Assert.IsTrue(_controller.ModelState.ContainsKey(string.Empty)); // Check for general error message
        }

        [TestMethod]
        public async Task Edit_POST_InvalidModelState_ReturnsViewResult_WithDepartment()
        {
            // Arrange
            var department = _departments.First();
            _mockDepartmentSet.Setup(m => m.FindAsync(department.DepartmentID)).Returns(Task.FromResult(department));
            _controller.ModelState.AddModelError("Error", "Sample error");
            byte[] rowVersion = department.RowVersion;


            // Act
            var result = await _controller.Edit(department.DepartmentID, rowVersion) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(department, result.Model);
            Assert.IsInstanceOfType(result.ViewBag.InstructorID, typeof(SelectList));
            _mockContext.Verify(m => m.SaveChangesAsync(), Times.Never());
        }
        
        [TestMethod]
        public async Task Edit_POST_ValidModel_UpdatesDepartment_And_RedirectsToIndex()
        {
            // Arrange
            var department = _departments.First();
            _mockDepartmentSet.Setup(m => m.FindAsync(department.DepartmentID)).Returns(Task.FromResult(department));
            // Simulate TryUpdateModel being successful.
            byte[] rowVersion = department.RowVersion;

            // Act
            var result = await _controller.Edit(department.DepartmentID, rowVersion) as RedirectToRouteResult;

            // Assert
            _mockContext.Verify(m => m.SaveChangesAsync(), Times.Once());
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
        }
        
        [TestMethod]
        public async Task Edit_POST_ConcurrencyException_AddModelError_And_ReturnsViewResult()
        {
            // Arrange
            var department = _departments.First(); // DepartmentID = 1
            var originalRowVersion = department.RowVersion; // e.g., {1,0,0,0}
            var clientSubmittedRowVersion = new byte[] {0,0,0,0}; // Different RowVersion to simulate client having old data

            _mockDepartmentSet.Setup(m => m.FindAsync(department.DepartmentID)).Returns(Task.FromResult(department));
            
            // Simulate DbUpdateConcurrencyException
            // The exception needs to contain the entity that caused the error.
            var entryMock = new Mock<DbEntityEntry<Department>>();
            entryMock.SetupGet(e => e.Entity).Returns(department); // This is the client's version
            
            // Simulate database having newer values
            var dbValuesDepartment = new Department { DepartmentID = department.DepartmentID, Name = "Engineering DB", Budget = 400000, StartDate = department.StartDate, InstructorID = department.InstructorID, RowVersion = new byte[] {2,0,0,0}};
            var dbPropertyValuesMock = new Mock<DbPropertyValues>();
            dbPropertyValuesMock.Setup(p => p.ToObject()).Returns(dbValuesDepartment);
            entryMock.Setup(e => e.GetDatabaseValues()).Returns(dbPropertyValuesMock.Object);


            _mockContext.Setup(c => c.SaveChangesAsync())
                .ThrowsAsync(new DbUpdateConcurrencyException("Concurrency error", new List<DbEntityEntry> { entryMock.Object }));

            _mockContext.Setup(c => c.Entry(department)).Returns(entryMock.Object); // Ensure Entry returns the mock with DB values setup

            // Act
            var result = await _controller.Edit(department.DepartmentID, clientSubmittedRowVersion) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(Department));
            var model = (Department)result.Model;

            Assert.IsTrue(_controller.ModelState.ContainsKey(string.Empty)); // General concurrency error
            Assert.IsTrue(_controller.ModelState.ContainsKey("Name")); // Specific field error
            Assert.AreEqual(dbValuesDepartment.RowVersion, model.RowVersion); // Model should be updated with DB's RowVersion
            Assert.IsInstanceOfType(result.ViewBag.InstructorID, typeof(SelectList));
        }


        [TestMethod]
        public async Task Delete_GET_NullId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.Delete(null, null);
            // Assert
            Assert.IsInstanceOfType(result, typeof(HttpStatusCodeResult));
            Assert.AreEqual((int)HttpStatusCode.BadRequest, ((HttpStatusCodeResult)result).StatusCode);
        }

        [TestMethod]
        public async Task Delete_GET_DepartmentNotFound_ReturnsHttpNotFound()
        {
            // Arrange
            _mockDepartmentSet.Setup(m => m.FindAsync(It.IsAny<object[]>())).Returns(Task.FromResult((Department)null));
            // Act
            var result = await _controller.Delete(999, null); // Non-existent ID
            // Assert
            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public async Task Delete_GET_SaveChangesError_SetsErrorMessage_And_ReturnsViewResult()
        {
            // Arrange
            var department = _departments.First();
            _mockDepartmentSet.Setup(m => m.FindAsync(department.DepartmentID)).Returns(Task.FromResult(department));

            // Act
            var result = await _controller.Delete(department.DepartmentID, true) as ViewResult; // saveChangesError = true

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(Department));
            Assert.IsNotNull(result.ViewBag.ConcurrencyErrorMessage); // DepartmentController uses ConcurrencyErrorMessage
        }

        [TestMethod]
        public async Task Delete_GET_DepartmentFound_ReturnsViewResult_WithDepartment()
        {
            // Arrange
            var department = _departments.First();
            _mockDepartmentSet.Setup(m => m.FindAsync(department.DepartmentID)).Returns(Task.FromResult(department));

            // Act
            var result = await _controller.Delete(department.DepartmentID, null) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Model, typeof(Department));
            Assert.AreEqual(department.DepartmentID, ((Department)result.Model).DepartmentID);
        }

        [TestMethod]
        public async Task DeleteConfirmed_DepartmentNotFound_Should_Not_Throw_And_Redirect()
        {
            // Arrange
            // The controller's Delete(Department department) action is called.
            // If FindAsync in the GET action returned null, it would be HttpNotFound.
            // This DeleteConfirmed takes a Department object.
            // The controller code: db.Entry(department).State = EntityState.Deleted;
            // If department is somehow null here (not typical for this action signature), Entry(null) would throw.
            // Assuming 'department' object is passed, but refers to an ID that SaveChanges might find non-existent (concurrency).
            // This test seems to overlap with DeleteConfirmed_ConcurrencyException_RedirectsToDelete_WithError if the cause is not found during SaveChanges.
            // The original instruction was: _mockDepartmentSet.Setup(m => m.Find(It.IsAny<object[]>())).Returns((Department)null);
            // This setup is for FindAsync, which is not directly used by DeleteConfirmed(Department department) action.
            // Let's interpret "DepartmentNotFound" as a concurrency issue where the department was deleted by another user before SaveChanges.
            
            var departmentToDelete = new Department { DepartmentID = 999, Name="NonExistent", RowVersion = new byte[]{1,0,0,0}}; // Non-existent ID

            _mockContext.Setup(c => c.SaveChangesAsync())
                .ThrowsAsync(new DbUpdateConcurrencyException("Error", new List<DbEntityEntry>())); // Simulate concurrency, department gone
            
            var mockEntry = new Mock<DbEntityEntry<Department>>();
            mockEntry.SetupProperty(e => e.State); // Allow setting State
            _mockContext.Setup(c => c.Entry(departmentToDelete)).Returns(mockEntry.Object);


            // Act
            var result = await _controller.Delete(departmentToDelete) as RedirectToRouteResult;

            // Assert
            // If DbUpdateConcurrencyException occurs, it redirects to Delete GET with error.
            Assert.IsNotNull(result);
            Assert.AreEqual("Delete", result.RouteValues["action"]);
            Assert.IsTrue((bool)result.RouteValues["concurrencyError"]);
            _mockDepartmentSet.Verify(m => m.Remove(It.IsAny<Department>()), Times.Never()); // Remove is not called directly; State is set.
        }


        [TestMethod]
        public async Task DeleteConfirmed_ValidId_RemovesDepartment_And_RedirectsToIndex()
        {
            // Arrange
            var departmentToDelete = _departments.First(); 
            
            // Mock Entry and State setting for the specific department
            var mockEntry = new Mock<DbEntityEntry<Department>>();
            mockEntry.SetupProperty(e => e.State); // Important: allow State to be set
            _mockContext.Setup(c => c.Entry(departmentToDelete)).Returns(mockEntry.Object);


            // Act
            var result = await _controller.Delete(departmentToDelete) as RedirectToRouteResult;

            // Assert
            _mockContext.Verify(c => c.Entry(departmentToDelete), Times.Once());
            Assert.AreEqual(EntityState.Deleted, mockEntry.Object.State); // Verify State was set to Deleted
            _mockContext.Verify(m => m.SaveChangesAsync(), Times.Once());
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
        }

        [TestMethod]
        public async Task DeleteConfirmed_ConcurrencyException_RedirectsToDelete_WithError()
        {
            // Arrange
            var department = _departments.First();
            
            var mockEntry = new Mock<DbEntityEntry<Department>>();
            mockEntry.SetupProperty(e => e.State);
            _mockContext.Setup(c => c.Entry(department)).Returns(mockEntry.Object);

            _mockContext.Setup(c => c.SaveChangesAsync())
                .ThrowsAsync(new DbUpdateConcurrencyException("Concurrency error"));

            // Act
            var result = await _controller.Delete(department) as RedirectToRouteResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Delete", result.RouteValues["action"]);
            Assert.IsTrue((bool)result.RouteValues["concurrencyError"]);
            Assert.AreEqual(department.DepartmentID, result.RouteValues["id"]);
        }
    }
}
