using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreedomClient.Core
{
    public class FileDownloadStartedEventArgs : EventArgs
    {
        public DownloadManifestEntry Entry { get; set; }
        public string FilePath { get; set; }

        public FileDownloadStartedEventArgs(DownloadManifestEntry entry, string filename)
        {
            Entry = entry;
            FilePath = filename;
        }
    }

    public class FileDownloadCompletedEventArgs : EventArgs
    {
        public DownloadManifestEntry Entry { get; set; }
        public string FilePath { get; set; }

        public FileDownloadCompletedEventArgs(DownloadManifestEntry entry, string filename)
        {
            Entry = entry;
            FilePath = filename;
        }
    }

    public class ExceptionDuringDownloadEventArgs: EventArgs
    {
        public Exception Exception { get; set; }
        public ExceptionDuringDownloadEventArgs(Exception exception)
        {
            Exception = exception; 
        }
    }

    public class FileDownloadProgressEventArgs: EventArgs
    {
        public long TotalBytesRead { get; set; }
        public DownloadManifestEntry Entry { get; set; }
        public string FilePath { get; set; }

        public FileDownloadProgressEventArgs(DownloadManifestEntry entry, long totalBytesRead, string filePath)
        {
            Entry = entry;
            TotalBytesRead = totalBytesRead;
            FilePath = filePath;
        }
    }

    public class ManifestDownloadStartedEventArgs: EventArgs
    {
        public DownloadManifest ToDownload { get; set; }

        public ManifestDownloadStartedEventArgs(DownloadManifest toDownload)
        {
            ToDownload = toDownload;
        }
    }
    public class ManifestDownloadCompletedEventArgs: EventArgs
    {
        public DownloadManifest Downloaded { get; set; }

        public ManifestDownloadCompletedEventArgs(DownloadManifest toDownload)
        {
            Downloaded = toDownload;
        }
    }

    public class FileVerifyStartedEventArgs : EventArgs
    {
        public DownloadManifestEntry Entry { get; set; }
        public string FilePath { get; set; }

        public FileVerifyStartedEventArgs(DownloadManifestEntry entry, string filePath)
        {
            Entry = entry;
            FilePath = filePath;
        }
    }

    public class FileVerifyCompletedEventArgs : EventArgs
    {
        public DownloadManifestEntry Entry { get; set; }
        public string FilePath { get; set; }

        public FileVerifyCompletedEventArgs(DownloadManifestEntry entry, string filePath)
        {
            Entry = entry;
            FilePath = filePath;
        }
    }

    public class FileVerifyProgressEventArgs: EventArgs
    {
        public long TotalBytesRead { get; set; }
        public DownloadManifestEntry Entry { get; set; }
        public string FilePath { get; set; }
        public FileVerifyProgressEventArgs(DownloadManifestEntry entry, long totalBytesRead, string filePath)
        {
            Entry = entry;
            TotalBytesRead = totalBytesRead;
            FilePath = filePath;
        }
    }

    public class ExceptionDuringVerifyEventArgs: EventArgs
    {
        public Exception Exception { get; set; }
        public ExceptionDuringVerifyEventArgs(Exception exception)
        {
            Exception = exception;
        }
    }

    public class ManifestVerificationStartedEventArgs: EventArgs
    {
        public DownloadManifest ToVerify { get; set; }

        public ManifestVerificationStartedEventArgs(DownloadManifest toVerify)
        {
            ToVerify = toVerify;
        }
    }
}
