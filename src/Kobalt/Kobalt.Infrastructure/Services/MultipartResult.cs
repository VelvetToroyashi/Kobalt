using Microsoft.AspNetCore.Mvc;

namespace Kobalt.Infrastructure.Services;

public class MultipartResult : IActionResult
{
    private readonly bool _isDiscordAttachment;

    private Stream? _payloadJSON;
    
    private readonly Dictionary<string, Stream> _contents = new();

    public MultipartResult(bool isDiscordAttachment = true)
    {
        _isDiscordAttachment = isDiscordAttachment;
        _payloadJSON = null;
    }

    public MultipartResult(IEnumerable<KeyValuePair<string, Stream>> contents) : this()
    {
        foreach (var item in contents)
        {
            _contents.Add(item.Key, item.Value);
        }
    }

    public MultipartResult AddPayload(Stream json)
    {
        _payloadJSON = json;
        return this;
    }
    
    public MultipartResult Add(string name, Stream content)
    {
        _contents.Add(name, content);
        return this;
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, Stream>> GetEnumerator() => _contents.GetEnumerator();
    
    /// <inheritdoc />
    public async Task ExecuteResultAsync(ActionContext context)
    {
        var contentData = new MultipartFormDataContent();
        
        if (_contents.Count is 0)
        {
            throw new InvalidOperationException("Cannot return an empty multipart result");
        }
        
        if (_payloadJSON is not null)
        {
            contentData.Add(new StreamContent(_payloadJSON), "payload_json");
        }

        var i = 0;
        foreach (var item in this)
        {
            var stream = new StreamContent(item.Value);
            contentData.Add(stream, $"file{i++}", item.Key);
        }
        
        var contentStream = await contentData.ReadAsStreamAsync();
        await contentStream.CopyToAsync(context.HttpContext.Response.Body);
    }
}
