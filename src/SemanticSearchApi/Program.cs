using Carter;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using OllamaSharp;
using Scalar.AspNetCore;
using SemanticSearchApi.Registries;
using SemanticSearchApi.Services;
using SharedKernel.Constants;

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
var vectorDbConnectionString = "Host=localhost;Port=5432;Database=vector_db;Username=postgres;Password=postgres";

builder.Services.AddChatClient(new OllamaApiClient(ollamaUri, OllamaModels.Llama32_1b));
builder.Services.AddEmbeddingGenerator(new OllamaApiClient(ollamaUri, OllamaModels.NomicEmbedText));

builder.Services.AddPostgresVectorStore(connectionString: vectorDbConnectionString);
builder.Services.AddSingleton<DocumentVectorSearchService>();
builder.Services.AddSingleton<RagQuestionService>();
builder.Services.AddSingleton<DocumentVectorSearchWithHydeService>();
builder.Services.AddSingleton<PromptService>();

builder.Services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Information));

// Tool registration isn't work yet.
// builder.Services.AddTransient<ChatOptions>(options => new ChatOptions
// {
//     Tools = FunctionRegistry.GetTools(options).ToList()
// });

builder.Services.AddTransient<ChatOptions>();

builder.Services.AddCarter();

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




app.MapCarter();

await app.RunAsync();
