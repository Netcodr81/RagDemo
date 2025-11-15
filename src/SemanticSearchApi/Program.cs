using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Qdrant.Client;
using Scalar.AspNetCore;
using SemanticSearchApi.Services;
using SharedKernel.Constants;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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

app.MapGet("/search", async Task<IResult> (
    [FromQuery] string query,
    [FromQuery] int? topK,
    [FromQuery] bool? includeEmbedding,
    [FromServices] DocumentVectorSearch search
) =>
{
    var results = await search.SearchAsync(query, topK ?? 5, includeEmbedding ?? false);
    return TypedResults.Ok(results);
    
});

app.MapGet("collections", async Task<IResult> ([FromServices] QdrantClient qdrantClient) =>
{
    var collections = await qdrantClient.ListFullSnapshotsAsync();
    return TypedResults.Ok(collections);
    
});


await app.RunAsync();
