using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ClinicApp.Models.Core;
using ClinicApp.Models.PatientModels;
using ClinicApp.Models.DoctorModels;

namespace ClinicApp.Data
{
    public class ClinicContext : DbContext
    {
        public ClinicContext(DbContextOptions<ClinicContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Specialization> Specializations { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<Medication> Medications { get; set; }
        public DbSet<MedicalRecord> MedicalRecords { get; set; }
        public DbSet<WaitlistRequest> WaitlistRequests { get; set; }
        public DbSet<Diagnosis> Diagnoses { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Patient>().ToTable("Patients");
            modelBuilder.Entity<Doctor>().ToTable("Doctors");

            var appointmentStatusConverter = new ValueConverter<AppointmentStatus, string>(
                v => v.ToString(),
                v => (AppointmentStatus)Enum.Parse(typeof(AppointmentStatus), v));

            modelBuilder.Entity<Appointment>()
                .Property(a => a.Status)
                .HasConversion(appointmentStatusConverter);

            var prescriptionStatusConverter = new ValueConverter<PrescriptionStatus, string>(
                v => v.ToString(),
                v => (PrescriptionStatus)Enum.Parse(typeof(PrescriptionStatus), v));

            modelBuilder.Entity<Prescription>()
                .Property(p => p.Status)
                .HasConversion(prescriptionStatusConverter);

            var notificationTypeConverter = new ValueConverter<NotificationType, string>(
                v => v.ToString(),
                v => (NotificationType)Enum.Parse(typeof(NotificationType), v));

            modelBuilder.Entity<Notification>()
                .Property(n => n.Type)
                .HasConversion(notificationTypeConverter);
        }
    }
}