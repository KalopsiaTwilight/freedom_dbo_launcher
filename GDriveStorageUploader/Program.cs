// See https://aka.ms/new-console-template for more information

using FreedomClient.Core;
using GDriveStorageUploader;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Newtonsoft.Json;

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
    catch(Exception e)
    {
        Console.Write($"Unable to parse {args[3]} as a {nameof(DownloadSourceConfiguration)}");
        Environment.Exit(1);
    }
}
downloadSourceConfig ??= new DownloadSourceConfiguration();

// Create download sources for files in directory 
Console.WriteLine($"Creating download sources for file in {args[0]}...");
var files = Directory.EnumerateFiles(args[0], "*", SearchOption.AllDirectories);
var fileInfos = files.Select(x => new FileInfo(x)).OrderByDescending(x => x.Length).ToList();
var downloadSources = new Dictionary<string, DownloadSource>();
foreach (var fileInfo in fileInfos)
{
    var fileKey = fileInfo.FullName.Substring(args[0].Length + 1).Replace("\\", "/");
    if (downloadSourceConfig.DownloadSources.ContainsKey(fileKey))
    {
        Console.WriteLine($"Skipping {fileInfo.Name} because it already exists in the config.");
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

//Add new datasources to config
foreach(var keypair in downloadSources)
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


static string BytesToString(long byteCount)
{
    string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
    if (byteCount == 0)
        return "0" + suf[0];
    long bytes = Math.Abs(byteCount);
    int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
    double num = Math.Round(bytes / Math.Pow(1024, place), 1);
    return (Math.Sign(byteCount) * num).ToString() + suf[place];
}