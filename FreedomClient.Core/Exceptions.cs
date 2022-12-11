using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreedomClient.Core
{
    public class AntiTamperingException: Exception
    {
        public AntiTamperingException(string message) : base(message) { }
    }

    public class InvalidManifestException : AntiTamperingException
    {
        public InvalidManifestException() : base("Manifest was not in the expected format.") { }
    }


    public class TamperedManifestException : AntiTamperingException
    {
        public TamperedManifestException() : base("Signature does not match with file contents of manifest.") { }
    }


    public class TamperedFileException: AntiTamperingException
    {
        public TamperedFileException(string fileName) : base($"SHA1 hash for {fileName} does not match with the hash in the manifest.") { }
    }
}
