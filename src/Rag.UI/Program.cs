using Rag.UI;
using Rag.UI.Services;
using Rag.UI.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient(HttpClients.RagApiClient, client =>
{
    // Use the API's HTTPS launch URL for local development (ensure the dev cert is trusted in the browser)
    client.BaseAddress = new Uri("https://localhost:7039/");
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddScoped<IFlowbiteService, FlowbiteService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();