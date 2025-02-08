using DotNetEnv;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using practice.db;
using practice.proxy;
using practice.parsers;

namespace practice
{

    class Program
    {
        public static async Task Main(string[] args)
        {
            // Завантаження змінних середовища
            Env.Load();

            // Налаштування DI-контейнера
            var services = new ServiceCollection();
            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();

            // Отримання екземпляра основного застосунку та запуск
            var app = serviceProvider.GetRequiredService<App>();

            using var cts = new CancellationTokenSource();
            await serviceProvider.GetRequiredService<App>().RunAsync(cts.Token).ConfigureAwait(false);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            string outputFile = "public/proxy/proxies.txt";
            string progressFile = "public/proxy/progress.txt";

            services.AddSingleton(outputFile); // Registering outputFile as a service
            services.AddSingleton(progressFile); // Registering progressFile as a service

            // Налаштування логування
            services.AddLogging(config =>
            {
                config.AddConsole();
                config.SetMinimumLevel(LogLevel.Information);
            });
            services.AddSingleton(sp =>
                           new ProxyUpdater(
                               sp.GetRequiredService<ProxyCrawler>(),
                               sp.GetRequiredService<ProxyChecker>(),
                               sp.GetRequiredService<ProxyRotator>(),
                               outputFile,
                               sp.GetRequiredService<ILogger<ProxyUpdater>>()
                           ));

            // Реєстрація сервісів проксі
            services.AddSingleton<ProxyCrawler>();
            services.AddSingleton<ProxyChecker>();

            // Реєстрація MongoDB-сервісу (припускаємо, що MongoDBConnection.Database налаштовано)
            services.AddSingleton(sp =>
                new MongoDbService(MongoDBConnection.Database));



            // Завантаження початкового списку проксі з файлу (якщо файл існує)
            string proxyFile = "public/proxy/proxies.txt";
            string[] initialProxies = File.Exists(proxyFile) ? File.ReadAllLines(proxyFile) : Array.Empty<string>();
            services.AddSingleton(sp =>
                new ProxyRotator(initialProxies, sp.GetRequiredService<ILogger<ProxyRotator>>()));



            // Реєстрація DictionaryFetcher (він тепер приймає ILogger<DictionaryFetcher>)
            services.AddSingleton<DictionaryFetcher>();

            // Основний клас застосунку
            services.AddTransient<App>();
        }
    }
}
