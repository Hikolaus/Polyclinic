using Microsoft.EntityFrameworkCore;
using ClinicApp.Data;
using ClinicApp.Services.Core;
using ClinicApp.Services.PatientService;
using ClinicApp.Services.DoctorService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Строка подключения 'DefaultConnection' не найдена в файле конфигурации.");
}


builder.Services.AddDbContext<ClinicContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews().AddSessionStateTempDataProvider();
builder.Services.AddRazorPages();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ClinicContext>();
    await context.Database.EnsureCreatedAsync();

    if (!context.Users.Any())
    {
        await SeedTestData(context);
        Console.WriteLine("База данных создана с тестовыми данными");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка БД: {ex.Message}");
}

Console.WriteLine($"Приложение запущено: https://localhost:7000");
Console.WriteLine("Тестовые пользователи:");
Console.WriteLine("Пациент: test / test123");
Console.WriteLine("Врач: doctor1 / doc123");
Console.WriteLine("Админ: admin / admin123");

await app.RunAsync();

async Task SeedTestData(ClinicContext context)
{
    var specs = new[]
    {
        new ClinicApp.Models.Core.Specialization { Name = "Терапевт", Description = "Врач общей практики" },
        new ClinicApp.Models.Core.Specialization { Name = "Хирург", Description = "Оперативное лечение" },
        new ClinicApp.Models.Core.Specialization { Name = "Кардиолог", Description = "Сердечно-сосудистые заболевания" }
    };
    await context.Specializations.AddRangeAsync(specs);
    await context.SaveChangesAsync();

    var admin = new ClinicApp.Models.Core.User
    {
        Login = "admin",
        PasswordHash = "admin123",
        Role = "Administrator",
        FullName = "Администратор",
        Email = "admin@clinic.ru",
        Phone = "+79001234567",
        RegistrationDate = DateTime.Now,
        IsActive = true
    };
    await context.Users.AddAsync(admin);
    await context.SaveChangesAsync();

    var terapevt = context.Specializations.First(s => s.Name == "Терапевт");
    var doctorUser = new ClinicApp.Models.Core.User
    {
        Login = "doctor1",
        PasswordHash = "doc123",
        Role = "Doctor",
        FullName = "Иванов Петр Сергеевич",
        Email = "doctor@clinic.ru",
        Phone = "+79001234568",
        RegistrationDate = DateTime.Now,
        IsActive = true
    };
    await context.Users.AddAsync(doctorUser);
    await context.SaveChangesAsync();

    var doctor = new ClinicApp.Models.DoctorModels.Doctor
    {
        Id = doctorUser.Id,
        SpecializationId = terapevt.Id,
        LicenseNumber = "DOC-001-2023",
        Experience = 10,
        IsActive = true
    };
    await context.Doctors.AddAsync(doctor);

    var patientUser = new ClinicApp.Models.Core.User
    {
        Login = "test",
        PasswordHash = "test123",
        Role = "Patient",
        FullName = "Тестовый Пациент",
        Email = "test@mail.ru",
        Phone = "88005553535",
        RegistrationDate = DateTime.Now,
        IsActive = true
    };
    await context.Users.AddAsync(patientUser);
    await context.SaveChangesAsync();

    var patient = new ClinicApp.Models.PatientModels.Patient
    {
        Id = patientUser.Id,
        PolicyNumber = "TEST123456",
        DateOfBirth = new DateTime(1990, 1, 1),
        Address = "ул. Тестовая 1",
        Gender = "Male"
    };
    await context.Patients.AddAsync(patient);

    await context.SaveChangesAsync();
}