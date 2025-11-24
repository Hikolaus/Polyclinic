using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using ClinicApp.Models.Core;
using ClinicApp.Models.PatientModels;
using ClinicApp.Models.DoctorModels;

namespace ClinicApp.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var context = new ClinicContext(
                serviceProvider.GetRequiredService<DbContextOptions<ClinicContext>>());

            if (context.Users.Any())
            {
                return;
            }

            var specializations = new Specialization[]
            {
                new Specialization { Name = "Терапевт", Description = "Врач общей практики", AverageConsultationTime = 20 },
                new Specialization { Name = "Хирург", Description = "Оперативное лечение", AverageConsultationTime = 40 },
                new Specialization { Name = "Кардиолог", Description = "Заболевания сердца", AverageConsultationTime = 30 },
                new Specialization { Name = "Невролог", Description = "Заболевания нервной системы", AverageConsultationTime = 30 },
                new Specialization { Name = "Офтальмолог", Description = "Заболевания глаз", AverageConsultationTime = 25 }
            };

            context.Specializations.AddRange(specializations);
            context.SaveChanges();

            var medications = new Medication[]
            {
                new Medication { Name = "Аспирин", Description = "Обезболивающее", Form = "Таблетки", Manufacturer = "Bayer", PrescriptionRequired = true },
                new Medication { Name = "Парацетамол", Description = "Жаропонижающее", Form = "Таблетки", Manufacturer = "GlaxoSmithKline", PrescriptionRequired = false },
                new Medication { Name = "Амоксициллин", Description = "Антибиотик", Form = "Капсулы", Manufacturer = "Sandoz", PrescriptionRequired = true }
            };

            context.Medications.AddRange(medications);
            context.SaveChanges();

            Console.WriteLine("Начальные данные добавлены в базу данных.");
        }
    }
}