using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DotNetEnv;
using ProxyServerAndCrawlerDemo;
using practice.db;
using practice.entities;

class Program
{
    public static async Task Main(string[] args)
    {
        Env.Load();

        var mongoDbService = new MongoDbService(MongoDBConnection.Database);

        // Path to your proxy list file
        var proxyFile = "public/proxy/proxies.txt";
        string[] proxies = File.Exists(proxyFile)
            ? await File.ReadAllLinesAsync(proxyFile)
            : [];

        var proxyRotator = new ProxyRotator(proxies);

        string dictionaryUrl = "https://www.dictionary.com/browse/precipitate";

        var dictionaryFetcher = new DictionaryFetcher(proxyRotator);

        using var cts = new CancellationTokenSource();
        try
        {
            // Attempt to fetch and parse the word card
            WordCard wordCard = await dictionaryFetcher.FetchWordHTMLAsync(dictionaryUrl, cts.Token);
            Console.WriteLine("Parsed Word Card:");
            Console.WriteLine($"Headword: {wordCard.Headword}");

            // Save the WordCard to the MongoDB database
            await mongoDbService.InsertWordCardAsync(wordCard);
            Console.WriteLine($"Word card for '{wordCard.Headword}' saved to the database.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error fetching or saving word card: " + ex.Message);
        }
    }
}
