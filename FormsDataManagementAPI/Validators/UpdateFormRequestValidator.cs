using FluentValidation;
using FormsDataManagementAPI.DTOs;

namespace FormsDataManagementAPI.Validators;

public class UpdateFormRequestValidator : AbstractValidator<UpdateFormRequest>
{
    public UpdateFormRequestValidator()
    {
        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Subject is required.")
            .MaximumLength(200).WithMessage("Subject must not exceed 200 characters.");

        RuleFor(x => x.Priority)
            .InclusiveBetween(1, 10).WithMessage("Priority must be between 1 and 10.")
            .When(x => x.Priority.HasValue);

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("DueDate must be a future date.")
            .When(x => x.DueDate.HasValue);
    }
}
