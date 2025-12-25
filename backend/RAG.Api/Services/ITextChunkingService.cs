namespace RAG.Api.Services;

public interface ITextChunkingService
{
    /// <summary>
    /// Metni belirtilen boyutta parçalara (chunk) böler
    /// </summary>
    /// <param name="text">Bölünecek metin</param>
    /// <param name="maxChunkSize">Maksimum chunk boyutu (karakter)</param>
    /// <param name="overlap">Chunk'lar arası örtüşme (karakter)</param>
    /// <returns>Chunk listesi</returns>
    List<string> ChunkText(string text, int maxChunkSize = 500, int overlap = 50);
}





