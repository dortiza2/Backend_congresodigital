using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.RegularExpressions;

namespace Congreso.Api.Filters
{
    /// <summary>
    /// Filtro global para validación y sanitización de entrada
    /// Equivalente a ValidationPipe en NestJS con políticas estrictas
    /// </summary>
    public class GlobalInputValidationFilter : IActionFilter
    {
        private readonly ILogger<GlobalInputValidationFilter> _logger;
        
        // Patrones para detectar posibles ataques
        private static readonly Regex[] MaliciousPatterns = {
            new Regex(@"<script[^>]*>.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"javascript:", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"on\w+\s*=", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"(union|select|insert|update|delete|drop|create|alter)\s+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"['\"";].*(--|#)", RegexOptions.IgnoreCase | RegexOptions.Compiled)
        };

        public GlobalInputValidationFilter(ILogger<GlobalInputValidationFilter> logger)
        {
            _logger = logger;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // 1. Verificar validación del modelo
            if (!context.ModelState.IsValid)
            {
                var errors = GetValidationErrors(context.ModelState);
                _logger.LogWarning("Model validation failed for {Action}: {Errors}", 
                    context.ActionDescriptor.DisplayName, string.Join(", ", errors));
                
                context.Result = new BadRequestObjectResult(new
                {
                    message = "Validation failed",
                    errors = errors
                });
                return;
            }

            // 2. Sanitizar y validar parámetros de entrada
            foreach (var parameter in context.ActionArguments)
            {
                if (parameter.Value != null)
                {
                    var sanitizedValue = SanitizeInput(parameter.Value);
                    if (sanitizedValue != parameter.Value)
                    {
                        _logger.LogWarning("Potentially malicious input detected and sanitized for parameter {Parameter} in action {Action}", 
                            parameter.Key, context.ActionDescriptor.DisplayName);
                        
                        // Actualizar el valor sanitizado
                        context.ActionArguments[parameter.Key] = sanitizedValue;
                    }

                    // Verificar patrones maliciosos
                    if (ContainsMaliciousContent(parameter.Value))
                    {
                        _logger.LogWarning("Malicious content detected in parameter {Parameter} for action {Action}", 
                            parameter.Key, context.ActionDescriptor.DisplayName);
                        
                        context.Result = new BadRequestObjectResult(new
                        {
                            message = "Invalid input detected",
                            parameter = parameter.Key
                        });
                        return;
                    }
                }
            }

            // 3. Validar tamaño de payload
            if (context.HttpContext.Request.ContentLength > 10 * 1024 * 1024) // 10MB
            {
                _logger.LogWarning("Request payload too large: {Size} bytes for action {Action}", 
                    context.HttpContext.Request.ContentLength, context.ActionDescriptor.DisplayName);
                
                context.Result = new BadRequestObjectResult(new
                {
                    message = "Request payload too large"
                });
                return;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No se requiere procesamiento post-acción
        }

        private List<string> GetValidationErrors(ModelStateDictionary modelState)
        {
            var errors = new List<string>();
            
            foreach (var state in modelState)
            {
                foreach (var error in state.Value.Errors)
                {
                    var fieldName = state.Key;
                    var errorMessage = !string.IsNullOrEmpty(error.ErrorMessage) 
                        ? error.ErrorMessage 
                        : "Invalid value";
                    
                    errors.Add($"{fieldName}: {errorMessage}");
                }
            }
            
            return errors;
        }

        private object SanitizeInput(object input)
        {
            return input switch
            {
                string str => SanitizeString(str),
                _ when input.GetType().IsClass && input.GetType() != typeof(string) => SanitizeObject(input),
                _ => input
            };
        }

        private string SanitizeString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Remover caracteres de control
            var sanitized = Regex.Replace(input, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");
            
            // Escapar caracteres HTML básicos
            sanitized = sanitized
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#x27;");

            // Limitar longitud
            if (sanitized.Length > 10000)
            {
                sanitized = sanitized.Substring(0, 10000);
            }

            return sanitized;
        }

        private object SanitizeObject(object obj)
        {
            if (obj == null) return obj;

            var type = obj.GetType();
            var properties = type.GetProperties();

            foreach (var property in properties)
            {
                if (property.CanRead && property.CanWrite && property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(obj) as string;
                    if (value != null)
                    {
                        property.SetValue(obj, SanitizeString(value));
                    }
                }
            }

            return obj;
        }

        private bool ContainsMaliciousContent(object input)
        {
            if (input == null) return false;

            var inputString = input.ToString();
            if (string.IsNullOrEmpty(inputString)) return false;

            return MaliciousPatterns.Any(pattern => pattern.IsMatch(inputString));
        }
    }

    /// <summary>
    /// Extension methods for registering global input validation
    /// </summary>
    public static class GlobalInputValidationExtensions
    {
        /// <summary>
        /// Adds global input validation services to the service collection
        /// </summary>
        public static IServiceCollection AddGlobalInputValidation(this IServiceCollection services)
        {
            // Register any required services for global input validation
            return services;
        }
    }
}