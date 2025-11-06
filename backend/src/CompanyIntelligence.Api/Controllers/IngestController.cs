using CompanyIntelligence.Api.Models;
using CompanyIntelligence.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CompanyIntelligence.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IngestController : ControllerBase
    {
        private readonly IngestionService _ingestion;
        public IngestController(IngestionService ingestion) => _ingestion = ingestion;

        [HttpPost]
        [RequestSizeLimit(200_000_000)] // ~200MB
        public async Task<ActionResult<IngestResponse>> Upload([FromForm] IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded");
            var result = await _ingestion.IngestAsync(file, ct);
            return Ok(result);
        }
    }
}
