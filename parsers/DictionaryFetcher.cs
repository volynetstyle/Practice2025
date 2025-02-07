using System.Net;
using HtmlAgilityPack;
using ProxyServerAndCrawlerDemo;

public class DictionaryFetcher
{
    private readonly ProxyRotator _proxyRotator;
    private readonly DictionaryParser _dictionaryParser;

    public DictionaryFetcher(ProxyRotator proxyRotator)
    {
        _proxyRotator = proxyRotator;
        _dictionaryParser = new DictionaryParser();
    }

    /// <summary>
    /// Attempts to fetch the Dictionary.com page using rotated proxies and parse it into a WordCard.
    /// </summary>
    /// <param name="url">The dictionary URL (for example, for "precipitate")</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A parsed WordCard instance.</returns>
   public async Task<WordCard> FetchWordHTMLAsync(string url, CancellationToken cancellationToken)
{
    const int maxAttempts = 10;
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        // Get the next available proxy from the rotator
        var proxyUrl = _proxyRotator.GetNextProxy();
        Console.WriteLine($"Attempt {attempt} using proxy: {proxyUrl ?? "none"}");

        try
        {
            using var handler = new HttpClientHandler();
            if (!string.IsNullOrEmpty(proxyUrl))
            {
                // Configure the handler to use the proxy.
                handler.Proxy = new WebProxy(proxyUrl);
                handler.UseProxy = true;
            }

            // Create HttpClient with a longer timeout if needed.
            using var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            // Attempt to fetch the HTML
            var response = await client.GetAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var html = await response.Content.ReadAsStringAsync();

                // Parse HTML using HtmlAgilityPack
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                // Extract word-related data
                var wordCard = DictionaryParser.Parse(htmlDoc);

                return wordCard;
            }
            else
            {
                Console.WriteLine($"Attempt {attempt} failed: {response.StatusCode}");
                _proxyRotator.MarkAsBad(proxyUrl);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Attempt {attempt} error: {ex.Message}");
            _proxyRotator.MarkAsBad(proxyUrl);
        }
    }

    throw new Exception("Failed to fetch the word card after several attempts.");
}
}
