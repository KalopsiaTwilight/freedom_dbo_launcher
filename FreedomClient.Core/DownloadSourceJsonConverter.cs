using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FreedomClient.Core
{
    public class DownloadSourceJsonConverter : JsonConverter<DownloadSource>
    {
        public override DownloadSource? ReadJson(JsonReader reader, Type objectType, DownloadSource? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            if (jObject.Property(nameof(GoogleDriveDownloadSource.GoogleDriveFileId)) != null)
            {
                return new GoogleDriveDownloadSource()
                {
                    GoogleDriveFileId = jObject.Property(nameof(GoogleDriveDownloadSource.GoogleDriveFileId)).Value.Value<string>(),
                };
            }
            if (jObject.Property(nameof(GoogleDriveArchiveDownloadSource.GoogleDriveArchiveId)) != null)
            {
                return new GoogleDriveArchiveDownloadSource()
                {
                    GoogleDriveArchiveId = jObject.Property(nameof(GoogleDriveArchiveDownloadSource.GoogleDriveArchiveId)).Value.Value<string>(),
                    ArchiveSize = jObject.Property(nameof(GoogleDriveArchiveDownloadSource.ArchiveSize))?.Value?.Value<long>() ?? 0
                };
            }
            if (jObject.Property(nameof(DirectHttpDownloadSource.SourceUri)) != null)
            {
                return new DirectHttpDownloadSource(jObject.Property(nameof(DirectHttpDownloadSource.SourceUri)).Value.Value<string>());
            }
            throw new InvalidOperationException("Unable to parse download source.");
        }

        public override void WriteJson(JsonWriter writer, DownloadSource? value, JsonSerializer serializer)
        {
            if (value is GoogleDriveDownloadSource driveSource)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(nameof(GoogleDriveDownloadSource.GoogleDriveFileId));
                writer.WriteValue(driveSource.GoogleDriveFileId);
                writer.WriteEndObject();
                return;
            } 
            else if (value is GoogleDriveArchiveDownloadSource driveArchiveSource)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(nameof(GoogleDriveArchiveDownloadSource.GoogleDriveArchiveId));
                writer.WriteValue(driveArchiveSource.GoogleDriveArchiveId);
                //writer.WritePropertyName(nameof(GoogleDriveArchiveDownloadSource.ArchiveSize));
                //writer.WriteValue(driveArchiveSource.ArchiveSize);
                writer.WriteEndObject();
                return;
            }
            else if (value is DirectHttpDownloadSource directHttpDownloadSource)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(nameof(DirectHttpDownloadSource.SourceUri));
                writer.WriteValue(directHttpDownloadSource.SourceUri);
                writer.WriteEndObject();
                return;
            }
            throw new InvalidOperationException("Unable to serialize download source.");
        }
    }
}
