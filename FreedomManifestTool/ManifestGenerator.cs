using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Store;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FreedomManifestTool
{
    internal class ManifestGenerator
    {
        public static void GenerateSignedManifest(string workingDirectory, string certificatePath, string privateKeyPath, string privateKeyPassword)
        {
            GenerateManifest(workingDirectory);
            GenerateSignature(workingDirectory, certificatePath, privateKeyPath, privateKeyPassword);
        }

        // Generate manifest.json file by SHA1 hashing all files in the working directory
        private static void GenerateManifest(string workingDirectory)
        {
            var manifest = new Dictionary<string, string>();
            // Search through subdirectories as well for localisation texts
            foreach (var file in Directory.EnumerateFiles(workingDirectory, "*", SearchOption.AllDirectories))
            {
                // Replace windows \'s in paths with unix /'s for platform compatability
                var key = file.Substring(workingDirectory.Length + 1).Replace("\\", "/");
                // Calculate sha1 hash 
                var hashBytes = SHA1.HashData(File.ReadAllBytes(file));
                StringBuilder hashResult = new StringBuilder(hashBytes.Length * 2);
                // Save the sha1hash in lowercase for platform compatability
                for (int i = 0; i < hashBytes.Length; i++)
                    hashResult.Append(hashBytes[i].ToString("x2"));
                var hash = hashResult.ToString();
                manifest.Add(key, hash);
            }
            var json = JsonSerializer.Serialize(manifest);
            File.WriteAllText(Path.Combine(workingDirectory, "manifest.json"), json);
        }

        // Generate a PCKS7 detached signature for the manifest.json
        private static void GenerateSignature(string workingDirectory, string certificatePath, string privateKeyPath, string privateKeyPassword)
        {
            var certParser = new X509CertificateParser();

            // Read Certificates
            var cert = certParser.ReadCertificate(File.ReadAllBytes(certificatePath));

            // Read Private Key
            RsaPrivateCrtKeyParameters privateKey;
            var reader = new StringReader(File.ReadAllText(privateKeyPath));
            var pemReader = new PemReader(reader, new SimplePasswordFinder(privateKeyPassword));
            object? pemObject = null;
            while (reader.Peek() != -1)
            {
                pemObject = pemReader.ReadObject();
                if (pemObject != null)
                {
                    break;
                }
            }
            if (pemObject == null)
            {
                throw new InvalidOperationException($"Could not read PEM file {privateKeyPath}.");
            }
            privateKey = ((RsaPrivateCrtKeyParameters)pemObject);

            // Calculate PCKS7 detached signature for manifest.json through BouncyCastle
            var gen = new CmsSignedDataGenerator();
            var storeParams = new X509CollectionStoreParameters(new List<X509Certificate>() { cert });
            var certStore = X509StoreFactory.Create("Certificate/Collection", storeParams);
            gen.AddCertificates(certStore);
            gen.AddSigner(privateKey, cert, CmsSignedGenerator.EncryptionRsa, CmsSignedGenerator.DigestSha256);

            var manifestPath = Path.Combine(workingDirectory, "manifest.json");
            var message = new CmsProcessableByteArray(File.ReadAllBytes(manifestPath));
            var signedContent = gen.Generate(message, false);

            // Write out PCKS7 detached signature to signature file.
            File.WriteAllBytes(Path.Combine(workingDirectory, "signature"), signedContent.ContentInfo.GetDerEncoded());
        }
    }
}
