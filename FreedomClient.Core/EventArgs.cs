using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreedomClient.Core
{
    public class FileDownloadStartedEventArgs : EventArgs
    {
        public string FileName { get; set; }
        public Dictionary<string, string> Manifest { get; set; }

        public FileDownloadStartedEventArgs(string fileName, Dictionary<string, string> manifest)
        {
            FileName = fileName;
            Manifest = manifest;
        }
    }

    public class FileDownloadCompletedEventArgs : EventArgs
    {
        public string FileName { get; set; }
        public Dictionary<string, string> Manifest { get; set; }

        public FileDownloadCompletedEventArgs(string fileName, Dictionary<string, string> manifest)
        {
            FileName = fileName;
            Manifest = manifest;
        }
    }

    public class FileVerifyStartedEventArgs : EventArgs
    {
        public string FileName { get; set; }
        public Dictionary<string, string> Manifest { get; set; }

        public FileVerifyStartedEventArgs(string fileName, Dictionary<string, string> manifest)
        {
            FileName = fileName;
            Manifest = manifest;
        }
    }

    public class FileVerifyCompletedEventArgs : EventArgs
    {
        public string FileName { get; set; }
        public Dictionary<string, string> Manifest { get; set; }

        public FileVerifyCompletedEventArgs(string fileName, Dictionary<string, string> manifest)
        {
            FileName = fileName;
            Manifest = manifest;
        }
    }

    public class ClientUpdateCompletedEventArgs: EventArgs
    {

    }

    public class ExceptionDuringDownloadEventArgs: EventArgs
    {
        public Exception Exception { get; set; }
        public ExceptionDuringDownloadEventArgs(Exception exception)
        {
            Exception = exception; 
        }
    }
}
