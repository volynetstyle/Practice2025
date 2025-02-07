
namespace practice.entities {    
    public class ProgressData
    {
        public List<string> PendingUrls { get; set; } = [];

        public List<string> CompletedUrls { get; set; } = [];

        public List<string> FailedUrls { get; set; } = [];
    }
}