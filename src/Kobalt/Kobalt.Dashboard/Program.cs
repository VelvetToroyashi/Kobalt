using AspNet.Security.OAuth.Discord;
using Kobalt.Dashboard.Extensions;
using Kobalt.Dashboard.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MudBlazor.Services;
using Remora.Discord.API;
using Remora.Discord.API.Objects;

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

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");


app.MapPost
(
    "api/auth/login",
    async (HttpContext context) =>
    {
        var returnUrl = context.Request.Form["returnUrl"][0]!;
        
        if (context.User.IsAuthenticated())
        {
            return Results.LocalRedirect(returnUrl);
        }

        return Results.Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, new[] { DiscordAuthenticationDefaults.AuthenticationScheme });
    }
);

app.MapPost
(
    "api/logout", async (HttpContext context, ITokenRepository tokens) =>
    {
        if (context.User.IsAuthenticated())
        {
            tokens.RevokeToken(context.User.GetUserID());
        }
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.LocalRedirect("/");
    }
);


app.Run();
