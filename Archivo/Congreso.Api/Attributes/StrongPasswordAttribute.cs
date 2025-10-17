using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Congreso.Api.Attributes
{
    /// <summary>
    /// Atributo de validación para contraseñas seguras
    /// Requiere: mínimo 8 caracteres, 1 mayúscula, 1 minúscula, 1 dígito, 1 símbolo
    /// </summary>
    public class StrongPasswordAttribute : ValidationAttribute
    {
        private const int MinLength = 8;
        private const int MaxLength = 128;

        public StrongPasswordAttribute()
        {
            ErrorMessage = "La contraseña debe tener al menos {0} caracteres, incluir al menos una letra mayúscula, una minúscula, un dígito y un símbolo especial.";
        }

        public override bool IsValid(object? value)
        {
            if (value is not string password)
                return false;

            if (string.IsNullOrWhiteSpace(password))
                return false;

            // Verificar longitud
            if (password.Length < MinLength || password.Length > MaxLength)
                return false;

            // Verificar que contenga al menos una letra minúscula
            if (!Regex.IsMatch(password, @"[a-z]"))
                return false;

            // Verificar que contenga al menos una letra mayúscula
            if (!Regex.IsMatch(password, @"[A-Z]"))
                return false;

            // Verificar que contenga al menos un dígito
            if (!Regex.IsMatch(password, @"\d"))
                return false;

            // Verificar que contenga al menos un símbolo especial
            if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?~`]"))
                return false;

            // Verificar que no contenga espacios en blanco
            if (password.Contains(' '))
                return false;

            return true;
        }

        public override string FormatErrorMessage(string name)
        {
            return string.Format(ErrorMessage ?? "Invalid password", MinLength);
        }

        /// <summary>
        /// Obtiene una descripción detallada de los requisitos de contraseña
        /// </summary>
        public static string GetPasswordRequirements()
        {
            return $"La contraseña debe cumplir los siguientes requisitos:\n" +
                   $"• Mínimo 8 caracteres y máximo 128 caracteres\n" +
                   $"• Al menos una letra minúscula (a-z)\n" +
                   $"• Al menos una letra mayúscula (A-Z)\n" +
                   $"• Al menos un dígito (0-9)\n" +
                   $"• Al menos un símbolo especial (!@#$%^&*()_+-=[]{{}}|;':\",./<>?~`)\n" +
                   $"• No debe contener espacios en blanco";
        }

        /// <summary>
        /// Valida una contraseña y devuelve errores específicos
        /// </summary>
        public static List<string> ValidatePasswordDetailed(string? password)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("La contraseña es requerida");
                return errors;
            }

            if (password.Length < MinLength)
                errors.Add($"La contraseña debe tener al menos {MinLength} caracteres");

            if (password.Length > MaxLength)
                errors.Add($"La contraseña no puede tener más de {MaxLength} caracteres");

            if (!Regex.IsMatch(password, @"[a-z]"))
                errors.Add("La contraseña debe incluir al menos una letra minúscula");

            if (!Regex.IsMatch(password, @"[A-Z]"))
                errors.Add("La contraseña debe incluir al menos una letra mayúscula");

            if (!Regex.IsMatch(password, @"\d"))
                errors.Add("La contraseña debe incluir al menos un dígito");

            if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?~`]"))
                errors.Add("La contraseña debe incluir al menos un símbolo especial");

            if (password.Contains(' '))
                errors.Add("La contraseña no debe contener espacios en blanco");

            return errors;
        }
    }
}