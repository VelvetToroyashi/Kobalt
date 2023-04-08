using AngleSharp;
using Remora.Results;

namespace Kobalt.Artists.API.Services;

public class FurAffinityParser
{
    private readonly HttpClient _client;
    
    public FurAffinityParser(IHttpClientFactory client)
    {
        _client = client.CreateClient("FurAffinity");
    }

    public async Task<Result<string>> GetArtistBioAsync(string artist)
    {
        using var response = await _client.GetAsync("https://www.furaffinity.net/user/{artist}/");
        
        if (!response.IsSuccessStatusCode)
        {
            return Result<string>.FromError<HttpRequestException>(new HttpRequestException("Failed to query for the artist's profile. Is FurAffinity down?"));
        }
        
        var html = await response.Content.ReadAsStringAsync();

        if (html.Contains("Log In</strong>"))
        {
            return new InvalidOperationError("The credentials for FurAffinity are invalid.");
        }

        var context = BrowsingContext.New();
        var document = await context.OpenAsync(req => req.Content(html));

        var systemError = document.QuerySelector("h2");
        
        if (systemError?.TextContent == "System Error")
        {
            return new InvalidOperationError("The artist's profile either does not exist, or is disabled.");
        }
        
        var bio = document.QuerySelector("div.section-body.userpage-profile");

        if (bio is null)
        {
            return new InvalidOperationError("The artist's bio could not be retrieved. This could be a bug.");
        }
        
        return bio.InnerHtml;
    }
}
