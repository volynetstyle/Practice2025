using Microsoft.Extensions.Logging;

namespace practice.proxy
{
  public class ProxyUpdater(ProxyCrawler crawler, ProxyChecker checker, ProxyRotator rotator, string outputFile, ILogger<ProxyUpdater> logger)


    {
    private readonly ProxyCrawler _crawler = crawler;
    private readonly ProxyChecker _checker = checker;
    private readonly ProxyRotator _rotator = rotator;
    private readonly ILogger<ProxyUpdater> _logger = logger;
    private readonly string _outputFile = outputFile;
    private readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(10);

        public async Task RunAsync(CancellationToken cancellationToken)
    {
      while (!cancellationToken.IsCancellationRequested)
      {
        try
        {
          _logger.LogInformation("Starting proxy update cycle.");
          var proxies = await _crawler.FetchProxiesAsync(cancellationToken).ConfigureAwait(false);
          var workingProxies = new List<string>();

          foreach (var proxy in proxies)
          {
    
            if (await ProxyChecker.CheckProxyAsync(proxy, cancellationToken).ConfigureAwait(false))
            {
              workingProxies.Add(proxy.ToString());
            }
          }

          _rotator.UpdateProxies(workingProxies);

          await File.WriteAllLinesAsync(_outputFile, workingProxies, cancellationToken).ConfigureAwait(false);
          _logger.LogInformation("Proxy update cycle completed. {Count} working proxies saved.", workingProxies.Count);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error during proxy update cycle");
        }

        await Task.Delay(_updateInterval, cancellationToken).ConfigureAwait(false);
      }
    }
  }
}