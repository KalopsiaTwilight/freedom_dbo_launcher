using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Store;
using System;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace FreedomClient.Core
{
    public class VerifiedFileClient
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly HashAlgorithm _hashAlgo;

        public event EventHandler<FileDownloadStartedEventArgs>? FileDownloadStarted;
        public event EventHandler<FileDownloadProgressEventArgs>? FileDownloadProgress;
        public event EventHandler<FileDownloadCompletedEventArgs>? FileDownloadCompleted;
        public event EventHandler<ExceptionDuringDownloadEventArgs>? ExceptionDuringDownload;
        public event EventHandler<ManifestDownloadStartedEventArgs>? ManifestDownloadStarted;
        public event EventHandler<ManifestDownloadCompletedEventArgs>? ManifestDownloadCompleted;
        public event EventHandler<FileVerifyStartedEventArgs>? FileVerifyStarted;
        public event EventHandler<FileVerifyCompletedEventArgs>? FileVerifyCompleted;
        public event EventHandler<FileVerifyProgressEventArgs>? FileVerifyProgress;
        public event EventHandler<ExceptionDuringVerifyEventArgs>? ExceptionDuringVerify;

        public Stopwatch DownloadTimer { get; private set; }


        public VerifiedFileClient(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
            _hashAlgo = SHA1.Create();
            DownloadTimer = new Stopwatch();
        }

        public async Task VerifyFiles(DownloadManifest manifest, string installDirectory, CancellationToken cancellationToken)
        {
            DownloadManifest toRedownload = new();
            // Verify integrity / version of all files in the manifest
            foreach (var entry in manifest)
            {
                var filePath = entry.Key;
                var outputPath = Path.Combine(installDirectory, filePath);
                if (!File.Exists(outputPath))
                {
                    toRedownload.Add(entry.Key, entry.Value);
                    continue;
                }
                try
                {
                    await VerifyFile(entry.Value, outputPath, cancellationToken);
                }
                catch (TamperedFileException)
                {
                    toRedownload.Add(entry.Key, entry.Value);
                }
            }
            // Redownload invalid files
            try
            {
                await DownloadManifestFiles(toRedownload, installDirectory, cancellationToken);
            }
            catch (Exception ex)
            {
                ExceptionDuringDownload?.Invoke(this, new ExceptionDuringDownloadEventArgs(ex));
                throw;
            }
        }

        public async Task<DownloadManifest> GetManifest(CancellationToken cancellationToken)
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
            var jsonSettings = new JsonSerializerSettings();
            jsonSettings.Converters.Add(new DownloadSourceJsonConverter());
            var manifest = JsonConvert.DeserializeObject<DownloadManifest>(manifestText, jsonSettings);
            if (manifest == null)
            {
                throw new InvalidManifestException();
            }
            var validHexStringRegex = new Regex("\\A\\b[0-9a-fA-F]+\\b\\Z");
            foreach (var val in manifest.Values)
            {
                if (val.Hash.Length != 40 || !validHexStringRegex.IsMatch(val.Hash))
                {
                    throw new InvalidManifestException();
                }
            }
            return manifest;
        }

        #region File Downloading
        private async Task DownloadManifestFiles(DownloadManifest manifest, string installPath, CancellationToken cancellationToken)
        {
            ManifestDownloadStarted?.Invoke(this, new ManifestDownloadStartedEventArgs(manifest));
            // Group files by id to process archives in one go
            var fileGroups = manifest.GroupBy(x => x.Value.Source.Id);
            foreach (var group in fileGroups)
            {
                foreach (var manifestEntry in group)
                {
                    var filePath = manifestEntry.Key;
                    EnsureDirectoriesExist(manifestEntry.Key, installPath);

                    // Download file
                    var outputPath = Path.Combine(installPath, filePath);
                    await DownloadFile(manifestEntry.Value, outputPath, cancellationToken);

                    // Verify SHA1 Hash for file
                    await VerifyFile(manifestEntry.Value, outputPath, cancellationToken);
                }
                // Clean up files for this source
                await CleanupDownloadSource(group.First().Value.Source, cancellationToken);
            }

            ManifestDownloadCompleted?.Invoke(this, new ManifestDownloadCompletedEventArgs(manifest));
        }

        private async Task DownloadFile(DownloadManifestEntry entry, string downloadPath, CancellationToken cancellationToken)
        {
            DownloadTimer.Reset();
            FileDownloadStarted?.Invoke(this, new FileDownloadStartedEventArgs(entry, downloadPath));
            DownloadTimer.Start();
            try
            {
                Action<long> progressCallback = (read) => FileDownloadProgress?.Invoke(this, new FileDownloadProgressEventArgs(entry, read, downloadPath));
                switch (entry.Source)
                {
                    case DirectHttpDownloadSource httpSource: await DownloadHttpFile(httpSource, downloadPath, cancellationToken, progressCallback); break;
                    case GoogleDriveDownloadSource driveSource: await DownloadGoogleDriveFile(driveSource, downloadPath, cancellationToken, progressCallback); break;
                    case GoogleDriveArchiveDownloadSource driveArchiveSource: await DownloadGoogleDriveArchiveFile(driveArchiveSource, downloadPath, cancellationToken, progressCallback); break;
                    default: throw new ArgumentException($"Unable to download source type: {entry.Source.GetType().Name}", nameof(entry));
                };
            }
            catch (Exception e)
            {
                ExceptionDuringDownload?.Invoke(this, new ExceptionDuringDownloadEventArgs(e));
                throw;
            }
            DownloadTimer.Stop();
            FileDownloadCompleted?.Invoke(this, new FileDownloadCompletedEventArgs(entry, downloadPath));
        }

        private async Task DownloadHttpFile(DirectHttpDownloadSource source, string outputPath, CancellationToken cancellationToken, Action<long>? reportCallback = null)
        {
            var bufferSize = 4096;
            var httpClient = _clientFactory.CreateClient();
            using (HttpResponseMessage response = await httpClient.GetAsync(source.Uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            using (Stream streamToReadFrom = await response.Content.ReadAsStreamAsync())
            {
                using (Stream streamToWriteTo = File.Open(outputPath, FileMode.Create))
                {
                    var buffer = new byte[bufferSize];
                    int bytesRead;
                    long totalRead = 0;
                    while ((bytesRead = await streamToReadFrom.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        await streamToWriteTo.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                        cancellationToken.ThrowIfCancellationRequested();
                        totalRead += bytesRead;
                        reportCallback?.Invoke(totalRead);
                    }
                }
            }
        }

        private async Task DownloadGoogleDriveFile(GoogleDriveDownloadSource source, string outputPath, CancellationToken cancellationToken, Action<long>? reportCallback = null)
        {
            var credential = GoogleCredential
                .FromStream(new EmbeddedFileProvider(Assembly.GetEntryAssembly()).GetFileInfo(Constants.GoogleCredentialsJsonPath).CreateReadStream())
                .CreateScoped(DriveService.Scope.Drive);
            var service = new DriveService(new BaseClientService.Initializer()
            {
                ApplicationName = Constants.AppIdentifier,
                HttpClientInitializer = credential
            });
            var request = service.Files.Get(source.GoogleDriveFileId);
            request.AcknowledgeAbuse = true;
            request.MediaDownloader.ProgressChanged +=
                progress => reportCallback?.Invoke(progress.BytesDownloaded);
            using (var fileStream = File.Create(outputPath))
            {
                var progress = await request.DownloadAsync(fileStream, cancellationToken);

                if (progress.BytesDownloaded == 0)
                {
                    throw new InvalidDataException();
                }
            }
        }

        private async Task DownloadGoogleDriveArchiveFile(GoogleDriveArchiveDownloadSource source, string outputPath, CancellationToken cancellationToken, Action<long>? reportCallback = null)
        {
            // Check if archive is already downloaded...
            var tempFilePath = Path.Combine(Path.GetTempPath(), source.GoogleDriveArchiveId);
            if (!File.Exists(tempFilePath))
            {
                var credential = GoogleCredential
                .FromStream(new EmbeddedFileProvider(Assembly.GetEntryAssembly()).GetFileInfo(Constants.GoogleCredentialsJsonPath).CreateReadStream())
                .CreateScoped(DriveService.Scope.Drive);
                var service = new DriveService(new BaseClientService.Initializer()
                {
                    ApplicationName = Constants.AppIdentifier,
                    HttpClientInitializer = credential
                });
                var request = service.Files.Get(source.GoogleDriveArchiveId);
                request.AcknowledgeAbuse = true;
                request.MediaDownloader.ProgressChanged +=
                    progress => reportCallback?.Invoke(progress.BytesDownloaded);
                using (var fileStream = File.Create(tempFilePath))
                {
                    var progress = await request.DownloadAsync(fileStream, cancellationToken);

                    if (progress.BytesDownloaded == 0)
                    {
                        throw new InvalidDataException();
                    }
                }
            }
            ExtractFileFromArchive(tempFilePath, outputPath);
           
        }

        private void ExtractFileFromArchive(string archivePath, string outputPath)
        {
            using (ZipArchive zip = ZipFile.Open(archivePath, ZipArchiveMode.Read))
            {
                foreach (var entry in zip.Entries)
                {
                    if (outputPath.ToLower().Contains(entry.FullName.ToLower()))
                    {
                        entry.ExtractToFile(outputPath, true);
                        break;
                    }
                }
            }
        }

        private Task CleanupDownloadSource(DownloadSource downloadSource, CancellationToken cancellationToken)
        {
            switch (downloadSource)
            {
                case GoogleDriveArchiveDownloadSource driveSource:
                    {
                        var tempFilePath = Path.Combine(Path.GetTempPath(), driveSource.GoogleDriveArchiveId);
                        File.Delete(tempFilePath);
                        return Task.CompletedTask;
                    }
                case GoogleDriveDownloadSource:
                case DirectHttpDownloadSource: return Task.CompletedTask;
                default: throw new ArgumentException($"Unable to clean up source type: {downloadSource.GetType().Name}", nameof(downloadSource));
            }
        }
        #endregion

        #region File Integrity

        private async Task VerifyFile(DownloadManifestEntry entry, string filePath, CancellationToken cancellationToken)
        {
            FileVerifyStarted?.Invoke(this, new FileVerifyStartedEventArgs(entry, filePath));
            try
            {
                byte[] hash;
                var buffersize = 1024 * 1024;

                using (var filestream = File.OpenRead(filePath))
                {
                    byte[] readAheadBuffer = new byte[buffersize];
                    byte[] buffer = new byte[buffersize];
                    int readAheadBytesRead, bytesRead;
                    long totalBytesRead = 0;
                    readAheadBytesRead = await filestream.ReadAsync(readAheadBuffer, 0, readAheadBuffer.Length, cancellationToken);
                    totalBytesRead += readAheadBytesRead;
                    while (readAheadBytesRead > 0)
                    {
                        bytesRead = readAheadBytesRead;
                        readAheadBuffer.CopyTo(buffer, 0);
                        readAheadBytesRead = await filestream.ReadAsync(readAheadBuffer, 0, readAheadBuffer.Length, cancellationToken);
                        totalBytesRead += readAheadBytesRead;

                        if (readAheadBytesRead == 0)
                        {
                            _hashAlgo.TransformFinalBlock(buffer, 0, bytesRead);
                        }
                        else
                        {
                            _hashAlgo.TransformBlock(buffer, 0, bytesRead, buffer, 0);
                        }
                        FileVerifyProgress?.Invoke(this, new FileVerifyProgressEventArgs(entry, totalBytesRead, filePath));
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    hash = _hashAlgo.Hash!;
                }
                var reportedHash = StringToByteArrayFastest(entry.Hash);
                if (!ByteArrayCompare(hash, reportedHash))
                {
                    File.Delete(filePath);
                    throw new TamperedFileException(filePath);
                }
                FileVerifyCompleted?.Invoke(this, new FileVerifyCompletedEventArgs(entry, filePath));
            }
            catch (Exception e)
            {
                ExceptionDuringVerify?.Invoke(this, new ExceptionDuringVerifyEventArgs(e));
                throw;
            }
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

        #endregion

        #region Utilities
        private void EnsureDirectoriesExist(string pathToTest, string installPath)
        {
            var outputDirs = pathToTest.Split("/").SkipLast(1);
            var createdPath = installPath;
            foreach (var outputDir in outputDirs)
            {
                var pathToCheck = Path.Combine(createdPath, outputDir);
                if (!Directory.Exists(pathToCheck))
                {
                    Directory.CreateDirectory(pathToCheck);
                }
                createdPath = pathToCheck;
            }
        }
        #endregion
    }
}