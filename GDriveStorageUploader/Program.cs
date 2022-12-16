// See https://aka.ms/new-console-template for more information

using FreedomClient.Core;
using GDriveStorageUploader;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Newtonsoft.Json;
using System.IO.Compression;

const long ArchiveTreshold = 100 * 1024 * 1024;
const long ArchiveSize = 100 * 1024 * 1024;

// Load arguments
if (args.Length < 3)
{
    Console.WriteLine("This program requires at least 3 arguments: {filedirectory} {googleCredentialsPath} {driveFolderId} {optional:downloadSourceConfig}");
    Environment.Exit(1);
}

// Parse DownloadSourceConfiguration
var jsonSettings = new JsonSerializerSettings();
jsonSettings.Converters.Add(new DownloadSourceJsonConverter());
jsonSettings.Formatting = Formatting.Indented;
DownloadSourceConfiguration? downloadSourceConfig = null;
if (args.Length >= 4)
{
    Console.WriteLine($"Parsing {nameof(DownloadSourceConfiguration)} in {args[3]}...");
    try
    {
        downloadSourceConfig = JsonConvert.DeserializeObject<DownloadSourceConfiguration>(File.ReadAllText(args[3]), jsonSettings);
    }
    catch
    {
        Console.Write($"Unable to parse {args[3]} as a {nameof(DownloadSourceConfiguration)}");
        Environment.Exit(1);
    }
}
downloadSourceConfig ??= new DownloadSourceConfiguration();


// Create download sources for files in directory 
Console.WriteLine($"Creating download sources for files in {args[0]}...");
var files = Directory.EnumerateFiles(args[0], "*", SearchOption.AllDirectories);
var fileInfos = files.Select(x => new FileInfo(x)).OrderByDescending(x => x.Length).ToList();
var downloadSources = new Dictionary<string, DownloadSource>();
var filesToArchive = new List<FileInfo>();
foreach (var fileInfo in fileInfos)
{
    var fileKey = fileInfo.FullName.Substring(args[0].Length + 1).Replace("\\", "/");
    if (downloadSourceConfig.DownloadSources.ContainsKey(fileKey))
    {
        Console.WriteLine($"Skipping {fileInfo.Name} because it already exists in the config.");
        continue;
    }

    if (fileInfo.Length < ArchiveTreshold)
    {
        Console.WriteLine($"Adding {fileInfo.Name} to archive files because it is smaller than the threshold for individual files.");
        filesToArchive.Add(fileInfo);
        continue;
    }

    Console.WriteLine($"Uploading file {fileInfo.Name}...");
    var fileId = UploadFileToDrive(fileInfo, args[1], args[2]);
    if (fileId == null)
    {
        Console.WriteLine("Unable to upload file. Halting...");
        break;
    }
    else
    {
        Console.WriteLine("File Uploaded!");
        downloadSources.Add(fileKey, new GoogleDriveDownloadSource()
        {
            GoogleDriveFileId = fileId
        });
    }
}

//Bundle small files together in archive
List<FileInfo> currentArchive = new List<FileInfo>();
var totalArchives = 0;
foreach (var file in filesToArchive)
{
    currentArchive.Add(file);
    if (currentArchive.Sum(x => x.Length) > ArchiveSize)
    {
        var archivePath = Path.Combine(Path.GetTempPath(), $"archive{totalArchives}.zip");
        Console.WriteLine($"Creating archive {totalArchives}...");
        CreateArchive(currentArchive, archivePath);
        currentArchive.Clear();
        totalArchives++;
    }
}
var finalArchivePath = Path.Combine(Path.GetTempPath(), $"archive{totalArchives}.zip");
CreateArchive(currentArchive, finalArchivePath);

//Add new datasources to config
foreach (var keypair in downloadSources)
{
    downloadSourceConfig.DownloadSources.Add(keypair.Key, keypair.Value);
}

// Write DownloadSourceConfig to json file
var outputPath = args.Length >= 4 ? args[3] : Path.Combine(args[0], "downloadsources.json");
var json = JsonConvert.SerializeObject(downloadSourceConfig, jsonSettings);
File.WriteAllText(outputPath, json);

Console.WriteLine($"Uploaded {downloadSources.Keys.Count} files!");
Console.WriteLine($"{nameof(DownloadSourceConfiguration)} written to {outputPath}.");
Console.WriteLine($"Press any key to exit...");
Console.ReadKey();

void CreateArchive(List<FileInfo> filesToArchive, string archivePath)
{
    var tempAchiveDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(tempAchiveDir);
    foreach (var fileInfo in filesToArchive)
    {
        var fileKey = fileInfo.FullName.Substring(args[0].Length + 1).Replace("\\", "/");
        EnsureDirectoriesExist(fileKey, tempAchiveDir);
        fileInfo.CopyTo(Path.Combine(tempAchiveDir, fileKey));
    }
    var tempFile = Path.GetTempFileName();
    
    ZipFile.CreateFromDirectory(tempAchiveDir, archivePath);
    Directory.Delete(tempAchiveDir, true);
    Console.WriteLine("Uploading archive...");
    var fileId = UploadFileToDrive(new FileInfo(archivePath), args[1], args[2]);
    if (fileId == null)
    {
        Console.WriteLine("Unable to upload archive. Halting...");
        return;
    }
    else
    {
        Console.WriteLine("Uploaded Archive!");
        var archiveSource = new GoogleDriveArchiveDownloadSource()
        {
            GoogleDriveArchiveId = fileId
        };
        foreach (var fileInfo in filesToArchive)
        {
            var fileKey = fileInfo.FullName.Substring(args[0].Length + 1).Replace("\\", "/");
            downloadSources.Add(fileKey, archiveSource);
        }
        File.Delete(archivePath);
    }
}

string? UploadFileToDrive(FileInfo fileInfo, string credentialPath, string parentId)
{
    try
    {
        GoogleCredential credential = GoogleCredential
            .FromStream(File.OpenRead(credentialPath))
            .CreateScoped(DriveService.Scope.Drive);

        // Create Drive API service.
        var service = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = Constants.AppIdentifier
        });

        var fileMetadata = new Google.Apis.Drive.v3.Data.File()
        {
            Name = fileInfo.Name,
            Parents = new List<string> { parentId }
        };
        FilesResource.CreateMediaUpload request;

        IUploadProgress? uploadStatus;
        using (var progress = new ProgressBar())
        {        // Create a new drive.
            using (var stream = new FileStream(fileInfo.FullName, FileMode.Open))
            {
                // Create a new file, with metadata and stream.
                request = service.Files.Create(
                    fileMetadata, stream, "application/octet-stream");
                request.Fields = "id";
                request.ProgressChanged += (x) =>
                {
                    var fraction = (double)x.BytesSent / fileInfo.Length;
                    progress.Report(fraction);
                };
                uploadStatus = request.Upload();
            }
        }

        if (uploadStatus.Status == UploadStatus.Completed)
        {
            var file = request.ResponseBody;
            return file.Id;
        }
        return null;
    }
    catch (Exception e)
    {
        // TODO(developer) - handle error appropriately
        if (e is AggregateException)
        {
            Console.WriteLine("Credential Not found");
        }
        else if (e is FileNotFoundException)
        {
            Console.WriteLine("File not found");
        }
        else
        {
            throw;
        }
    }
    return null;
}

void EnsureDirectoriesExist(string pathToTest, string installPath)
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