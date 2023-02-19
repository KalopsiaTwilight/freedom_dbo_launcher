// See https://aka.ms/new-console-template for more information

using FreedomClient.Core;
using GDriveStorageUploader;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Newtonsoft.Json;
using Org.BouncyCastle.Math.EC.Rfc7748;
using System.IO.Compression;
using System.Text.RegularExpressions;

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

// Prepare google drive client
GoogleCredential credential = GoogleCredential
    .FromStream(File.OpenRead(args[1]))
    .CreateScoped(DriveService.Scope.Drive);

// Create Drive API service.
var gdriveService = new DriveService(new BaseClientService.Initializer
{
    HttpClientInitializer = credential,
    ApplicationName = Constants.AppIdentifier
});



// Create download sources for files in directory 
Console.WriteLine($"Creating download sources for files in {args[0]}...");
var files = Directory.EnumerateFiles(args[0], "*", SearchOption.AllDirectories);
var fileInfos = files.Select(x => new FileInfo(x)).OrderByDescending(x => x.Length).ToList();
var downloadSources = new Dictionary<string, DownloadSource>();
var filesToArchive = new List<FileInfo>();

// Set up regexes for path ignores
List<Regex> ignoreRegexs = new();
foreach (var regex in downloadSourceConfig.IgnoredPaths)
{
    ignoreRegexs.Add(new Regex(regex.StartsWith("^") ? regex : ("^" + regex)));
}

foreach (var fileInfo in fileInfos)
{
    var fileKey = fileInfo.FullName.Substring(args[0].Length + 1).Replace("\\", "/");

    // Test if filepath should be skipped
    if (ignoreRegexs.Any(x => x.IsMatch(fileKey)))
        {
            continue;
        }

    FileInfo fileSource = fileInfo;
    if (downloadSourceConfig.StaticFiles.Keys.Contains(fileKey))
    {
        fileSource = new FileInfo(downloadSourceConfig.StaticFiles[fileKey]);
    }


    if (fileSource.Length < ArchiveTreshold)
    {
        Console.WriteLine($"Adding {fileSource.Name} to archive files because it is smaller than the threshold for individual files.");
        filesToArchive.Add(fileSource);
        continue;
    }

    if (downloadSourceConfig.DownloadSources.ContainsKey(fileKey))
    {
        Console.WriteLine($"Skipping {fileSource.Name} because it already exists in the config.");
        continue;
    }

    Console.WriteLine($"Uploading file {fileSource.Name}...");
    var fileId = UploadFileToDrive(fileSource, args[2]);
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

DeletePreviousArchiveFiles();

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
    if (downloadSourceConfig.DownloadSources.ContainsKey(keypair.Key))
    {
        downloadSourceConfig.DownloadSources[keypair.Key] = keypair.Value;
    } else
    {
        downloadSourceConfig.DownloadSources.Add(keypair.Key, keypair.Value);
    }
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
    var fileId = UploadFileToDrive(new FileInfo(archivePath), args[2]);
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

string? UploadFileToDrive(FileInfo fileInfo, string parentId)
{
    try
    {
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
                request = gdriveService.Files.Create(
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

void DeletePreviousArchiveFiles()
{
    var archives = downloadSourceConfig.DownloadSources
        .Where(x => x.Value is GoogleDriveArchiveDownloadSource)
        .Select(x => x.Value.Id)
        .Distinct()
        .ToList();
    foreach(var archive in archives)
    {
        try
        {
            var request = gdriveService.Files.Delete(archive);
            var resp = request.Execute();
        }
        catch(Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }
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