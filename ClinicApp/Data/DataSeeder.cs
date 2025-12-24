using Microsoft.EntityFrameworkCore;
using ClinicApp.Models.Core;
using ClinicApp.Models.DoctorModels;
using ClinicApp.Models.PatientModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicApp.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var context = new ClinicContext(
                serviceProvider.GetRequiredService<DbContextOptions<ClinicContext>>());

            if (!await context.Specializations.AnyAsync())
            {
                var specs = new[]
                {
                    new Specialization { Name = "Терапевт", Description = "Врач общей практики", AverageConsultationTime = 20 },
                    new Specialization { Name = "Хирург", Description = "Оперативное лечение", AverageConsultationTime = 40 },
                    new Specialization { Name = "Кардиолог", Description = "Заболевания сердца", AverageConsultationTime = 30 },
                    new Specialization { Name = "Невролог", Description = "Заболевания нервной системы", AverageConsultationTime = 30 },
                    new Specialization { Name = "Офтальмолог", Description = "Заболевания глаз", AverageConsultationTime = 25 }
                };
                await context.Specializations.AddRangeAsync(specs);
                await context.SaveChangesAsync();
            }

            if (!await context.Medications.AnyAsync())
            {
                var medications = new[]
                {
                    new Medication { Name = "Аспирин", Description = "Обезболивающее", Form = "Таблетки", Manufacturer = "Bayer", PrescriptionRequired = true },
                    new Medication { Name = "Парацетамол", Description = "Жаропонижающее", Form = "Таблетки", Manufacturer = "GlaxoSmithKline", PrescriptionRequired = false },
                    new Medication { Name = "Амоксициллин", Description = "Антибиотик", Form = "Капсулы", Manufacturer = "Sandoz", PrescriptionRequired = true }
                };
                await context.Medications.AddRangeAsync(medications);
                await context.SaveChangesAsync();
            }

            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Login == "admin");
            if (adminUser == null)
            {
                adminUser = new User
                {
                    Login = "admin",
                    PasswordHash = "admin123",
                    Role = "Administrator",
                    FullName = "Администратор Системы",
                    Email = "admin@clinic.ru",
                    Phone = "+79990000000",
                    IsActive = true,
                    RegistrationDate = DateTime.Now
                };
                await context.Users.AddAsync(adminUser);
                await context.SaveChangesAsync();
            }

            if (!await context.Administrators.AnyAsync(a => a.Id == adminUser.Id))
            {
                var adminProfile = new Administrator
                {
                    Id = adminUser.Id,
                    Department = "IT-Отдел",
                    Responsibilities = "Главный системный администратор"
                };
                await context.Administrators.AddAsync(adminProfile);
                await context.SaveChangesAsync();
            }

            if (!await context.Users.AnyAsync(u => u.Login == "doctor1"))
            {
                var terapevtSpec = await context.Specializations.FirstOrDefaultAsync(s => s.Name == "Терапевт");
                if (terapevtSpec == null) terapevtSpec = await context.Specializations.FirstAsync();

                var doctorUser = new User
                {
                    Login = "doctor1",
                    PasswordHash = "doc123",
                    Role = "Doctor",
                    FullName = "Иванов Петр Сергеевич",
                    Email = "ivanov@clinic.ru",
                    Phone = "+79001112233",
                    IsActive = true,
                    RegistrationDate = DateTime.Now
                };
                await context.Users.AddAsync(doctorUser);
                await context.SaveChangesAsync();

                var doctorProfile = new Doctor
                {
                    Id = doctorUser.Id,
                    SpecializationId = terapevtSpec.Id,
                    LicenseNumber = "DOC-2024-001",
                    Experience = 10,
                    IsActive = true
                };
                await context.Doctors.AddAsync(doctorProfile);
                await context.SaveChangesAsync();
            }

            if (!await context.Users.AnyAsync(u => u.Login == "test"))
            {
                var patientUser = new User
                {
                    Login = "test",
                    PasswordHash = "test123",
                    Role = "Patient",
                    FullName = "Сидоров Алексей",
                    Email = "patient@mail.ru",
                    Phone = "+79005554433",
                    IsActive = true,
                    RegistrationDate = DateTime.Now
                };
                await context.Users.AddAsync(patientUser);
                await context.SaveChangesAsync();

                var patientProfile = new Patient
                {
                    Id = patientUser.Id,
                    PolicyNumber = "OMS-1234567890",
                    DateOfBirth = new DateTime(1990, 5, 15),
                    Gender = "Male",
                    Address = "г. Москва, ул. Ленина, д. 10",
                    IsActive = true
                };
                await context.Patients.AddAsync(patientProfile);
                await context.SaveChangesAsync();
            }

            Console.WriteLine("База данных проверена и инициализирована.");
        }
    }
}