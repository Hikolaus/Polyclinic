using NUnit.Framework;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using ClinicApp.Controllers;
using ClinicApp.Services.DoctorService;
using ClinicApp.Models.DoctorModels;
using ClinicApp.Models.Core;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClinicApp.Tests
{
    [TestFixture]
    public class DoctorTests
    {
        private Mock<IDoctorService> _doctorServiceMock;
        private DoctorController _controller;
        private Mock<ISession> _sessionMock;

        [SetUp]
        public void Setup()
        {
            _doctorServiceMock = new Mock<IDoctorService>();
            _controller = new DoctorController(_doctorServiceMock.Object);

            _sessionMock = new Mock<ISession>();
            var httpContext = new DefaultHttpContext();
            httpContext.Session = _sessionMock.Object;
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
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
        public async Task Dashboard_NotAuthorized_ReturnsNotAuthorizedView()
        {
            SetupSessionRole("Patient");
            var result = await _controller.Dashboard() as ViewResult;
            Assert.AreEqual("NotAuthorized", result?.ViewName);
        }

        [Test]
        public async Task Dashboard_Authorized_ReturnsViewWithDoctor()
        {
            SetupSessionRole("Doctor");
            var doctor = new Doctor { Id = 1, LicenseNumber = "123" };

            _doctorServiceMock.Setup(s => s.GetCurrentDoctor()).ReturnsAsync(doctor);
            _doctorServiceMock.Setup(s => s.GetTodayAppointments()).ReturnsAsync(new List<Appointment>());

            var result = await _controller.Dashboard() as ViewResult;

            Assert.IsNotNull(result);
            Assert.AreNotEqual("NotAuthorized", result.ViewName);
            Assert.AreEqual(doctor, result.Model);
        }

        [Test]
        public async Task CompleteConsultation_Success_RedirectsToAppointments()
        {
            SetupSessionRole("Doctor");
            var model = new ConsultationViewModel { AppointmentId = 1 };
            _doctorServiceMock.Setup(s => s.CompleteConsultationAsync(model)).ReturnsAsync(true);

            var result = await _controller.CompleteConsultation(model) as RedirectToActionResult;

            Assert.AreEqual("Appointments", result?.ActionName);
            Assert.AreEqual("Прием завершен", _controller.TempData["Success"]);
        }
    }
}