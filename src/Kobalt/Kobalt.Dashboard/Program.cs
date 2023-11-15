using System.Net.Http.Headers;
using AspNet.Security.OAuth.Discord;
using Kobalt.Dashboard.Extensions;
using Kobalt.Dashboard.Services;
using Kobalt.Dashboard.Services.Remora;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MudBlazor.Services;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Caching.Extensions;
using Remora.Discord.Caching.Services;
using Remora.Discord.Rest;
using Remora.Discord.Rest.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

builder.Services.AddAuthentication
(
    options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = DiscordAuthenticationDefaults.AuthenticationScheme;
    }
)
.AddCookie
(
    opt =>
    {
        opt.ExpireTimeSpan = TimeSpan.FromDays(7);
    }
)
.AddDiscord
(
    config =>
    {
        config.UsePkce = true;
        config.SaveTokens = true;

        config.Scope.Add("guilds");

        config.ClientId = builder.Configuration["Discord:ClientId"]         ?? throw new InvalidOperationException("Discord Client ID is required.");
        config.ClientSecret = builder.Configuration["Discord:ClientSecret"] ?? throw new InvalidOperationException("Discord Client Secret is required.");

        config.Events.OnCreatingTicket +=  (context) =>
        {
            var services = context.HttpContext.RequestServices;

            var tokenRepo = services.GetRequiredService<ITokenRepository>();
            tokenRepo.SetToken(context);
            
            return Task.CompletedTask;
        };

        config.ClaimActions.MapCustomJson
        (
            "urn:discord:avatar:url",
            user =>
            {
                var id = ulong.Parse(user.GetString("id")!);
                
                if (user.GetString("avatar") is {} avatarHash)
                {
                    var avatar = CDN.GetUserAvatarUrl(DiscordSnowflake.New(id), new ImageHash(avatarHash));
                    return avatar.Entity.ToString();
                }
                
                return CDN.GetDefaultUserAvatarUrl(DiscordSnowflake.New(id)).Entity.ToString();
            }
        );
        
        config.ClaimActions.MapCustomJson("urn:discord:username", user => user.GetString("global_name") ?? user.GetString("username")!);
    }
);

builder.Services.AddSingleton<ITokenRepository, TokenRepository>();

builder.Services.AddAuthorization();

builder.Services.AddScoped<AuthenticationStateProvider, DiscordAuthenticationStateProvider>();
builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddDiscordRest(_ => ("Dummy token", DiscordTokenType.Bearer));
builder.Services.AddSingleton<IAsyncTokenStore>(s => s.GetRequiredService<ITokenRepository>());

builder.Services.AddDiscordCaching()
.Configure<CacheSettings>(c => c.SetAbsoluteExpiration<IReadOnlyList<IPartialGuild>>(TimeSpan.FromMinutes(10)));

builder.Services.Decorate<IDiscordRestUserAPI, TokenScopedDiscordRestUserAPI>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.MapGet
(
    "api/auth/login",
    (HttpContext context, [FromQuery] string returnUrl = "/") =>
    {
        if (context.User.IsAuthenticated())
        {
            return Results.Redirect(returnUrl);
        }

        var authenticate = new AuthenticationProperties { RedirectUri = returnUrl };
        return Results.Challenge(authenticate, new[] { DiscordAuthenticationDefaults.AuthenticationScheme });
    }
);

app.MapPost
(
    "api/auth/logout", async (HttpContext context, ITokenRepository tokens) =>
    {
        if (context.User.IsAuthenticated())
        {
            var token = tokens.GetToken(context.User.GetUserID());

            if (token is { AccessToken: not null })
            {
                var bearer = new AuthenticationHeaderValue("Bearer", token.AccessToken);
                // TODO: Use IHttpClientFactory
                using var client = new HttpClient();
                
                client.DefaultRequestHeaders.Authorization = bearer;
                
                await client.PostAsync("https://discord.com/api/oauth2/token/revoke", new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["token"] = token.AccessToken
                }));
            }
            
            tokens.RevokeToken(context.User.GetUserID());
        }
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/");
    }
);

app.Run();
