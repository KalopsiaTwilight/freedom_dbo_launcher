using Newtonsoft.Json;

namespace FreedomClient.Core
{
    public class DownloadManifest: Dictionary<string, DownloadManifestEntry> { }

    public record DownloadManifestEntry
    {
        public string Hash { get; set; }
        public DownloadSource Source { get; set; }
        public long FileSize { get; set; }  
    }

    public abstract record DownloadSource
    {
        [JsonIgnore]
        public virtual string Id { get; }
    }

    public record DirectHttpDownloadSource : DownloadSource
    {
        [JsonIgnore]
        public override string Id { get => SourceUri; }   
        public string SourceUri { get; set; }

        public DirectHttpDownloadSource() { }
        public DirectHttpDownloadSource(string sourceUri)
        {
            SourceUri = sourceUri;
        }

        [JsonIgnore]
        public Uri Uri => new Uri(SourceUri);
    }

    public record GoogleDriveDownloadSource : DownloadSource
    {
        [JsonIgnore]
        public override string Id { get => GoogleDriveFileId; }
        public string GoogleDriveFileId { get; set; }
        public string FileName { get; set; }
    }
}
