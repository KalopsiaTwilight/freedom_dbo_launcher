namespace FreedomClient.Core
{
    public class DownloadSourceConfiguration
    {
        public string HttpDownloadSourceUri { get; set; } = Constants.CdnUrl;

        public Dictionary<string, string> StaticFiles { get; set; } = new();

        public List<string> IgnoredPaths { get; set; } = new();

        public Dictionary<string, DownloadSource> DownloadSources { get; set; } = new();
    }
}
