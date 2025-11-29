using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
  options.AddDefaultPolicy(policy => policy
    .AllowAnyOrigin() // dev only — restrict origins for production
    .AllowAnyHeader()
    .AllowAnyMethod()));

// Add server-side caching services
builder.Services.AddMemoryCache();
// Add response caching middleware so clients / proxies can cache GET responses when appropriate
builder.Services.AddResponseCaching();

var app = builder.Build();

// Ensure CORS policy is applied early (keeps browser from raising CORS errors)
app.UseCors();

// Enable response caching middleware. Note: response caching honors headers on the response
// (Cache-Control) set below in the endpoint or via attributes. This is lightweight and
// works well for public read-only GET endpoints.
app.UseResponseCaching();

// Global exception handler: always return JSON so the client won't receive HTML error pages
// which can cause parsing errors on the client and noisy console logs.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json; charset=utf-8";

        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var message = builder.Environment.IsDevelopment()
            ? feature?.Error?.Message ?? "Unexpected server error"
            : "An unexpected server error occurred.";

        var payload = new { Error = message };
        await context.Response.WriteAsJsonAsync(payload);
    });
});
// Simple products API endpoint & Return sample product data
app.MapGet("/api/products", async (IMemoryCache cache, HttpContext httpContext) =>
{
    const string cacheKey = "products_v1";

    // Use GetOrCreateAsync to avoid cache-stampede and keep creation logic centralized.
    var products = await cache.GetOrCreateAsync(cacheKey, async entry =>
    {
        // In a real app this is where you'd load from DB / remote API.
        // Keep cached entries short-lived for this sample; tune as needed.
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60); // keep in-memory for 60s
        entry.SlidingExpiration = TimeSpan.FromSeconds(20);

        // Simulate load
        await Task.CompletedTask;

        return new[]
        {
            new { Id = 1, Name = "Laptop", Price = 1200.50, Stock = 25 },
            new { Id = 2, Name = "Headphones", Price = 50.00, Stock = 100 }
        };
    });

    // Add headers so downstream caches (proxy / browser) know they can cache the response for a short time.
    // ResponseCaching middleware will use this header when deciding to cache in-memory at the server/proxy.
    httpContext.Response.GetTypedHeaders().CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
    {
        Public = true,
        MaxAge = TimeSpan.FromSeconds(30)
    };

    // Also set an explicit header for older clients or proxies.
    httpContext.Response.Headers["Cache-Control"] = "public, max-age=30";

    return Results.Json(products);
});

// Simple endpoint that allows cache invalidation in dev/testing scenarios.
// In production you would secure this endpoint or perform invalidation when the underlying data changes.
app.MapPost("/api/products/refresh", (IMemoryCache cache) =>
{
    const string cacheKey = "products_v1";
    cache.Remove(cacheKey);
    return Results.Ok(new { Refreshed = true });
});

app.Run();