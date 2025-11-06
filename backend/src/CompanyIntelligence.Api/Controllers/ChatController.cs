using CompanyIntelligence.Api.Models;
using CompanyIntelligence.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace CompanyIntelligence.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly EmbeddingService _embed;
        private readonly QdrantService _qdrant;
        private readonly LlmService _llm;

        public ChatController(EmbeddingService embed, QdrantService qdrant, LlmService llm)
        {
            _embed = embed; _qdrant = qdrant; _llm = llm;
        }

        [HttpPost]
        public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Question)) return BadRequest("Question required");
            var qv = await _embed.CreateEmbeddingAsync(req.Question);
            var hits = await _qdrant.SearchAsync(qv, topK: 5);

            var sb = new StringBuilder();
            foreach (var h in hits.Take(5))
            {
                sb.AppendLine($"- {h.Text}");
            }

            var answer = await _llm.GenerateAsync(sb.ToString(), req.Question);
            return Ok(new ChatResponse
            {
                Answer = answer,
                Sources = hits.Select(h => h.Source ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList()
            });
        }
    }
}
