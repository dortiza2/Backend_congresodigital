using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Collections.Generic;

namespace Congreso.Api.Infrastructure
{
    public class SwaggerFileOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var actionDescriptor = context.ApiDescription.ActionDescriptor as ControllerActionDescriptor;
            if (actionDescriptor == null) return;

            var formFileParameters = context.ApiDescription.ParameterDescriptions
                .Where(x => x.ModelMetadata?.ModelType == typeof(IFormFile) || 
                           x.ModelMetadata?.ModelType == typeof(List<IFormFile>) ||
                           x.ModelMetadata?.ModelType == typeof(IEnumerable<IFormFile>))
                .ToList();

            if (!formFileParameters.Any()) return;

            operation.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Required = new HashSet<string>(formFileParameters.Select(x => x.Name)),
                            Properties = formFileParameters.ToDictionary(
                                x => x.Name,
                                x => GenerateSchemaForParameter(x, context)
                            )
                        }
                    }
                }
            };
        }

        private OpenApiSchema GenerateSchemaForParameter(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiParameterDescription param, OperationFilterContext context)
        {
            if (param.ModelMetadata?.ModelType == typeof(IFormFile))
            {
                return new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary",
                    Description = param.ModelMetadata?.Description
                };
            }
            else if (param.ModelMetadata?.ModelType == typeof(List<IFormFile>) || 
                     param.ModelMetadata?.ModelType == typeof(IEnumerable<IFormFile>))
            {
                return new OpenApiSchema
                {
                    Type = "array",
                    Items = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary"
                    },
                    Description = param.ModelMetadata?.Description
                };
            }
            else
            {
                return context.SchemaGenerator.GenerateSchema(param.ModelMetadata?.ModelType, context.SchemaRepository);
            }
        }
    }
}