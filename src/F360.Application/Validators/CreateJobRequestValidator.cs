using F360.Application.DTOs.Requests;
using FluentValidation;

namespace F360.Application.Validators;

public class CreateJobRequestValidator : AbstractValidator<CreateJobRequest>
{
    public CreateJobRequestValidator()
    {
        RuleFor(x => x.Cep)
            .NotEmpty()
            .Matches(@"^\d{5}-?\d{3}$")
            .WithMessage("Invalid CEP format");

        RuleFor(x => x.Priority)
            .IsInEnum()
            .WithMessage("Invalid priority");
    }
}
