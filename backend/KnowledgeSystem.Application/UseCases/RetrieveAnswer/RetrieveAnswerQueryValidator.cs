using FluentValidation;

namespace KnowledgeSystem.Application.UseCases.RetrieveAnswer;

/// <summary>
/// Validator for RetrieveAnswerQuery
/// Ensures query inputs meet business requirements
/// </summary>
public sealed class RetrieveAnswerQueryValidator : AbstractValidator<RetrieveAnswerQuery>
{
    public RetrieveAnswerQueryValidator()
    {
        RuleFor(x => x.Question)
            .NotEmpty()
            .WithMessage("Question is required")
            .MinimumLength(3)
            .WithMessage("Question must be at least 3 characters")
            .MaximumLength(1000)
            .WithMessage("Question cannot exceed 1000 characters");

        RuleFor(x => x.TopK)
            .InclusiveBetween(1, 20)
            .WithMessage("TopK must be between 1 and 20");
    }
}

