using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Rag.UI;
using Rag.UI.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient(HttpClients.RagUiClient, client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});

builder.Services.AddHttpClient(HttpClients.RagApiClient, client =>
{
    // Use the API's HTTPS launch URL for local development (ensure the dev cert is trusted in the browser)
    client.BaseAddress = new Uri("https://localhost:7039/");
});

builder.Services.AddScoped<IFlowbiteService, FlowbiteService>();

await builder.Build().RunAsync();