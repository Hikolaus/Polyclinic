using System.ComponentModel.DataAnnotations;

namespace ClinicApp.Models.Core
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Логин обязателен")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Логин должен быть от 3 до 50 символов")]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть не менее 6 символов")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Подтверждение пароля обязательно")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "ФИО обязательно")]
        [StringLength(100, ErrorMessage = "ФИО не должно превышать 100 символов")]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Некорректный email адрес")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Некорректный номер телефона")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Номер полиса обязателен")]
        [StringLength(20, ErrorMessage = "Номер полиса не должен превышать 20 символов")]
        public string PolicyNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Дата рождения обязательна")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Пол обязателен")]
        public string Gender { get; set; } = string.Empty;

        public string? Address { get; set; }
    }
}