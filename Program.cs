using backend.handler;
using backend.initialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton(Database.Client);

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

PreExec.Run();

app.MapMiscEndpoints();

app.Run();