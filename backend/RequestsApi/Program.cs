using System.Diagnostics;
using RequestsApi.Middleware;
using RequestsBL;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("RequestsDb")
    ?? throw new InvalidOperationException("Connection string 'RequestsDb' is not configured.");

builder.Services.AddBusinessLayer(connectionString);

// traceId lets a client report an error that we can find in the logs.
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance = context.HttpContext.Request.Path;
        context.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
    });
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// The Angular dev server runs on another origin; production would serve the app from the API's origin or configure its own origins.
const string DevFrontendCorsPolicy = "DevFrontend";
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options => options.AddPolicy(DevFrontendCorsPolicy, policy => policy
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()));
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // XML comments from the API (endpoints) and the BL (DTOs and query parameters).
    foreach (var assemblyName in new[] { "RequestsApi", "RequestsBL" })
    {
        options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.xml"));
    }
});

var app = builder.Build();

// Before the exception handler, so error responses (ProblemDetails) also get the CORS headers
// and the Angular app can read their message.
if (app.Environment.IsDevelopment())
{
    app.UseCors(DevFrontendCorsPolicy);
}

// Catches exceptions from everything after it.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Not in Development: a redirect from http to https on a cross-origin request makes the browser send Origin: null,
// so CORS blocks the Angular dev server. With this, the app works with both the http and the https launch profile.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

// Makes Program visible to WebApplicationFactory<Program> in the integration tests.
public partial class Program;
