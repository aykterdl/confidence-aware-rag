using FluentValidation;

namespace KnowledgeSystem.Application.UseCases.IngestDocument;

/// <summary>
/// Validator for IngestDocumentCommand
/// Ensures command inputs meet business requirements
/// </summary>
public sealed class IngestDocumentCommandValidator : AbstractValidator<IngestDocumentCommand>
{
    public IngestDocumentCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Document title is required")
            .MaximumLength(500)
            .WithMessage("Document title cannot exceed 500 characters");

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Document content is required")
            .MinimumLength(10)
            .WithMessage("Document content must be at least 10 characters");
    }
}

