using Kobalt.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSerilogLogging();

var app = builder.Build();
app.MapControllers();

app.MapPut("/infractions/guilds/{guildID}", () => {});
app.MapGet("/infractions/guilds/{guildID}/{id}", () => {});
app.MapPatch("/infractions/guilds/{guildID}/{id}", () => {});
app.MapGet("/infractions/guilds/{guildID}/users/{id}", () => {});

app.MapGet("/infractions/{guildID}/rules", () => {});
app.MapPost("/infractions/{guildID}/rules", () => {});
app.MapPatch("/infractions/{guildID}/rules/{id}", () => {});
app.MapDelete("/infractions/{guildID}/rules/{id}", () => {});

app.Run();
