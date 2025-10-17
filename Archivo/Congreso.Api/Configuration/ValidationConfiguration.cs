using FluentValidation;
using FluentValidation.AspNetCore;
using System.Reflection;

namespace Congreso.Api.Configuration;

/// <summary>
/// Extension methods for configuring FluentValidation in the application
/// </summary>
public static class ValidationConfiguration
{
    /// <summary>
    /// Adds FluentValidation services to the container
    /// </summary>
    public static IServiceCollection AddFluentValidation(this IServiceCollection services)
    {
        // Add FluentValidation
        services.AddFluentValidation(fv =>
        {
            // Register all validators from the current assembly
            fv.RegisterValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            
            // Configure validation options
            fv.ImplicitlyValidateChildProperties = true;
            fv.ImplicitlyValidateRootCollectionElements = true;
        });

        return services;
    }
}