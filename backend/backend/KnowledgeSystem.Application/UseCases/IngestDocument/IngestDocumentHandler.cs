using FluentValidation;
using KnowledgeSystem.Application.Interfaces;
using KnowledgeSystem.Domain.Entities;
using KnowledgeSystem.Domain.Services;

namespace KnowledgeSystem.Application.UseCases.IngestDocument;

/// <summary>
/// Use Case Handler: Orchestrates document ingestion workflow
/// 
/// Workflow:
/// 1. Validate command
/// 2. Create KnowledgeDocument aggregate
/// 3. Section content using domain strategy
/// 4. Generate embeddings for all sections
/// 5. Persist document with sections
/// </summary>
public sealed class IngestDocumentHandler
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IContentSectioningStrategy _sectioningStrategy;
    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly IValidator<IngestDocumentCommand> _validator;

    public IngestDocumentHandler(
        IDocumentRepository documentRepository,
        IContentSectioningStrategy sectioningStrategy,
        IEmbeddingGenerator embeddingGenerator,
        IValidator<IngestDocumentCommand> validator)
    {
        _documentRepository = documentRepository;
        _sectioningStrategy = sectioningStrategy;
        _embeddingGenerator = embeddingGenerator;
        _validator = validator;
    }

    public async Task<IngestDocumentResult> HandleAsync(
        IngestDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        // STEP 1: Validate command
        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        // STEP 2: Create KnowledgeDocument aggregate (Domain)
        var document = KnowledgeDocument.Create(command.Title);

        // STEP 3: Section content using domain strategy
        var sections = _sectioningStrategy.SectionContent(document.Id, command.Content);
        document.AddSections(sections);

        // STEP 4: Generate embeddings for all sections
        var sectionTexts = sections.Select(s => s.Content).ToList();
        var embeddings = await _embeddingGenerator.GenerateEmbeddingsAsync(
            sectionTexts,
            cancellationToken);

        // STEP 5: Assign embeddings to sections
        for (int i = 0; i < sections.Count(); i++)
        {
            sections.ElementAt(i).SetEmbedding(embeddings[i]);
        }

        // STEP 6: Persist document (Infrastructure)
        var documentId = await _documentRepository.SaveAsync(document, cancellationToken);

        // STEP 7: Return result
        return new IngestDocumentResult
        {
            DocumentId = documentId.Value,
            SectionsCreated = document.SectionCount,
            Title = document.Title
        };
    }
}

