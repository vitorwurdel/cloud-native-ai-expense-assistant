using ExpenseAssistant.Application.Common;
using ExpenseAssistant.Infrastructure.AI;
using ExpenseAssistant.Infrastructure.Persistence;
using ExpenseAssistant.Infrastructure.VectorStore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

builder.Services.Configure<OpenAiOptions>(
    builder.Configuration.GetSection("OpenAI"));

builder.Services.Configure<QdrantOptions>(
    builder.Configuration.GetSection("Qdrant"));

builder.Services.AddHttpClient<IEmbeddingService, OpenAiEmbeddingService>();
builder.Services.AddHttpClient<ILanguageModelService, OpenAiLanguageModelService>();
builder.Services.AddHttpClient<IVectorStoreService, QdrantVectorStoreService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();