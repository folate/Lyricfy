using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using static System.Text.Json.JsonDocument;

namespace LyricfyLibraries;

public static class LyricsFetcher
{
    private static readonly HttpClient HttpClient = new HttpClient();
    public static LyricsFetcherOptions Options { get; set; }
    
    static LyricsFetcher()
    {
        SetupHeaders();
    }
    
    
    public static async Task<Metadata> GetLyrics(string url)
    {
        url = url.Split("?")[0];
        var credits = await _getCredits(url);
        var fixedTitle = CleanTitle(credits.title);
        var query = HttpUtility.HtmlEncode(fixedTitle) + " " + credits.author.Split(",")[0];
        return new Metadata()
        {
            albumUrl = Options.ProxyImageUrlStart + HttpUtility.UrlEncode(credits.albumUrl),
            author = HttpUtility.HtmlDecode(credits.author),
            title = HttpUtility.HtmlDecode(credits.title),
            lyrics = await _getLyrics(query)
        };
    }
    
    private static async Task<string> _getLyrics(string query)
    {
        var url = await _fetchLyricsUrl(query);
        if (string.IsNullOrEmpty(url))
        {
            return "Error while fetching lyrics. Try again later.";
        }
        
        var lyricsBody = await HttpClient.GetAsync($"{Options.ProxyUrlStart}{HttpUtility.UrlEncode(url)}");
        var lyricsHtmlDoc = new HtmlDocument();
        var lyricsHtml = await lyricsBody.Content.ReadAsStringAsync();
        lyricsHtmlDoc.LoadHtml(lyricsHtml);
        var lyricsNodes = lyricsHtmlDoc.DocumentNode.SelectNodes("//div[@data-lyrics-container='true']");
        var lyrics = string.Empty;
        foreach (var node in lyricsNodes)
        {
            var nodeCopy = node.Clone();
            lyrics += nodeCopy.InnerHtml;
        }
        
        return CleanLyrics(lyrics);
    }
    

    public static async Task<Metadata> GetGenius(string url)
    {
        if (!url.StartsWith("https://genius.com/"))
        {
            return CreateErrorMetadata("<br><br>Please provide a valid Genius URL.");
        }
        var lyricsBody = await HttpClient.GetAsync(Options.ProxyUrlStart + HttpUtility.UrlEncode(url));
        if (!lyricsBody.IsSuccessStatusCode)
        {
            return CreateErrorMetadata("<br><br>Not Found");
        }
        
        var lyricsHtmlDoc = new HtmlDocument();
        var lyricsHtml = await lyricsBody.Content.ReadAsStringAsync();
        lyricsHtmlDoc.LoadHtml(lyricsHtml);
        
        var titleNode = lyricsHtmlDoc.DocumentNode.SelectSingleNode("/html/body/div[1]/main/div[1]/div[3]/div[1]/div[1]/h1/span");
        var authorNode = lyricsHtmlDoc.DocumentNode.SelectSingleNode("/html/body/div[1]/main/div[1]/div[3]/div[1]/div[1]/div[1]/span");
        var imgNode = lyricsHtmlDoc.DocumentNode.SelectSingleNode("//meta[@property='og:image']");
        var albumUrlNode = imgNode?.Attributes["content"].Value;
        
        var lyrics = _extractLyrics(lyricsHtmlDoc);
        return new Metadata()
        {
            albumUrl = Options.ProxyImageUrlStart + HttpUtility.UrlEncode(albumUrlNode),
            author = HttpUtility.HtmlDecode(authorNode.InnerText),
            title = HttpUtility.HtmlDecode(titleNode.InnerText),
            lyrics = CleanLyrics(lyrics)
        };
    }
    
    
    private static string _extractLyrics(HtmlDocument lyricsHtmlDoc)
    {
        var lyricsNodes = lyricsHtmlDoc.DocumentNode.SelectNodes("//div[@data-lyrics-container='true']");
        var lyrics = string.Empty;
        foreach (var node in lyricsNodes)
        {
            var nodeCopy = node.Clone();
            lyrics += nodeCopy.InnerHtml;
        }
        return lyrics;
    }
    
