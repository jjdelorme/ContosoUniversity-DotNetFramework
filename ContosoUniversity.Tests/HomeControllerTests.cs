using Microsoft.VisualStudio.TestTools.UnitTesting;
using ContosoUniversity.Controllers;
using ContosoUniversity.DAL;
using ContosoUniversity.ViewModels; // For EnrollmentDateGroup
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity.Infrastructure; // For DatabaseFacade, DbRawSqlQuery
using System; // For DateTime
using System.Collections; // For non-generic IEnumerable

namespace ContosoUniversity.Tests
{
    [TestClass]
    public class HomeControllerTests
    {
        private Mock<ISchoolContext> _mockContext;
        private HomeController _controller;
        private List<EnrollmentDateGroup> _enrollmentData;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockContext = new Mock<ISchoolContext>();

            _enrollmentData = new List<EnrollmentDateGroup>
            {
                new EnrollmentDateGroup { EnrollmentDate = DateTime.Parse("2023-01-15"), StudentCount = 5 },
                new EnrollmentDateGroup { EnrollmentDate = DateTime.Parse("2023-02-20"), StudentCount = 10 }
            };

            // Mocking DatabaseFacade and SqlQuery<T>
            var mockDatabaseFacade = new Mock<DatabaseFacade>(null); // Pass null for DbContext if not needed for interactions

            var mockDbRawSqlQuery = new Mock<DbRawSqlQuery<EnrollmentDateGroup>>();
            
            // Setup the mock as an IEnumerable<EnrollmentDateGroup>
            mockDbRawSqlQuery.As<IEnumerable<EnrollmentDateGroup>>()
                .Setup(m => m.GetEnumerator())
                .Returns(() => _enrollmentData.GetEnumerator()); // Use lambda to ensure fresh enumerator

            // Setup the mock as a non-generic IEnumerable for cases where it might be treated as such (e.g. by some MVC internals or helpers)
            mockDbRawSqlQuery.As<IEnumerable>()
                .Setup(m => m.GetEnumerator())
                .Returns(() => _enrollmentData.GetEnumerator());


            // Setup the SqlQuery method on DatabaseFacade mock to return our DbRawSqlQuery mock
            mockDatabaseFacade.Setup(db => db.SqlQuery<EnrollmentDateGroup>(It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns(mockDbRawSqlQuery.Object);

            // Setup the Database property on ISchoolContext mock to return our DatabaseFacade mock
            _mockContext.Setup(c => c.Database).Returns(mockDatabaseFacade.Object);

            _controller = new HomeController(_mockContext.Object);
        }

        [TestMethod]
        public void Index_ReturnsViewResult()
        {
            // Act
            var result = _controller.Index() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void About_ReturnsViewResult_WithEnrollmentData()
        {
            // Act
            var result = _controller.About() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result.Model as IEnumerable<EnrollmentDateGroup>;
            Assert.IsNotNull(model);
            Assert.AreEqual(_enrollmentData.Count, model.Count());
            // Optionally, compare contents if necessary
            CollectionAssert.AreEqual(_enrollmentData, model.ToList());
        }

        [TestMethod]
        public void Contact_ReturnsViewResult()
        {
            // Act
            var result = _controller.Contact() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            // Check ViewBag.Message as per controller's code
            Assert.AreEqual("Your contact page.", result.ViewBag.Message);
        }
    }
}
