using FreedomClient.Core;
using Newtonsoft.Json;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Store;
using System.Security.Cryptography;
using System.Text;

namespace FreedomManifestTool
{
    internal class ManifestGenerator
    {
        public static void GenerateSignedManifest(string workingDirectory, string certificatePath, string privateKeyPath, string privateKeyPassword, DownloadSourceConfiguration downloadConfig)
        {
            GenerateManifest(workingDirectory, downloadConfig);
            GenerateSignature(workingDirectory, certificatePath, privateKeyPath, privateKeyPassword);
        }

        // Generate manifest.json file by SHA1 hashing all files in the working directory
        private static void GenerateManifest(string workingDirectory, DownloadSourceConfiguration downloadConfig)
        {
            var manifest = new DownloadManifest();
            var hashAlgo = SHA1.Create();
            // Search through all files in the subdirectory
            foreach (var file in Directory.EnumerateFiles(workingDirectory, "*", SearchOption.AllDirectories))
            {
                // Replace windows \'s in paths with unix /'s for platform compatability
                var key = file.Substring(workingDirectory.Length + 1).Replace("\\", "/");
                // Calculate sha1 hash 
                var hashBytes = hashAlgo.ComputeHash(File.OpenRead(file));
                StringBuilder hashResult = new StringBuilder(hashBytes.Length * 2);
                // Save the sha1hash in lowercase for platform compatability
                for (int i = 0; i < hashBytes.Length; i++)
                    hashResult.Append(hashBytes[i].ToString("x2"));
                var hash = hashResult.ToString();

                var manifestEntry = new DownloadManifestEntry()
                {
                    Hash = hash,
                    FileSize = new FileInfo(file).Length,
                    Source = downloadConfig.FileSources.ContainsKey(key) 
                        ? downloadConfig.DownloadSources[downloadConfig.FileSources[key]] 
                        : new DirectHttpDownloadSource(Path.Join(downloadConfig.HttpDownloadSourceUri, key))
                };

                manifest.Add(key, manifestEntry);
            }
            var jsonConvertSettings = new JsonSerializerSettings
            {
                //TypeNameHandling = TypeNameHandling.All,

            };
            jsonConvertSettings.Converters.Add(new DownloadSourceJsonConverter());
            var json = JsonConvert.SerializeObject(manifest, jsonConvertSettings);
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
