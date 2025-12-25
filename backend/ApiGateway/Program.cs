var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapPost("/api/ask", async (
    HttpClient client,
    QuestionDto req
) =>
{
    var response = await client.PostAsJsonAsync(
        "http://rag-service:8000/ask",
        req
    );

    return Results.Content(
        await response.Content.ReadAsStringAsync(),
        "application/json"
    );
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();

record QuestionDto(string Question);
