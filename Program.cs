using backend.handler;
using backend.initialization;
using backend.middleware;
using backend.models;
using Microsoft.AspNetCore.Http.Json;
using Scalar.AspNetCore;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers(options => { options.InputFormatters.Insert(0, new MsgPackInputFormatter()); });
builder.Services.AddSingleton<DotEnv>();
builder.Services.AddSingleton(new Database(builder.Configuration));
builder.Services.AddSingleton(sp => sp.GetRequiredService<Database>().Client);

builder.Services.Configure<JsonOptions>(options => { options.SerializerOptions.AddContext<AppJsonContext>(); });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();
}

app.UseRequestLocalization(new RequestLocalizationOptions()
    .AddSupportedCultures(["en-US","ja-JP","zh-Hans-CN"])
    .SetDefaultCulture("en-US"));
app.UseMiddleware<SignVerifyingMiddleware>();

PreExec.Run(app.Services.GetRequiredService<Database>().Client);

// use module registry
app.MapMiscEndpoints();
app.MapSrpEndpoints();

app.Run();