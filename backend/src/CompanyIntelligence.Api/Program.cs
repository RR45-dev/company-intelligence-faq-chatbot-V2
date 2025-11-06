using CompanyIntelligence.Api.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Bind config
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.Configure<QdrantOptions>(builder.Configuration.GetSection("Qdrant"));

// Env overrides
var cfg = builder.Configuration;
var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (!string.IsNullOrWhiteSpace(openAiKey)) cfg["OpenAI:ApiKey"] = openAiKey;
var openAiModel = Environment.GetEnvironmentVariable("OPENAI_MODEL");
if (!string.IsNullOrWhiteSpace(openAiModel)) cfg["OpenAI:Model"] = openAiModel;
var embModel = Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_MODEL");
if (!string.IsNullOrWhiteSpace(embModel)) cfg["OpenAI:EmbeddingModel"] = embModel;
var qUrl = Environment.GetEnvironmentVariable("QDRANT_URL");
if (!string.IsNullOrWhiteSpace(qUrl)) cfg["Qdrant:Url"] = qUrl;
var qColl = Environment.GetEnvironmentVariable("QDRANT_COLLECTION");
if (!string.IsNullOrWhiteSpace(qColl)) cfg["Qdrant:Collection"] = qColl;

// Services
builder.Services.AddSingleton<HttpClient>();
builder.Services.AddSingleton<OpenAiService>();
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<LlmService>();
builder.Services.AddSingleton<QdrantService>();
builder.Services.AddSingleton<DocumentParserService>();
builder.Services.AddSingleton<ChunkerService>();
builder.Services.AddSingleton<IngestionService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CompanyIntelligence API", Version = "v1" });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.MapControllers();

// Ensure Qdrant collection exists
var qdrant = app.Services.GetRequiredService<QdrantService>();
await qdrant.EnsureCollectionAsync();

app.Run();
