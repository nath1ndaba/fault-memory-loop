using FaultMemoryLoop.Application.Contracts;
using FluentValidation;

namespace FaultMemoryLoop.Application.Validators;

public class ResolveJobRequestValidator : AbstractValidator<ResolveJobRequest>
{
    public ResolveJobRequestValidator()
    {
        RuleFor(x => x.ActualDiagnosis).NotEmpty();
        RuleFor(x => x.ActualFix).NotEmpty();
        RuleFor(x => x.ResolvedBy).NotEmpty();
        RuleFor(x => x.LabourHours).GreaterThanOrEqualTo(0);
    }
}