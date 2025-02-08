using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace practice.proxy
{
  public class ProxyRotator(IEnumerable<string> proxies, ILogger<ProxyRotator> logger)
    {
    private readonly List<string> _proxies = new List<string>(proxies);
    private int _index = 0;
    private readonly object _lock = new object();
    private readonly ILogger<ProxyRotator> _logger = logger;

        public string? GetNextProxy()
    {
      lock (_lock)
      {
        if (_proxies.Count == 0)
          return null;
        var proxy = _proxies[_index];
        _index = (_index + 1) % _proxies.Count;
        return proxy;
      }
    }

    // Оновлення списку проксі новими робочими проксі
    public void UpdateProxies(IEnumerable<string> newProxies)
    {
      lock (_lock)
      {
        _proxies.Clear();
        _proxies.AddRange(newProxies);
        _index = 0;
        _logger.LogInformation("Updated proxy list with {Count} proxies", _proxies.Count);
      }
    }

    public IEnumerable<string> GetAllProxies()
    {
      lock (_lock)
      {
        return [.. _proxies];
      }
    }

    public void MarkAsBad(string proxy)
    {
      lock (_lock)
      {
        if (_proxies.Remove(proxy))
          _logger.LogInformation("Removed bad proxy: {Proxy}", proxy);
      }
    }
  }
}