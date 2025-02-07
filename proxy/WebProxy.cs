using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ProxyServerAndCrawlerDemo
{
    /// <summary>
    /// Manages rotation through a list of proxies, skipping non-working ones
    /// </summary>
    public class ProxyRotator
    {
        private readonly List<string> _proxies;
        private int _currentIndex = 0;
        private readonly ConcurrentDictionary<string, DateTime> _badProxies = new();
        private readonly object _lock = new();

        public ProxyRotator(IEnumerable<string> proxies)
        {
            _proxies = proxies.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        }

        public string GetNextProxy()
        {
            lock (_lock)
            {
                if (_proxies.Count == 0) return null;

                for (int i = 0; i < _proxies.Count; i++)
                {
                    int index = (_currentIndex + i) % _proxies.Count;
                    var proxy = _proxies[index];
                    
                    if (!_badProxies.ContainsKey(proxy))
                    {
                        _currentIndex = (index + 1) % _proxies.Count;
                        return proxy;
                    }
                    
                    // Clear expired bad proxies
                    if (_badProxies.TryGetValue(proxy, out var markedTime) && 
                        DateTime.Now - markedTime > TimeSpan.FromMinutes(5))
                    {
                        _badProxies.TryRemove(proxy, out _);
                        _currentIndex = (index + 1) % _proxies.Count;
                        return proxy;
                    }
                }

                return null; // All proxies marked bad temporarily
            }
        }

        public void MarkAsBad(string proxy)
        {
            if (!string.IsNullOrEmpty(proxy))
                _badProxies.TryAdd(proxy, DateTime.Now);
        }

        public int Count => _proxies.Count;
    }

    /// <summary>
    /// Optimized HTTP proxy server using HttpClient
    /// </summary>
    public class HttpProxyServer
    {
        private readonly HttpListener _listener;
        private bool _isRunning = false;

        public HttpProxyServer(string prefix)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _isRunning = true;
            _listener.Start();
            Console.WriteLine($"Proxy server started on: {string.Join(", ", _listener.Prefixes)}");

            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = HandleRequestAsync(context, cancellationToken);
                }
                catch (Exception ex) when (ex is HttpListenerException || ex is OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancelToken)
        {
            try
            {
                var request = context.Request;
                var targetUrl = request.Url.ToString();
                Console.WriteLine($"Proxying: {targetUrl}");

                using var client = new HttpClient(new HttpClientHandler 
                { 
                    AllowAutoRedirect = false 
                });
                
                var targetRequest = new HttpRequestMessage(
                    new HttpMethod(request.HttpMethod), 
                    targetUrl
                );

                // Copy headers
                foreach (string key in request.Headers.AllKeys)
                {
                    if (key.Equals("Host", StringComparison.OrdinalIgnoreCase)) continue;
                    
                    if (!targetRequest.Headers.TryAddWithoutValidation(key, request.Headers[key]))
                    {
                        targetRequest.Content?.Headers.TryAddWithoutValidation(key, request.Headers[key]);
                    }
                }

                // Copy body
                if (request.HasEntityBody)
                {
                    targetRequest.Content = new StreamContent(request.InputStream);
                }

                // Send request
                using var targetResponse = await client.SendAsync(targetRequest, cancelToken);
                
                // Copy response
                context.Response.StatusCode = (int)targetResponse.StatusCode;
                foreach (var header in targetResponse.Headers)
                {
                    context.Response.Headers.Add(header.Key, string.Join(", ", header.Value));
                }
                
                foreach (var header in targetResponse.Content.Headers)
                {
                    context.Response.Headers.Add(header.Key, string.Join(", ", header.Value));
                }

                await using var responseStream = await targetResponse.Content.ReadAsStreamAsync();
                await responseStream.CopyToAsync(context.Response.OutputStream, cancelToken);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadGateway;
                await using var writer = new StreamWriter(context.Response.OutputStream);
                await writer.WriteAsync($"Proxy error: {ex.Message}");
            }
            finally
            {
                context.Response.Close();
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener.Stop();
            Console.WriteLine("Proxy server stopped.");
        }
    }

    /// <summary>
    /// Enhanced crawler with proxy rotation support
    /// </summary>
    public class FreeProxyCrawler
    {
        private readonly string _outputFile;
        private readonly string _progressFile;
        private readonly ProxyRotator _proxyRotator;
        private readonly Regex _proxyRegex = new(
            @"<td>(\d{1,3}(?:\.\d{1,3}){3})<\/td>\s*<td>(\d+)<\/td>", 
            RegexOptions.Compiled
        );

        public FreeProxyCrawler(string outputFile, string progressFile, ProxyRotator proxyRotator)
        {
            _outputFile = outputFile;
            _progressFile = progressFile;
            _proxyRotator = proxyRotator;
        }

        public async Task<List<string>> CrawlAsync(CancellationToken cancellationToken)
        {
            var proxies = new List<string>();
            const string url = "https://free-proxy-list.net/";
            Console.WriteLine($"Starting crawl: {url}");

            var (success, html) = await FetchWithRetries(url, cancellationToken);
            if (!success) return proxies;

            var matches = _proxyRegex.Matches(html);
            Console.WriteLine($"Found {matches.Count} potential proxies");

            foreach (Match match in matches)
            {
                if (cancellationToken.IsCancellationRequested) break;
                
                var proxy = $"{match.Groups[1].Value}:{match.Groups[2].Value}";
                proxies.Add(proxy);
                await SaveProgressAsync(proxies.Count, matches.Count, proxy);
            }

            await File.WriteAllLinesAsync(_outputFile, proxies, cancellationToken);
            Console.WriteLine($"Crawl completed. Saved {proxies.Count} proxies");
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
                        handler.Proxy = new WebProxy(proxyUrl);
                        handler.UseProxy = true;
                    }

                    using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
                    var response = await client.GetAsync(url, cancelToken);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        return (true, await response.Content.ReadAsStringAsync());
                    }
                    
                    Console.WriteLine($"Attempt {attempt} failed: {response.StatusCode}");
                    _proxyRotator.MarkAsBad(proxyUrl);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Attempt {attempt} error: {ex.Message}");
                    _proxyRotator.MarkAsBad(proxyUrl);
                }
            }

            return (false, null);
        }

        private async Task SaveProgressAsync(int current, int total, string lastProxy)
        {
            var progress = $"Progress: {current}/{total}. Last proxy: {lastProxy}";
            Console.WriteLine(progress);
            await File.WriteAllTextAsync(_progressFile, progress);
        }
    }
}