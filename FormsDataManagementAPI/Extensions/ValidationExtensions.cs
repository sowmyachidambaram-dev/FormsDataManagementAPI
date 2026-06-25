using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace FormsDataManagementAPI.Extensions;

public static class ValidationExtensions
{
    public static ValidationProblemDetails ToValidationProblemDetails(this ValidationResult result)
    {
        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        return new ValidationProblemDetails(errors)
        {
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest
        };
    }
}
