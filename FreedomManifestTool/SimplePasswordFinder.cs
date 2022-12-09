using Org.BouncyCastle.OpenSsl;

namespace FreedomManifestTool
{
    // Implementation of IPasswordFinder to provide a single password.
    internal class SimplePasswordFinder : IPasswordFinder
    {
        private readonly string _password;

        public SimplePasswordFinder(string password)
        {
            _password = password;
        }

        public char[] GetPassword()
        {
            return _password.ToCharArray();
        }
    }
}
