using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CompanyIntelligence.Api.Models;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace CompanyIntelligence.Api.Services
{
    public class OpenAiOptions
    {
        public string ApiKey { get; set; } = "";
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";
        public string Model { get; set; } = "gpt-4o-mini";
        public string EmbeddingModel { get; set; } = "text-embedding-3-large";
    }

    public class QdrantOptions
    {
        public string Url { get; set; } = "http://localhost:6333";
        public string Collection { get; set; } = "company_knowledge";
        public int VectorSize { get; set; } = 3072;
        public string Distance { get; set; } = "Cosine";
    }

    public class OpenAiService
    {
        private readonly HttpClient _http;
        private readonly OpenAiOptions _opt;

        public OpenAiService(HttpClient http, IOptions<OpenAiOptions> opt)
        {
            _http = http;
            _opt = opt.Value;
        }

        private HttpRequestMessage Create(string path, object payload)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"{_opt.BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}");
            req.Headers.Add("Authorization", $"Bearer {_opt.ApiKey}");
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            return req;
        }

        public async Task<float[]> CreateEmbeddingAsync(string text)
        {
            var payload = new
            {
                input = text,
                model = _opt.EmbeddingModel
            };
            using var req = Create("embeddings", payload);
            var res = await _http.SendAsync(req);
            res.EnsureSuccessStatusCode();
            using var stream = await res.Content.ReadAsStreamAsync();
            var doc = await JsonDocument.ParseAsync(stream);
            var vec = doc.RootElement.GetProperty("data")[0].GetProperty("embedding").EnumerateArray()
                .Select(x => x.GetSingle()).ToArray();
            return vec.Select(v => (float)v).ToArray();
        }

        public async Task<string> ChatAsync(string prompt)
        {
            var payload = new
            {
                model = _opt.Model,
                messages = new[] {
                    new { role = "system", content = "You are a helpful assistant that answers using only the provided CONTEXT. If the answer is not in the context, say you don't have enough data."},
                    new { role = "user", content = prompt }
                },
                temperature = 0.2
            };
            using var req = Create("chat/completions", payload);
            var res = await _http.SendAsync(req);
            res.EnsureSuccessStatusCode();
            using var stream = await res.Content.ReadAsStreamAsync();
            var doc = await JsonDocument.ParseAsync(stream);
            var msg = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            return msg ?? string.Empty;
        }
    }

    public class EmbeddingService
    {
        private readonly OpenAiService _openAi;
        public EmbeddingService(OpenAiService openAi) => _openAi = openAi;
        public Task<float[]> CreateEmbeddingAsync(string text) => _openAi.CreateEmbeddingAsync(text);
    }

    public class LlmService
    {
        private readonly OpenAiService _openAi;
        public LlmService(OpenAiService openAi) => _openAi = openAi;

        public Task<string> GenerateAsync(string context, string question)
        {
            var prompt = $"CONTEXT:\n{context}\n\nQUESTION: {question}\n\nINSTRUCTIONS: Answer using only the CONTEXT above. If context is insufficient, say 'Not enough data.'";
            return _openAi.ChatAsync(prompt);
        }
    }

    public class QdrantService
    {
        private readonly HttpClient _http;
        private readonly QdrantOptions _opt;

        public QdrantService(HttpClient http, IOptions<QdrantOptions> opt)
        {
            _http = http;
            _opt = opt.Value;
        }

        private string CollUrl => $"{_opt.Url.TrimEnd('/')}/collections/{_opt.Collection}";

        public async Task EnsureCollectionAsync()
        {
            var res = await _http.GetAsync(CollUrl);
            if (res.IsSuccessStatusCode) return;

            var payload = new
            {
                vectors = new
                {
                    size = _opt.VectorSize,
                    distance = _opt.Distance.ToLower() == "cosine" ? "Cosine" : _opt.Distance
                }
            };
            var req = new HttpRequestMessage(HttpMethod.Put, CollUrl);
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var createRes = await _http.SendAsync(req);
            createRes.EnsureSuccessStatusCode();
        }

        public async Task UpsertAsync(IEnumerable<ChunkRecord> chunks, IEnumerable<float[]> embeddings)
        {
            var points = chunks.Zip(embeddings, (c, e) => new
            {
                id = c.Id,
                vector = e,
                payload = new { text = c.Text, source = c.Source }
            }).ToArray();

            var payload = new { points };
            var req = new HttpRequestMessage(HttpMethod.Put, $"{CollUrl}/points?wait=true");
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var res = await _http.SendAsync(req);
            res.EnsureSuccessStatusCode();
        }

        public async Task<List<SearchResult>> SearchAsync(float[] queryVector, int topK = 5)
        {
            var payload = new
            {
                vector = queryVector,
                limit = topK,
                with_payload = true
            };
            var req = new HttpRequestMessage(HttpMethod.Post, $"{CollUrl}/points/search");
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var res = await _http.SendAsync(req);
            res.EnsureSuccessStatusCode();
            using var stream = await res.Content.ReadAsStreamAsync();
            var doc = await JsonDocument.ParseAsync(stream);
            var results = new List<SearchResult>();
            foreach (var r in doc.RootElement.GetProperty("result").EnumerateArray())
            {
                var id = r.GetProperty("id").ToString();
                var score = r.GetProperty("score").GetDouble();
                var payloadItem = r.GetProperty("payload");
                var text = payloadItem.GetProperty("text").GetString() ?? "";
                var source = payloadItem.TryGetProperty("source", out var s) ? s.GetString() : null;
                results.Add(new SearchResult { Id = id, Text = text, Score = score, Source = source });
            }
            return results;
        }
    }

    public class DocumentParserService
    {
        public async Task<string> ExtractTextAsync(IFormFile file, CancellationToken ct = default)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension == ".pdf")
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms, ct);
                ms.Position = 0;
                using var doc = PdfDocument.Open(ms);
                var sb = new StringBuilder();
                foreach (var page in doc.GetPages())
                {
                    var text = ContentOrderTextExtractor.GetText(page);
                    sb.AppendLine(text);
                }
                return sb.ToString();
            }
            else if (extension == ".txt")
            {
                using var reader = new StreamReader(file.OpenReadStream());
                return await reader.ReadToEndAsync();
            }
            else
            {
                throw new InvalidOperationException("Unsupported file type. Use PDF or TXT for this starter.");
            }
        }
    }

    public class ChunkerService
    {
        // naive word-based chunking ~500 tokens/section (good enough starter)
        public List<ChunkRecord> Chunk(string text, string source, int maxWords = 350)
        {
            var words = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var chunks = new List<ChunkRecord>();
            var buffer = new List<string>();
            int i = 0, chunkId = 0;

            foreach (var w in words)
            {
                buffer.Add(w);
                i++;
                if (i >= maxWords)
                {
                    chunks.Add(new ChunkRecord
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Text = string.Join(' ', buffer),
                        Source = source
                    });
                    buffer.Clear();
                    i = 0;
                    chunkId++;
                }
            }
            if (buffer.Count > 0)
            {
                chunks.Add(new ChunkRecord
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Text = string.Join(' ', buffer),
                    Source = source
                });
            }
            return chunks;
        }
    }

    public class IngestionService
    {
        private readonly DocumentParserService _parser;
        private readonly ChunkerService _chunker;
        private readonly EmbeddingService _embed;
        private readonly QdrantService _qdrant;

        public IngestionService(DocumentParserService parser, ChunkerService chunker, EmbeddingService embed, QdrantService qdrant)
        {
            _parser = parser; _chunker = chunker; _embed = embed; _qdrant = qdrant;
        }

        public async Task<IngestResponse> IngestAsync(IFormFile file, CancellationToken ct = default)
        {
            var text = await _parser.ExtractTextAsync(file, ct);
            var chunks = _chunker.Chunk(text, file.FileName);
            var embeddings = new List<float[]>();
            foreach (var c in chunks)
            {
                embeddings.Add(await _embed.CreateEmbeddingAsync(c.Text));
            }
            await _qdrant.UpsertAsync(chunks, embeddings);
            return new IngestResponse { ChunksCreated = chunks.Count, VectorsUpserted = embeddings.Count, FileName = file.FileName };
        }
    }
}
