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

        public FileDownloadStartedEventArgs(string fileName)
        {
            FileName = fileName;
        }
    }

    public class FileDownloadCompletedEventArgs : EventArgs
    {
        public string FileName { get; set; }

        public FileDownloadCompletedEventArgs(string fileName)
        {
            FileName = fileName;
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
}
