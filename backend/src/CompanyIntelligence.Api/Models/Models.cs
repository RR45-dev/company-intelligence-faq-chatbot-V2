namespace CompanyIntelligence.Api.Models
{
    public record ChatRequest(string Question);
    public class ChatResponse
    {
        public required string Answer { get; set; }
        public List<string> Sources { get; set; } = new();
    }

    public class IngestResponse
    {
        public int ChunksCreated { get; set; }
        public int VectorsUpserted { get; set; }
        public string? FileName { get; set; }
    }

    public class ChunkRecord
    {
        public required string Id { get; set; }
        public required string Text { get; set; }
        public string? Source { get; set; }
    }

    public class SearchResult
    {
        public required string Id { get; set; }
        public required string Text { get; set; }
        public double Score { get; set; }
        public string? Source { get; set; }
    }
}
