namespace Kobalt.Infrastructure.Types.Booru;

/// <summary>
/// Represents the return result of a booru search.
/// </summary>
/// <param name="SearchUrl">The url to search the booru site with the given tags.</param>
/// <param name="Posts">A dictionary of Image Link ➜ Image Source.</param>
public record QueryResultData(string SearchUrl, IReadOnlyList<Post> Posts);
