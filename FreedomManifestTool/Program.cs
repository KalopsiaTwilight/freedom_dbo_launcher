// See https://aka.ms/new-console-template for more information
using FreedomManifestTool;

if (args.Length < 4)
{
    Console.WriteLine("This program requires 4 arguments: {fileDirectory} {certificatePath} {privateKeyPath} {privateKeyPass}");
}
Console.WriteLine($"Generating manifest for {args[0]}");
ManifestGenerator.GenerateSignedManifest(args[0], args[1], args[2], args[3]);
Console.WriteLine($"Manifest and signature generated.");