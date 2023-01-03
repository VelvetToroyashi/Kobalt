using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Web;
using Kobalt.Infrastructure.Types.Booru;
using Kobalt.Infrastructure.Types.Results;
using Microsoft.Extensions.Options;
using Remora.Rest.Json.Policies;
using Remora.Results;

namespace Kobalt.Infrastructure.Services.Booru;

/// <summary>
/// Represents a service that interfaces with image booru boards such as e621, e926, and Derpibooru.
/// </summary>
public sealed class BooruSearchService
{
    private const string _e621Url = "https://e621.net/posts.json";
    private const string _e6MaxLimit = "320";

    private readonly string[] Blacklist = new[]
    {
        "gore",
        "scat",
        "cub",
        "beastiality",
    };

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = new SnakeCaseNamingPolicy()
    };
    
    private readonly HttpClient _client;

    public BooruSearchService(IHttpClientFactory clientFactory)
    {
        _client = clientFactory.CreateClient("booru");
        // Set the default user agent for the HttpClient.
        //TODO: Version number should be injected somewhere?
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("Kobalt/1.0 (by VelvetThePanda)");
    }

    public async Task<Result<QueryResultData>> SearchAsync(int count, string tags)
    {
        if (tags.Split(' ').FirstOrDefault(t => Blacklist.Contains(t)) is {} blacklisted)
        {
            return Result<QueryResultData>.FromError(new BlacklistedError($"`{blacklisted}` is a blacklisted tag, and cannot be searched."));
        }
        
        var search = $"{_e621Url}?limit={_e6MaxLimit}&tags={tags.Replace(' ', '+')}";

        var request = new HttpRequestMessage(HttpMethod.Get, search);
        
        var response = await _client.SendAsync(request);

        var content = await response.Content.ReadFromJsonAsync<PostHolder>(_jsonOptions);

        var deserialized = content.Posts;
        
        if (!deserialized.Any())
        {
            return new NotFoundError("Search yielded no results.");
        }
        
        var posts = new HashSet<Post>(count);
        
        for (int i = 0; i < count; i++)
        {
            Post post;
            var j = 0;
            
            do
            {
                j++;
                post = deserialized[Random.Shared.Next(0, deserialized.Count)];
            }
            while (post.File.Url is not null && !posts.Add(post) && j < deserialized.Count);
        }

        if (!posts.Any()) // A strange edge-case, but a possible nonetheless.
        {
            return new NotFoundError("Search yielded no results.");
        }
        
        return Result<QueryResultData>.FromSuccess(new(search, posts.ToArray()));
    }
}
