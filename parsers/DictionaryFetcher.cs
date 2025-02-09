using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using practice.proxy;
using practice.entities; 

namespace practice.parsers
{
    public class DictionaryFetcher(ProxyRotator proxyRotator, ILogger<DictionaryFetcher> logger)
    {
        private readonly ProxyRotator _proxyRotator = proxyRotator;
        private readonly DictionaryParser _dictionaryParser = new DictionaryParser();
        private readonly ILogger<DictionaryFetcher> _logger = logger;

        /// <summary>
        /// Attempts to fetch the Dictionary.com page using rotated proxies and parse it into a WordCard.
        /// </summary>
        /// <param name="url">The dictionary URL (for example, for "precipitate")</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A parsed WordCard instance.</returns>
        public async Task<WordCard> FetchWordHTMLAsync(string url, CancellationToken cancellationToken)
        {
            const int maxAttempts = 1;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var proxyUrl = "";
                _logger.LogInformation("Attempt {Attempt} using proxy: {Proxy}", attempt,  "23.167.245.41:443");

                try
                {
                    using var handler = new HttpClientHandler();
                    if (!string.IsNullOrEmpty(proxyUrl))
                    {
                        handler.Proxy = new WebProxy(proxyUrl);
                        handler.UseProxy = true;
                    }

                    using var client = new HttpClient(handler)
                    {
                        Timeout = TimeSpan.FromSeconds(30)
                    };

                    var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        var html = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var htmlDoc = new HtmlDocument();
                        htmlDoc.LoadHtml(html);

                        var wordCard = _dictionaryParser.Parse(htmlDoc);
                        return wordCard;
                    }
                    else
                    {
                        _logger.LogWarning("Attempt {Attempt} failed with status code: {StatusCode}", attempt, response.StatusCode);
                        if (!string.IsNullOrEmpty(proxyUrl))
                            _proxyRotator.MarkAsBad(proxyUrl);
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    _logger.LogError(httpEx, "Attempt {Attempt} HttpRequestException", attempt);
                    if (!string.IsNullOrEmpty(proxyUrl))
                        _proxyRotator.MarkAsBad(proxyUrl);
                }
                catch (TaskCanceledException tcEx) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogError(tcEx, "Attempt {Attempt} timed out", attempt);
                    if (!string.IsNullOrEmpty(proxyUrl))
                        _proxyRotator.MarkAsBad(proxyUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Attempt {Attempt} error", attempt);
                    if (!string.IsNullOrEmpty(proxyUrl))
                        _proxyRotator.MarkAsBad(proxyUrl);
                }

                // Невелика затримка між спробами
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
            }

            throw new Exception("Failed to fetch the word card after several attempts.");
        }
    }
}
