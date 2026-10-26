using RequestsBL;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("RequestsDb")
    ?? throw new InvalidOperationException("Connection string 'RequestsDb' is not configured.");

builder.Services.AddBusinessLayer(connectionString);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Makes Program visible to WebApplicationFactory<Program> in the integration tests.
public partial class Program;
