namespace practice.proxy {
 public class ProxyReader
{
    private readonly string _filePath;

    public ProxyReader(string filePath)
    {
        _filePath = filePath;
    }

    public string ReadLastProxy()
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException("Proxy file not found.");
        }

        // Read all lines and get the last line
        var allProxies = File.ReadAllLines(_filePath);
        return allProxies.Length > 0 ? allProxies[^1].Trim() : string.Empty;
    }
}

}