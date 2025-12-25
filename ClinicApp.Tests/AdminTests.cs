using NUnit.Framework;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ClinicApp.Controllers;
using ClinicApp.Services.Core;
using ClinicApp.Models.Core;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClinicApp.Tests
{
    [TestFixture]
    public class AdminTests
    {
        private Mock<IAdminService> _adminServiceMock;
        private AdminController _controller;
        private Mock<ISession> _sessionMock;

        [SetUp]
        public void Setup()
        {
            _adminServiceMock = new Mock<IAdminService>();
            _controller = new AdminController(_adminServiceMock.Object);

            _sessionMock = new Mock<ISession>();
            var httpContext = new DefaultHttpContext();
            httpContext.Session = _sessionMock.Object;
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            _controller.ViewData = new ViewDataDictionary(
                new EmptyModelMetadataProvider(),
                new ModelStateDictionary());
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
        }

        private void SetupSessionRole(string role)
        {
            byte[] val = Encoding.UTF8.GetBytes(role);
            _sessionMock.Setup(s => s.TryGetValue("UserRole", out val)).Returns(true);
        }

        [Test]
        public async Task Dashboard_Authorized_PopulatesViewBag()
        {
            SetupSessionRole("Administrator");
            var stats = new Dictionary<string, object>
            {
                { "TotalPatients", 100 },
                { "TotalDoctors", 5 },
                { "TotalAppointments", 50 },
                { "ChartDates", new string[]{} },
                { "ChartCounts", new int[]{} }
            };

            _adminServiceMock.Setup(s => s.GetDashboardStats()).ReturnsAsync(stats);

            var result = await _controller.Dashboard() as ViewResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(100, _controller.ViewBag.TotalPatients);
            Assert.AreEqual(5, _controller.ViewBag.TotalDoctors);
        }

        [Test]
        public async Task AddMedication_Valid_Redirects()
        {
            SetupSessionRole("Administrator");
            var med = new Medication { Name = "Analgin" };

            var result = await _controller.AddMedication(med) as RedirectToActionResult;

            Assert.AreEqual("Medications", result?.ActionName);
            _adminServiceMock.Verify(s => s.AddMedication(med), Times.Once);
        }
    }
}