    private static Metadata CreateErrorMetadata(string errorMessage)
    {
        return new Metadata()
        {
            albumUrl = string.Empty,
            author = "Error",
            title = "Error",
            lyrics = errorMessage
        };
    }
    
    
    private static async Task<Credits> _getCredits(string url)
    {
        var response = await HttpClient.GetAsync(Options.ProxyUrlStart + url);
        if (!response.IsSuccessStatusCode)
        {
            return new Credits()
            {
                albumUrl = string.Empty,
                author = "Not Found",
                title = "Not Found",
            };
        }
        var htmlDoc = new HtmlDocument();
        var htmlContent = await response.Content.ReadAsStringAsync();
        htmlDoc.LoadHtml(htmlContent);
        
        var titleNode = htmlDoc.DocumentNode.SelectSingleNode("//meta[@property='og:title']");
        var authorNode = htmlDoc.DocumentNode.SelectSingleNode("//meta[@name='music:musician_description']");
        var albumUrlNode = htmlDoc.DocumentNode.SelectSingleNode("//meta[@property='og:image']");

        var title = titleNode?.Attributes["content"].Value;
        var author = authorNode?.Attributes["content"].Value;
        var albumUrl = albumUrlNode?.Attributes["content"].Value;
        
        return new Credits{ title = title, author = author, albumUrl = albumUrl };
    }
    
    private static string CleanTitle(string title)
    {
        char[] charsToRemove = [',', '.', ';', '\'', ':', '`', '~', '[', ']', '{', '}'];
        var cleanedTitle = HttpUtility.HtmlDecode(title);

        foreach (char c in charsToRemove)
        {
            cleanedTitle = cleanedTitle.Replace(c.ToString(), "");
        }

        return cleanedTitle;
    }
    
    private static async Task<string?> _fetchLyricsUrl(string query)
    {
        query = HttpUtility.UrlEncode($"https://genius.com/api/search/multi?per_page=5&q={query}");
        var response = await HttpClient.GetAsync($"{Options.ProxyUrlStart}{query}");
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        var jdoc = Parse(await response.Content.ReadAsStringAsync());
        try
        {
            return jdoc.RootElement
                .GetProperty("response")
                .GetProperty("sections")[0]
                .GetProperty("hits")[0]
                .GetProperty("result")
                .GetProperty("url").GetString();
        }
        catch (Exception)
        {
            string patternToRemove = @"\(.*?\)";
            string clearedQuery = Regex.Replace(query, patternToRemove, "");
            response = await HttpClient.GetAsync($"{Options.ProxyUrlStart}{clearedQuery}");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            jdoc = Parse(await response.Content.ReadAsStringAsync());
            try
            {
                return jdoc.RootElement
                    .GetProperty("response")
                    .GetProperty("sections")[0]
                    .GetProperty("hits")[0]
                    .GetProperty("result")
                    .GetProperty("url").GetString();
            }
            catch (Exception f)
            {
                Console.WriteLine(f);
                return null;
            }
        }
    }

    private static string CleanLyrics(string lyrics)
    {
        lyrics = lyrics.Replace("<br>", "[br]");
        lyrics = HttpUtility.HtmlDecode(lyrics);
        string pattern = @"<[^>]+>";
        string cleanedLyrics = Regex.Replace(lyrics, pattern, "");
        cleanedLyrics = cleanedLyrics.Replace("[br]", "<br>");
        pattern = @"\[[^\]]*\]";
        cleanedLyrics = Regex.Replace(cleanedLyrics, pattern, "");
        return cleanedLyrics;
    }
    
    private static void SetupHeaders()
    {
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:123.0) Gecko/20100101 Firefox/123.0");
        HttpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
        HttpClient.DefaultRequestHeaders.Add("Accept-Language", "pl-PL,pl;q=0.7");
        HttpClient.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
        HttpClient.DefaultRequestHeaders.Add("Sec-CH-UA", "\"Brave\";v=\"123\", \"Not:A-Brand\";v=\"8\", \"Chromium\";v=\"123\"");
        HttpClient.DefaultRequestHeaders.Add("Sec-CH-UA-Mobile", "?0");
        HttpClient.DefaultRequestHeaders.Add("Sec-CH-UA-Platform", "\"Windows\"");
        HttpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
        HttpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
        HttpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
        HttpClient.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
        HttpClient.DefaultRequestHeaders.Add("Sec-GPC", "1");
        HttpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
    }
    
}
public class LyricsFetcherOptions
{
    public string ProxyImageUrlStart { get; set; }
    public string ProxyUrlStart { get; set; }
}