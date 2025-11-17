using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Qdrant.Client;
using Scalar.AspNetCore;
using SemanticSearchApi.Services;
using SharedKernel.Constants;
using SharedKernel.ResponseModels;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Allow the Blazor WASM app(s) running on localhost to call this API in development.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalDev", policy =>
    {
        policy.WithOrigins("https://localhost:7235", "http://localhost:5165")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var ollamaUri = new Uri(builder.Configuration.GetValue<string>("OllamaUri"));

builder.Services.AddOllamaChatCompletion(OllamaModels.Llama32_1b, ollamaUri);
builder.Services.AddOllamaTextGeneration(OllamaModels.Llama32_1b, ollamaUri);
builder.Services.AddOllamaEmbeddingGenerator(OllamaModels.NomicEmbedText, ollamaUri);

builder.Services.AddSingleton<QdrantClient>(options => new QdrantClient("localhost"));
builder.Services.AddQdrantVectorStore("localhost");
builder.Services.AddSingleton<DocumentVectorSearch>();

builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => { options.Theme = ScalarTheme.Mars; });
}

app.UseHttpsRedirection();

// Enable CORS for local development origins before mapping endpoints
app.UseCors("AllowLocalDev");

app.MapGet("/semantic-search", async Task<IResult> (
    [FromQuery] string query,
    [FromQuery] int? topK,
    [FromQuery] bool? includeEmbedding,
    [FromServices] DocumentVectorSearch search
) =>
{
    var results = await search.SearchAsync(query, topK ?? 5, includeEmbedding ?? false);
    return TypedResults.Ok(new SemanticSearchResponse
    {
        Items = !results.Any()? [] : results.Select(r => new Item(r.Document.DocumentName ?? string.Empty, r.Document.Author ?? string.Empty, r.Document.Content ?? string.Empty, 
            RelevanceScore: Math.Round((decimal)r.Score * 100, 2))).ToList()
    });
    
}).Produces<SemanticSearchResponse>();

app.MapGet("collections", async Task<IResult> ([FromServices] QdrantClient qdrantClient) =>
{
    var collections = await qdrantClient.ListFullSnapshotsAsync();
    return TypedResults.Ok(collections);
    
});


await app.RunAsync();
