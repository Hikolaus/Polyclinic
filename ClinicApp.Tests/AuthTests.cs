using NUnit.Framework;
using Moq;
using Microsoft.AspNetCore.Mvc;
using ClinicApp.Controllers;
using ClinicApp.Services.Core;
using ClinicApp.Models.Core;
using ClinicApp.Models.PatientModels;

namespace ClinicApp.Tests
{
    [TestFixture]
    public class AuthTests
    {
        private Mock<IAuthService> _authServiceMock;
        private AuthController _controller;

        [SetUp]
        public void Setup()
        {
            _authServiceMock = new Mock<IAuthService>();
            _controller = new AuthController(_authServiceMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
        }

        [Test]
        public async Task Login_ValidCredentials_RedirectsToPatientDashboard()
        {
            var user = new User { Id = 1, Login = "test", Role = "Patient" };
            _authServiceMock.Setup(s => s.Authenticate("test", "password")).ReturnsAsync(user);

            var result = await _controller.Login("test", "password") as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Dashboard", result.ActionName);
            Assert.AreEqual("Patient", result.ControllerName);
            _authServiceMock.Verify(s => s.Login(user), Times.Once);
        }

        [Test]
        public async Task Login_InvalidCredentials_ReturnsViewWithError()
        {
            _authServiceMock.Setup(s => s.Authenticate(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            var result = await _controller.Login("wrong", "pass") as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsTrue(_controller.ViewBag.Error == "Неверный логин или пароль");
        }

        [Test]
        public async Task Register_PasswordsDoNotMatch_ReturnsViewWithError()
        {
            var result = await _controller.Register(
                "login", "pass", "DIFFERENT_PASS",
                "Name", "email", "phone", "policy", DateTime.Now, "Male", "Address"
            ) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsTrue(_controller.ViewBag.Error == "Пароли не совпадают");
        }

        [Test]
        public async Task Register_ValidData_RedirectsToDashboard()
        {
            _authServiceMock.Setup(s => s.RegisterPatient(It.IsAny<User>(), It.IsAny<Patient>()))
                .ReturnsAsync(true);

            var result = await _controller.Register(
                "login", "pass", "pass",
                "Name", "email", "phone", "policy", DateTime.Now, "Male", "Address"
            ) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Dashboard", result.ActionName);
        }
    }
}