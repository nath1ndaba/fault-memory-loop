using FaultMemoryLoop.Application.Contracts;
using FluentValidation;

namespace FaultMemoryLoop.Application.Validators;

/// <summary>
/// A malformed or empty fault description shouldn't silently reach the model —
/// this is the first line of defence, before any LLM call is made.
/// </summary>
public class TriageRequestValidator : AbstractValidator<TriageRequest>
{
    public TriageRequestValidator()
    {
        RuleFor(x => x.RawDescription)
            .NotEmpty().WithMessage("A fault description is required.")
            .MinimumLength(10).WithMessage("Description is too short to extract anything meaningful from.")
            .MaximumLength(2000).WithMessage("Description is too long — summarise before submitting.");

        RuleFor(x => x.CreatedBy)
            .NotEmpty().WithMessage("Adviser identifier is required for the audit trail.");
    }
}
