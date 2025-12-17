using ClinicApp.Controllers;
using ClinicApp.Data;
using ClinicApp.Models.Core;
using ClinicApp.Models.PatientModels;
using ClinicApp.Services.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace ClinicApp.Tests
{
    [TestFixture]
    public class AuthTests
    {
        private AuthController _controller;
        private Mock<IAuthService> _mockAuthService;
        private ClinicContext _context;
        private SqliteConnection _connection;

        [SetUp]
        public void Setup()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            var options = new DbContextOptionsBuilder<ClinicContext>().UseSqlite(_connection).Options;
            _context = new ClinicContext(options);
            _context.Database.EnsureCreated();

            _mockAuthService = new Mock<IAuthService>();

            var mockSession = new Mock<ISession>();
            var httpContext = new DefaultHttpContext { Session = mockSession.Object };

            _controller = new AuthController(_mockAuthService.Object, _context);
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
            _context?.Dispose();
            _connection?.Close();
        }

        [Test]
        public async Task Login_ValidCredentials_RedirectsToDashboard()
        {
            var user = new User { Id = 1, Login = "test", Role = "Patient" };
            _mockAuthService.Setup(x => x.Authenticate("test", "pass")).ReturnsAsync(user);

            var result = await _controller.Login("test", "pass");

            var redirect = result as RedirectToActionResult;
            Assert.IsNotNull(redirect);
            Assert.AreEqual("Dashboard", redirect.ActionName);
        }

        [Test]
        public async Task Register_NewPatient_CreatesUserAndPatientRecord()
        {
            _mockAuthService.Setup(s => s.Register(It.IsAny<User>()))
                .Returns(async (User u) =>
                {
                    _context.Users.Add(u);
                    await _context.SaveChangesAsync();
                    return true;
                });

            await _controller.Register("newLogin", "123", "123", "FIO", "e", "p", "POLIS-999", DateTime.Now, "Male", "Addr");

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.PolicyNumber == "POLIS-999");
            Assert.IsNotNull(patient);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == "newLogin");
            Assert.IsNotNull(user);

            Assert.AreEqual(user.Id, patient.Id);
        }
    }
}