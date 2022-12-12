// See https://aka.ms/new-console-template for more information
using FreedomClient.Core;
using FreedomManifestTool;
using Newtonsoft.Json;

if (args.Length < 4)
{
    Console.WriteLine("This program requires at least 4 arguments: {fileDirectory} {certificatePath} {privateKeyPath} {privateKeyPass} {optional: downloadSourceConfigPath}");
    Environment.Exit(1);
}
Console.WriteLine($"Generating manifest for {args[0]}");
DownloadSourceConfiguration? downloadConfig = null;
if (args.Length >= 5)
{
    try
    {
        var jsonConvertSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            
        };
        jsonConvertSettings.Converters.Add(new DownloadSourceJsonConverter());
        downloadConfig = JsonConvert.DeserializeObject<DownloadSourceConfiguration>(File.ReadAllText(args[4]), jsonConvertSettings);
    } catch
    {
        Console.WriteLine($"Unable to parse DataSourceConfiguration in file {args[4]}");
        Environment.Exit(1);
    }
}
downloadConfig ??= new DownloadSourceConfiguration();
ManifestGenerator.GenerateSignedManifest(args[0], args[1], args[2], args[3], downloadConfig);
Console.WriteLine($"Manifest and signature generated.");