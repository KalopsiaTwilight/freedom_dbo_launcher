using Newtonsoft.Json;

namespace FreedomClient.Core
{
    public class DownloadManifest: Dictionary<string, DownloadManifestEntry>, IEquatable<DownloadManifest>
    {
        public bool Equals(DownloadManifest? other)
        {
            if (other is null)
            {
                return false;
            }

            // Optimization for a common success case.
            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            // If run-time types are not exactly the same, return false.
            if (GetType() != other.GetType())
            {
                return false;
            }

            if (Count != other.Count)
            {
                return false;
            }
            foreach (var keypair in other)
            {
                if (!ContainsKey(keypair.Key) || this[keypair.Key] != keypair.Value)
                {
                    return false;
                }
            }
            return true;
        }

        public DownloadManifest CreatePatchManifestFrom(DownloadManifest other)
        {
            DownloadManifest result = new();
            foreach (var keypair in this)
            {
                if (!other.ContainsKey(keypair.Key) || other[keypair.Key] != keypair.Value)
                {
                    result.Add(keypair.Key, keypair.Value);
                }
            }
            return result;
        }
    }

    public class DownloadManifestEntry: IEquatable<DownloadManifestEntry>
    {
        public string Hash { get; set; }
        public DownloadSource Source { get; set; }
        public long FileSize { get; set; }

        public override bool Equals(object? obj)
        {
            return Equals(obj as DownloadManifestEntry);
        }

        public bool Equals(DownloadManifestEntry? other)
        {
            if (other is null)
            {
                return false;
            }

            // Optimization for a common success case.
            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            // If run-time types are not exactly the same, return false.
            if (GetType() != other.GetType())
            {
                return false;
            }
            return Hash == other.Hash &&
                   FileSize == other.FileSize;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Hash, Source, FileSize);
        }

        public static bool operator ==(DownloadManifestEntry lhs, DownloadManifestEntry rhs)
        {
            if (lhs is null)
            {
                if (rhs is null)
                {
                    return true;
                }
                // Only the left side is null.
                return false;
            }
            // Equals handles case of null on right side.
            return lhs.Equals(rhs);
        }

        public static bool operator !=(DownloadManifestEntry lhs, DownloadManifestEntry rhs) => !(lhs == rhs);

    }

    public abstract class DownloadSource: IEquatable<DownloadSource>
    {
        [JsonIgnore]
        public virtual string Id { get; }

        public override bool Equals(object? obj)
        {
            return Equals(obj as DownloadSource);
        }

        public bool Equals(DownloadSource? other)
        {
            if (other is null)
            {
                return false;
            }

            // Optimization for a common success case.
            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            // If run-time types are not exactly the same, return false.
            if (GetType() != other.GetType())
            {
                return false;
            }

            // Return true if the fields match.
            // Note that the base class is not invoked because it is
            // System.Object, which defines Equals as reference equality.
            return Id == other.Id;

        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        public static bool operator ==(DownloadSource lhs, DownloadSource rhs)
        {
            if (lhs is null)
            {
                if (rhs is null)
                {
                    return true;
                }
                // Only the left side is null.
                return false;
            }
            // Equals handles case of null on right side.
            return lhs.Equals(rhs);
        }

        public static bool operator !=(DownloadSource lhs, DownloadSource rhs) => !(lhs == rhs);
    }

    public class DirectHttpDownloadSource : DownloadSource
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

    public class GoogleDriveDownloadSource : DownloadSource
    {
        [JsonIgnore]
        public override string Id { get => GoogleDriveFileId; }
        public string GoogleDriveFileId { get; set; }
    }

    public class GoogleDriveArchiveDownloadSource: DownloadSource
    {
        [JsonIgnore]
        public override string Id { get => GoogleDriveArchiveId; }
        public string GoogleDriveArchiveId { get; set; }

        public long ArchiveSize { get; set; }
    }
}
