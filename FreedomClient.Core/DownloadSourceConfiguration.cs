namespace FreedomClient.Core
{
    public class DownloadSourceConfiguration
    {
        public string HttpDownloadSourceUri { get; set; } = Constants.CdnUrl;

        public Dictionary<string, DownloadSource> DownloadSources { get; set; } = new Dictionary<string, DownloadSource>();
    }
}
