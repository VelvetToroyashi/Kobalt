using AspNet.Security.OAuth.Discord;
using Kobalt.Dashboard.Components;
using Kobalt.Dashboard.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
       .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddAuthentication
(
   options =>
   {
       options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
       options.DefaultChallengeScheme = DiscordAuthenticationDefaults.AuthenticationScheme;
   }
)
.AddDiscord
(
   config =>
   {
       config.ClientId = builder.Configuration["Discord:ClientId"]         ?? throw new InvalidOperationException("Discord Client ID is required.");
       config.ClientSecret = builder.Configuration["Discord:ClientSecret"] ?? throw new InvalidOperationException("Discord Client Secret is required.");
   }
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.MapPost
(
    "api/login",
    async (HttpContext context, [FromForm] string returnUrl = "/") =>
    {
        if (context.User.IsAuthenticated())
        {
            return Results.Redirect(returnUrl);
        }

        return Results.Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, new[] { DiscordAuthenticationDefaults.AuthenticationScheme });
    }
);

app.Run();