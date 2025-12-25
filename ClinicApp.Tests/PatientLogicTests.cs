using NUnit.Framework;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using ClinicApp.Controllers;
using ClinicApp.Services.PatientService;
using ClinicApp.Services.Core;
using ClinicApp.Models.Core;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace ClinicApp.Tests
{
    [TestFixture]
    public class PatientLogicTests
    {
        private Mock<IPatientService> _patientServiceMock;
        private Mock<IScheduleService> _scheduleServiceMock;
        private Mock<IAdminService> _adminServiceMock;
        private PatientController _controller;
        private Mock<ISession> _sessionMock;

        [SetUp]
        public void Setup()
        {
            _patientServiceMock = new Mock<IPatientService>();
            _scheduleServiceMock = new Mock<IScheduleService>();
            _adminServiceMock = new Mock<IAdminService>();

            _controller = new PatientController(
                _patientServiceMock.Object,
                _scheduleServiceMock.Object,
                _adminServiceMock.Object
            );

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
        public async Task MyAppointments_Authorized_ReturnsList()
        {
            SetupSessionRole("Patient");
            var appointments = new List<Appointment>();
            _patientServiceMock.Setup(s => s.GetPatientAppointments()).ReturnsAsync(appointments);

            var result = await _controller.MyAppointments() as ViewResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(appointments, result.Model);
        }

        [Test]
        public async Task CreateAppointment_Post_Success_Redirects()
        {
            SetupSessionRole("Patient");
            string dateStr = "2024-01-01 12:00";

            _patientServiceMock.Setup(s => s.CreateAppointment(It.IsAny<Appointment>()))
                .ReturnsAsync(true);

            var result = await _controller.CreateAppointment(1, dateStr, "Reason") as RedirectToActionResult;

            Assert.AreEqual("MyAppointments", result?.ActionName);
            Assert.AreEqual("Записано", _controller.TempData["Success"]);
        }

        [Test]
        public async Task GetDoctorsBySpec_ReturnsJson()
        {
            var doctors = new List<ClinicApp.Models.DoctorModels.Doctor>
            {
                new() { Id = 1, SpecializationId = 2, User = new() { FullName = "Dr. House" } }
            };
            _patientServiceMock.Setup(s => s.GetAvailableDoctors()).ReturnsAsync(doctors);

            var result = await _controller.GetDoctorsBySpec(2) as JsonResult;

            Assert.IsNotNull(result);
        }
    }
}