using System.Reflection;
using EstoqueService.Data;
using EstoqueService.Integrations;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var dbPath = Path.Combine(builder.Environment.ContentRootPath, "estoque.db");
builder.Services.AddDbContext<EstoqueDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddHttpClient<DocumentoFiscalApiClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Integracoes:DocumentoFiscalBaseUrl"]
        ?? "http://localhost:5003"));

builder.Services.AddHttpClient<ExpedicaoApiClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Integracoes:ExpedicaoBaseUrl"]
        ?? "http://localhost:5004"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EstoqueDbContext>();
    db.Database.EnsureCreated();
    DbInitializer.Seed(db);
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");
app.MapControllers();

app.Run();
