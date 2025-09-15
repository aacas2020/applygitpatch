using System;
using System.IO;
using ApplyGitPatch;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 3 || !string.Equals(args[0], "--apply", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Usage: ApplyGitPatch.Cli --apply <patchfile> --to <targetfolder>");
            return 2;
        }

        string? patchFile = null;
        string? targetFolder = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--apply" && i + 1 < args.Length) { patchFile = args[++i]; continue; }
            if (args[i] == "--to" && i + 1 < args.Length) { targetFolder = args[++i]; continue; }
        }

        if (string.IsNullOrWhiteSpace(patchFile) || string.IsNullOrWhiteSpace(targetFolder))
        {
            Console.WriteLine("Missing arguments. Usage: --apply <patchfile> --to <targetfolder>");
            return 2;
        }

        patchFile = Path.GetFullPath(patchFile);
        targetFolder = Path.GetFullPath(targetFolder);

        if (!File.Exists(patchFile)) { Console.WriteLine($"Patch file not found: {patchFile}"); return 3; }
        if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

        var patchText = File.ReadAllText(patchFile);
        var parser = new PatchParser();
        var files = parser.Parse(patchText);
        Console.WriteLine($"Parsed {files.Count} file change(s).");

        var applier = new PatchApplier();
        var result = applier.ApplyToFolder(files, targetFolder);
        foreach (var msg in result.Messages)
            Console.WriteLine(msg);

        Console.WriteLine(result.Success ? "Success" : "Completed with errors");
        return result.Success ? 0 : 1;
    }
}
