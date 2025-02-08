using Microsoft.Extensions.Logging;
using practice.db;

using practice.parsers;
using practice.proxy;

namespace practice
{

  /// <summary>
  /// Клас, що містить бізнес-логіку застосунку: отримання даних словника і збереження в MongoDB.
  /// </summary>
  public class App(MongoDbService mongoDbService, DictionaryFetcher dictionaryFetcher, ILogger<App> logger, ProxyUpdater proxyUpdater, ProxyRotator proxyRotator)
  {
    private readonly MongoDbService _mongoDbService = mongoDbService;
    private readonly DictionaryFetcher _dictionaryFetcher = dictionaryFetcher;
    private readonly ILogger<App> _logger = logger;
    private readonly ProxyUpdater _proxyUpdater = proxyUpdater;
    private readonly ProxyRotator _proxyRotator = proxyRotator;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
      string dictionaryUrl = "https://www.dictionary.com/browse/precipitate";
      using var cts = new CancellationTokenSource();

      try
      {
        // var updaterTask = _proxyUpdater.RunAsync(cancellationToken);
        // _logger.LogInformation("Proxy updater started.");

        // while (!cancellationToken.IsCancellationRequested)
        // {
        //   var currentProxy = _proxyRotator.GetNextProxy();
        //   _logger.LogInformation("Using proxy: {Proxy}", currentProxy ?? "none");
        //   await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
        // }

        // await updaterTask.ConfigureAwait(false);

        _logger.LogInformation("Fetching word card from URL: {Url}", dictionaryUrl);
        WordCard wordCard = await _dictionaryFetcher.FetchWordHTMLAsync(dictionaryUrl, cts.Token).ConfigureAwait(false);
        _logger.LogInformation("Parsed Word Card: Headword = {Headword}", wordCard.Headword);

        await _mongoDbService.InsertWordCardAsync(wordCard);
        _logger.LogInformation("Word card for '{Headword}' saved to the database.", wordCard.Headword);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error fetching or saving word card.");
      }


    }
  }

}
