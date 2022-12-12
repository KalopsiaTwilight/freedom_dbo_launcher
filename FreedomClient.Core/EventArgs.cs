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

    public class FileVerifyStartedEventArgs : EventArgs
    {
        public string FileName { get; set; }

        public FileVerifyStartedEventArgs(string fileName)
        {
            FileName = fileName;
        }
    }

    public class FileVerifyCompletedEventArgs : EventArgs
    {
        public string FileName { get; set; }

        public FileVerifyCompletedEventArgs(string fileName)
        {
            FileName = fileName;
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

        public FileDownloadProgressEventArgs(DownloadManifestEntry entry, long totalBytesRead)
        {
            Entry = entry;
            TotalBytesRead = totalBytesRead;
        }
    }
}
