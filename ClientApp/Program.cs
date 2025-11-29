using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ClientApp;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var apiBase = builder.HostEnvironment.IsDevelopment()
    ? new Uri("https://localhost:7103") // your ServerApp dev URL
    : new Uri(builder.HostEnvironment.BaseAddress);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = apiBase });
// Product API service (wraps HttpClient calls with timeout and better error handling)
builder.Services.AddScoped<ClientApp.Services.IProductApiService, ClientApp.Services.ProductApiService>();

await builder.Build().RunAsync();
