using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreedomClient.Core
{
    internal class AntiTamperingException: Exception
    {
        internal AntiTamperingException(string message) : base(message) { }
    }

    internal class InvalidManifestException : AntiTamperingException
    {
        internal InvalidManifestException() : base("Manifest was not in the expected format.") { }
    }


    internal class TamperedManifestException : AntiTamperingException
    {
        internal TamperedManifestException() : base("Signature does not match with file contents of manifest.") { }
    }


    internal class TamperedFileException: AntiTamperingException
    {
        internal TamperedFileException(string fileName) : base($"SHA1 hash for {fileName} does not match with the hash in the manifest.") { }
    }
}
