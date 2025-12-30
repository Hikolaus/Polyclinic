using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Microsoft.AspNetCore.Http;
using ClinicApp.Data;
using ClinicApp.Services.Core;
using ClinicApp.Models.Core;
using ClinicApp.Models.PatientModels;
using System;
using System.Text;
using System.Threading.Tasks;

namespace ClinicApp.Tests
{
    [TestFixture]
    public class AuthServiceTests
    {
        private ClinicContext _context;
        private AuthService _service;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<ISession> _sessionMock;
        private Mock<HttpContext> _httpContextMock;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ClinicContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new ClinicContext(options);
            _sessionMock = new Mock<ISession>();
            _httpContextMock = new Mock<HttpContext>();
            _httpContextMock.Setup(s => s.Session).Returns(_sessionMock.Object);
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _httpContextAccessorMock.Setup(h => h.HttpContext).Returns(_httpContextMock.Object);

            _service = new AuthService(_context, _httpContextAccessorMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task Authenticate_Success_ReturnsUser()
        {
            _context.Users.Add(new User { Login = "u", PasswordHash = "p", Role = "R", FullName = "N", IsActive = true });
            await _context.SaveChangesAsync();

            var user = await _service.Authenticate("u", "p");
            Assert.IsNotNull(user);
        }

        [Test]
        public async Task Authenticate_Fail_ReturnsNull()
        {
            var user = await _service.Authenticate("u", "p");
            Assert.IsNull(user);
        }

        [Test]
        public async Task Register_Success_CreatesUser()
        {
            var res = await _service.Register(new User { Login = "u", PasswordHash = "p", Role = "A", FullName = "N" });
            Assert.IsTrue(res);
            Assert.AreEqual(1, await _context.Users.CountAsync());
        }

        [Test]
        public async Task RegisterPatient_Success_CreatesUserAndPatient()
        {
            var res = await _service.RegisterPatient(
                new User { Login = "u", PasswordHash = "p", Role = "Patient", FullName = "N" },
                new Patient { PolicyNumber = "123", DateOfBirth = DateTime.Now, Gender = "M" }
            );
            Assert.IsTrue(res);
            Assert.AreEqual(1, await _context.Patients.CountAsync());
        }

        [Test]
        public void Login_SetsSession()
        {
            var user = new User { Id = 1, Role = "Admin", FullName = "Name" };
            _service.Login(user);
            _sessionMock.Verify(s => s.Set("UserId", It.IsAny<byte[]>()), Times.Once);
        }

        [Test]
        public void Logout_ClearsSession()
        {
            _service.Logout();
            _sessionMock.Verify(s => s.Clear(), Times.Once);
        }

        [Test]
        public void GetCurrentUser_ReturnsUserFromSession()
        {
            _context.Users.Add(new User { Id = 1, Login = "u", Role = "A", FullName = "N" });
            _context.SaveChanges();

            byte[] idBytes = BitConverter.GetBytes(1);
            if (BitConverter.IsLittleEndian) Array.Reverse(idBytes);

            _sessionMock.Setup(s => s.TryGetValue("UserId", out idBytes)).Returns(true);

            var user = _service.GetCurrentUser();
        }

        [Test]
        public void GetCurrentUserRole_ReturnsRole()
        {
            var roleBytes = Encoding.UTF8.GetBytes("Admin");
            _sessionMock.Setup(s => s.TryGetValue("UserRole", out roleBytes)).Returns(true);
            var role = _service.GetCurrentUserRole();
            Assert.AreEqual("Admin", role);
        }

        [Test]
        public void IsLoggedIn_Check()
        {
            byte[] outBytes;
            _sessionMock.Setup(s => s.TryGetValue("UserId", out outBytes)).Returns(false);

            Assert.IsFalse(_service.IsLoggedIn());

            byte[] idBytes = BitConverter.GetBytes(1);
            if (BitConverter.IsLittleEndian) Array.Reverse(idBytes);

            _sessionMock.Setup(s => s.TryGetValue("UserId", out idBytes)).Returns(true);

            Assert.IsTrue(_service.IsLoggedIn());
        }
    }
}