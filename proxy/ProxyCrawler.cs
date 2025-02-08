using HtmlAgilityPack;
using Microsoft.Extensions.Logging;


namespace practice.proxy
{
  public class ProxyInfo
  {
    public required string IP { get; set; }
    public required string Port { get; set; }
    public bool Https { get; set; }
    public bool Google { get; set; }
    public override string ToString() => $"{IP}:{Port}";
  }

  public class ProxyCrawler(ILogger<ProxyCrawler> logger, string outputFile, string progressFile, ProxyRotator proxyRotator)
    {
    private readonly ILogger<ProxyCrawler> _logger = logger;
    private readonly string _outputFile = outputFile;
    private readonly string _progressFile = progressFile;
    private readonly ProxyRotator _proxyRotator = proxyRotator;

    public async Task<List<ProxyInfo>> FetchProxiesAsync(CancellationToken cancellationToken)
    {
      var proxies = new List<ProxyInfo>();
      const string url = "https://free-proxy-list.net/";

      try
      {
        var (success, html) = await FetchWithRetries(url, cancellationToken);
        if (!success) return proxies;

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        // Find the table with id="proxylisttable"
        var table = htmlDoc.DocumentNode.SelectSingleNode("//table[@class='table table-striped table-bordered']");
        if (table != null)
        {
          var rows = table.SelectNodes(".//tbody/tr");
          if (rows != null)
          {
            foreach (var row in rows)
            {
              var cells = row.SelectNodes("td");
              if (cells != null && cells.Count >= 8)
              {
                var proxy = new ProxyInfo
                {
                  IP = cells[0].InnerText.Trim(),
                  Port = cells[1].InnerText.Trim(),
                  Google = cells[5].InnerText.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase),
                  Https = cells[6].InnerText.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase)
                };

                // Filter by HTTPS support
                if (proxy.Https)
                {
                  proxies.Add(proxy);
                }
              }
            }
          }
        }

        // Save proxies to file
        await File.WriteAllLinesAsync(_outputFile, proxies.ConvertAll(p => p.ToString()), cancellationToken);
        _logger.LogInformation($"Crawl completed. Saved {proxies.Count} proxies.");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error fetching proxies");
      }

      return proxies;
    }

    private async Task<(bool success, string html)> FetchWithRetries(string url, CancellationToken cancelToken)
    {
      const int maxAttempts = 3;
      for (int attempt = 1; attempt <= maxAttempts; attempt++)
      {
        var proxyUrl = _proxyRotator.GetNextProxy();
        try
        {
          using var handler = new HttpClientHandler();
          if (!string.IsNullOrEmpty(proxyUrl))
          {
            handler.Proxy = new System.Net.WebProxy(proxyUrl);
            handler.UseProxy = true;
          }

          using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
          var response = await client.GetAsync(url, cancelToken);

          if (response.IsSuccessStatusCode)
          {
            return (true, await response.Content.ReadAsStringAsync());
          }

          _logger.LogWarning($"Attempt {attempt} failed: {response.StatusCode}");
          _proxyRotator.MarkAsBad(proxyUrl);
        }
        catch (Exception ex)
        {
          _logger.LogWarning($"Attempt {attempt} error: {ex.Message}");
          _proxyRotator.MarkAsBad(proxyUrl);
        }
      }

      return (false, null);
    }
  }
}
