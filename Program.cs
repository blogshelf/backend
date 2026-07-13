using backend.handler;
using backend.initialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton(new Database(builder.Configuration));
builder.Services.AddSingleton(sp => sp.GetRequiredService<Database>().Client);

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

PreExec.Run(app.Services.GetRequiredService<Database>().Client);

// use module registry
app.MapMiscEndpoints();
app.MapSrpEndpoints();

app.Run();