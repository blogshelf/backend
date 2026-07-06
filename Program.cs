using backend.initialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

PreExec.Run();

app.Run();