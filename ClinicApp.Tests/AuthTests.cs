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
using System.Threading.Tasks;

namespace ClinicApp.Tests
{
    [TestFixture]
    public class AuthTests
    {
        private ClinicContext _context;
        private AuthService _authService;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ClinicContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())

                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new ClinicContext(options);
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            _authService = new AuthService(_context, _httpContextAccessorMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        [Test]
        public async Task RegisterPatient_SavesUserAndPatientToDb()
        {
            var user = new User { Login = "new_pat", PasswordHash = "123", Role = "Patient", FullName = "Test" };
            var patient = new Patient { PolicyNumber = "OMS123", DateOfBirth = DateTime.Now, Gender = "Male" };

            bool result = await _authService.RegisterPatient(user, patient);

            Assert.IsTrue(result);

            var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Login == "new_pat");
            var dbPatient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == dbUser.Id);

            Assert.IsNotNull(dbUser, "Пользователь должен быть в таблице Users");
            Assert.IsNotNull(dbPatient, "Пациент должен быть в таблице Patients");
            Assert.AreEqual("OMS123", dbPatient.PolicyNumber);
        }

        [Test]
        public async Task Authenticate_ValidCredentials_ReturnsUser()
        {
            var user = new User { Login = "user1", PasswordHash = "pass1", Role = "Patient", FullName = "User", IsActive = true };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _authService.Authenticate("user1", "pass1");

            Assert.IsNotNull(result);
            Assert.AreEqual("user1", result.Login);
        }

        [Test]
        public async Task Authenticate_InvalidPassword_ReturnsNull()
        {
            var user = new User { Login = "user1", PasswordHash = "pass1", Role = "Patient", FullName = "User", IsActive = true };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _authService.Authenticate("user1", "wrong_pass");

            Assert.IsNull(result);
        }
    }
}