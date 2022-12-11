using Newtonsoft.Json;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Store;
using System;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace FreedomClient.Core
{
    public class VerifiedFileClient
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly HashAlgorithm _hashAlgo;
        public Stopwatch DownloadTimer { get; private set; }

        public event EventHandler<FileDownloadStartedEventArgs>? FileDownloadStarted;
        public event EventHandler<FileDownloadCompletedEventArgs>? FileDownloadCompleted;
        public event EventHandler<FileVerifyStartedEventArgs>? FileVerifyStarted;
        public event EventHandler<FileVerifyCompletedEventArgs>? FileVerifyCompleted;
        public event EventHandler<ExceptionDuringDownloadEventArgs>? ExceptionDuringDownload;

        public VerifiedFileClient(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
            _hashAlgo = SHA1.Create();
            DownloadTimer = new Stopwatch();
        }

        public async Task<bool> CheckForUpdates(Dictionary<string, string> currentManifest, CancellationToken cancellationToken)
        {
            var manifest = await GetManifest(cancellationToken);
            if (manifest.Count != currentManifest.Count)
            {
                return true;
            }
            foreach(var keypair in manifest)
            {
                if (!currentManifest.ContainsKey(keypair.Key) || manifest[keypair.Key] != keypair.Value)
                {
                    return true;
                } 
            }
            return false;
        }

        public async Task VerifyFiles(Dictionary<string, string> manifest, string installDirectory, CancellationToken cancellationToken)
        {
            Dictionary<string, string> toRedownload = new Dictionary<string, string>();
            // Verify integrity / version of all files in the manifest
            foreach (var keypair in manifest)
            {
                var filePath = keypair.Key;
                var outputPath = Path.Combine(installDirectory, filePath);
                if (!File.Exists(outputPath))
                {
                    toRedownload.Add(keypair.Key, keypair.Value);
                    continue;
                }
                try
                {
                    await VerifyFile(outputPath, keypair.Value, cancellationToken);
                } catch (TamperedFileException)
                {
                    toRedownload.Add(keypair.Key, keypair.Value);
                }
            }
            // Redownload invalid files
            try
            {
                await DownloadManifestFiles(toRedownload, installDirectory, cancellationToken);
            } catch(Exception ex)
            {
                ExceptionDuringDownload?.Invoke(this, new ExceptionDuringDownloadEventArgs(ex));
                throw;
            }
        }

        private async Task DownloadManifestFiles(Dictionary<string, string> manifest, string installPath, CancellationToken cancellationToken)
        {
            var httpClient = _clientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(Constants.CdnUrl);

            DownloadTimer.Start();
            // Download files in manifest
            foreach (var keypair in manifest)
            {
                var filePath = keypair.Key;
                var outputPath = Path.Combine(installPath, filePath);

                // Download file
                FileDownloadStarted?.Invoke(this, new FileDownloadStartedEventArgs(filePath));
                await DownloadFile(httpClient, keypair.Key, installPath, cancellationToken);

                // Verify SHA1 Hash for file
                await VerifyFile(outputPath, keypair.Value, cancellationToken);

                FileDownloadCompleted?.Invoke(this, new FileDownloadCompletedEventArgs(filePath));
            }
            DownloadTimer.Stop();
        }

        public async Task<Dictionary<string, string>> GetManifest(CancellationToken cancellationToken)
        {
            var httpClient = _clientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(Constants.CdnUrl);
            var manifestResp = await httpClient.GetAsync("/manifest.json", cancellationToken);
            manifestResp.EnsureSuccessStatusCode();
            var manifestText = await manifestResp.Content.ReadAsStringAsync();
            var signatureResp = await httpClient.GetAsync("/signature", cancellationToken);
            var signatureBytes = await signatureResp.Content.ReadAsByteArrayAsync();

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
            }
            catch
            {
                throw new TamperedManifestException();
            }

            // Deserialize manifest and check for valid contents
            var manifest = JsonConvert.DeserializeObject<Dictionary<string, string>>(manifestText);
            if (manifest == null)
            {
                throw new InvalidManifestException();
            }
            var validHexStringRegex = new Regex("\\A\\b[0-9a-fA-F]+\\b\\Z");
            foreach (var val in manifest.Values)
            {
                if (val.Length != 40 || !validHexStringRegex.IsMatch(val))
                {
                    throw new InvalidManifestException();
                }
            }
            return manifest;
        }

        private async Task DownloadFile(HttpClient httpClient, string uri, string downloadPath, CancellationToken cancellationToken)
        {
            try
            {
                using (HttpResponseMessage response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                using (Stream streamToReadFrom = await response.Content.ReadAsStreamAsync())
                {
                    // Copy response stream to output path
                    var outputDirs = uri.Split("/").SkipLast(1);
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
                    var outputPath = Path.Combine(downloadPath, uri);
                    using (Stream streamToWriteTo = File.Open(outputPath, FileMode.Create))
                    {
                        await streamToReadFrom.CopyToAsync(streamToWriteTo, cancellationToken);
                    }
                }
            }
            catch(Exception e)
            {
                ExceptionDuringDownload?.Invoke(this, new ExceptionDuringDownloadEventArgs(e));
                throw;
            }
        }

        private async Task VerifyFile(string filePath, string validateHash, CancellationToken cancellationToken)
        {
            FileVerifyStarted?.Invoke(this, new FileVerifyStartedEventArgs(filePath));
            byte[] hash;
            using (var filestream = File.OpenRead(filePath))
            {
                hash = await _hashAlgo.ComputeHashAsync(filestream, cancellationToken);
            }
            var reportedHash = StringToByteArrayFastest(validateHash);
            if (!ByteArrayCompare(hash, reportedHash))
            {
                File.Delete(filePath);
                throw new TamperedFileException(filePath);
            }
            FileVerifyCompleted?.Invoke(this, new FileVerifyCompletedEventArgs(filePath));
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