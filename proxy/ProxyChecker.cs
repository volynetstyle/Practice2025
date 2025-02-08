
using System.Net;

namespace practice.proxy
{

    public class ProxyChecker
    {
        private static SemaphoreSlim consoleSemaphore = new SemaphoreSlim(1, 1);
        private static int currentProxyNumber = 0;

        public static async Task<List<string>> GetWorkingProxies(List<string> proxies)
        {
            var tasks = new List<Task<Tuple<string, bool>>>();

            foreach (var proxyUrl in proxies)
            {
                tasks.Add(CheckProxy(proxyUrl, proxies.Count));
            }

            var results = await Task.WhenAll(tasks);

            var workingProxies = new List<string>();
            foreach (var result in results)
            {
                if (result.Item2)
                {
                    workingProxies.Add(result.Item1);
                }
            }

            return workingProxies;
        }

        private static async Task<Tuple<string, bool>> CheckProxy(string proxyUrl, int totalProxies)
        {
            var client = ProxyHttpClient.CreateClient(proxyUrl);
            bool isWorking = await IsProxyWorking(client);

            await consoleSemaphore.WaitAsync();
            try
            {
                currentProxyNumber++;
                Console.WriteLine($"Proxy: {currentProxyNumber} de {totalProxies}");
            }
            finally
            {
                consoleSemaphore.Release();
            }

            return new Tuple<string, bool>(proxyUrl, isWorking);
        }

        private static async Task<bool> IsProxyWorking(HttpClient client)
        {
            try
            {
                var testUrl = "http://www.google.com";
                var response = await client.GetAsync(testUrl);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        internal static async Task<bool> CheckProxyAsync(ProxyInfo proxy, CancellationToken cancellationToken)
        {
            // Create a proxy handler
            var proxyHandler = new HttpClientHandler
            {
                Proxy = new WebProxy(proxy.IP, int.Parse(proxy.Port)),
                UseProxy = true
            };

            // Create an HttpClient with the proxy handler
            using var client = new HttpClient(proxyHandler);

            try
            {
                // Set a timeout for the request
                client.Timeout = TimeSpan.FromSeconds(5);

                // Send a test request to a known URL
                var response = await client.GetAsync("https://www.google.com", cancellationToken);

                // Return true if the response is successful
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                // Return false if an exception occurs
                return false;
            }
        }


    }

}