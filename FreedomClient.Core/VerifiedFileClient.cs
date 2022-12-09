using Newtonsoft.Json;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Store;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace FreedomClient.Core
{
    public class VerifiedFileClient
    {
        private readonly IHttpClientFactory _clientFactory;
        public Stopwatch DownloadTimer { get; private set; }

        public event EventHandler<FileDownloadStartedEventArgs>? FileDownloadStarted;
        public event EventHandler<FileDownloadCompletedEventArgs>? FileDownloadCompleted;
        public event EventHandler<FileVerifyStartedEventArgs>? FileVerifyStarted;
        public event EventHandler<FileVerifyCompletedEventArgs>? FileVerifyCompleted;
        public event EventHandler<ClientUpdateCompletedEventArgs>? ClientUpdateCompleted;
        public event EventHandler<ExceptionDuringDownloadEventArgs>? ExceptionDuringDownload;

        public VerifiedFileClient(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
            DownloadTimer = new Stopwatch();
        }

        public async Task DownloadFiles(string rootUri, string downloadPath, CancellationToken cancellationToken)
        {
            try
            {
                var httpClient = _clientFactory.CreateClient();
                httpClient.BaseAddress = new Uri(rootUri);
                var manifestResp = await httpClient.GetAsync("/manifest.json", cancellationToken);
                manifestResp.EnsureSuccessStatusCode();
                var manifestText = await manifestResp.Content.ReadAsStringAsync();
                var signatureResp = await httpClient.GetAsync("/signature", cancellationToken);
                var signatureBytes = await signatureResp.Content.ReadAsByteArrayAsync();
                var manifest = ValidateManifest(manifestText, signatureBytes);
                DownloadTimer.Start();
                // Download files in manifest
                foreach (var keypair in manifest)
                {
                    // Get file
                    var filePath = keypair.Key;
                    FileDownloadStarted?.Invoke(this, new FileDownloadStartedEventArgs(filePath, manifest));
                    var fileResp = await httpClient.GetAsync(filePath, cancellationToken);
                    var fileData = await fileResp.Content.ReadAsByteArrayAsync();

                    // Verify SHA1 Hash for file
                    var sha1Hash = SHA1.HashData(fileData);
                    var reportedHash = StringToByteArrayFastest(keypair.Value);
                    if (!ByteArrayCompare(sha1Hash, reportedHash))
                    {
                        throw new TamperedFileException(filePath);
                    }

                    // Write file to outpath
                    var outputPath = Path.Combine(downloadPath, filePath);
                    var outputDirs = filePath.Split("/").SkipLast(1);
                    var createdPath = downloadPath;
                    foreach (var outputDir in outputDirs)
                    {
                        var pathToCheck = Path.Combine(createdPath, outputDir);
                        if (!Directory.Exists(pathToCheck))
                        {
                            Directory.CreateDirectory(pathToCheck);
                        }
                        createdPath = pathToCheck;
                    }
                    await File.WriteAllBytesAsync(outputPath, fileData, cancellationToken);
                    FileDownloadCompleted?.Invoke(this, new FileDownloadCompletedEventArgs(filePath, manifest));
                }
                DownloadTimer.Stop();
                ClientUpdateCompleted?.Invoke(this, new ClientUpdateCompletedEventArgs());
            } catch(Exception ex) {
                ExceptionDuringDownload?.Invoke(this, new ExceptionDuringDownloadEventArgs(ex));
                throw;
            }
           
        }

        private Dictionary<string, string> ValidateManifest(string manifestText, byte[] signatureBytes)
        {
            // Verify PCKS7 signature against known public certificate
            try
            {
                var publicCert = new X509CertificateParser().ReadCertificate(Encoding.UTF8.GetBytes(Constants.PublicSigningCert));
                var storeParams = new X509CollectionStoreParameters(new List<X509Certificate>() { publicCert });
                var certStore = X509StoreFactory.Create("Certificate/Collection", storeParams);
                CmsSignedData signature = new CmsSignedData(new CmsProcessableByteArray(Encoding.UTF8.GetBytes(manifestText)), signatureBytes);

                var signers = signature.GetSignerInfos();
                var collection = signers.GetSigners();
                var iterator = collection.GetEnumerator();
                while (iterator.MoveNext())
                {
                    var signer = (SignerInformation)iterator.Current;
                    var certCollection = certStore.GetMatches(signer.SignerID);
                    var certIt = certCollection.GetEnumerator();
                    certIt.MoveNext();
                    signer.Verify((X509Certificate)certIt.Current);
                }
            } catch
            {
                throw new TamperedManifestException();
            }

            var manifest = JsonConvert.DeserializeObject<Dictionary<string, string>>(manifestText);
            if (manifest == null)
            {
                throw new InvalidManifestException();
            }
            var validHexStringRegex = new Regex("\\A\\b[0-9a-fA-F]+\\b\\Z");
            foreach(var val in manifest.Values)
            {
                if (val.Length != 40 || !validHexStringRegex.IsMatch(val))
                {
                    throw new InvalidManifestException();
                }
            }
            return manifest;
        }
        static bool ByteArrayCompare(ReadOnlySpan<byte> a1, ReadOnlySpan<byte> a2)
        {
            return a1.SequenceEqual(a2);
        }


        private static byte[] StringToByteArrayFastest(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        private static int GetHexVal(char hex)
        {
            int val = (int)hex;
            //For uppercase A-F letters:
            //return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            //return val - (val < 58 ? 48 : 87);
            //Or the two combined, but a bit slower:
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }
    }
}