using Microsoft.AspNetCore.Mvc;
using System.Web;
using Microsoft.AspNetCore.Cors;

[Route("api/[controller]")]
[ApiController]
public class ProxyController : ControllerBase
{
    private readonly HttpClient _client;

    public ProxyController(HttpClient client)
    {
        _client = client;
        SetupClientHeaders();
    }
    
    [Route("image")]
    [HttpGet]
    public async Task<IActionResult> FetchImage(string url)
    {
        url = HttpUtility.HtmlDecode(url);
        url = PrepareUrl(url);
        if (!IsValidImageUrl(url))
        {
            Console.WriteLine(url);
            return BadRequest("Disallowed url");
        }
        var response = await _client.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsByteArrayAsync();
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return File(content, "image/png");
        }

        return StatusCode((int)response.StatusCode);
    }

    [HttpGet]
    public async Task<IActionResult> FetchContent(string url)
    {
        url = PrepareUrl(url);
        if (url.StartsWith("https://open.spotify.com/track"))
        {
            url = url.Split("?")[0];
        }
        else if (url.StartsWith("https://genius.com/"))
        {
            _client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
            _client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            _client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            _client.DefaultRequestHeaders.Add("Host", "genius.com");
            _client.DefaultRequestHeaders.Add("Pragma", "no-cache");
        }
        else
        {
            return BadRequest("Disallowed url");
        }
        
        var response = await _client.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return Ok(content);
        }

        return StatusCode((int)response.StatusCode);
    }
    
    private string PrepareUrl(string url)
    {
        return url.Split("@")[0];
    }
    
    private bool IsValidImageUrl(string url)
    {
        return ((url.EndsWith(".png") || url.EndsWith(".jpg")) && url.StartsWith("https://images.genius.com/")) || url.StartsWith("https://i.scdn.co/image");
    }
    
    private void SetupClientHeaders()
    {
        _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:123.0) Gecko/20100101 Firefox/123.0");
        _client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
        _client.DefaultRequestHeaders.Add("Accept-Language", "pl-PL,pl;q=0.7");
        _client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
        _client.DefaultRequestHeaders.Add("Sec-CH-UA", "\"Brave\";v=\"123\", \"Not:A-Brand\";v=\"8\", \"Chromium\";v=\"123\"");
        _client.DefaultRequestHeaders.Add("Sec-CH-UA-Mobile", "?0");
        _client.DefaultRequestHeaders.Add("Sec-CH-UA-Platform", "\"Windows\"");
        _client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
        _client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
        _client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
        _client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
        _client.DefaultRequestHeaders.Add("Sec-GPC", "1");
        _client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
    }
}