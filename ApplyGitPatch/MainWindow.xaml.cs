using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ApplyGitPatch;

public partial class MainWindow : Window
{
    private System.Windows.Controls.TextBox _patchInput = null!;
    private System.Windows.Controls.TextBox _logOutput = null!;
    private System.Windows.Controls.TextBlock _targetFolderText = null!;
    private string? _targetFolder;

    public MainWindow()
    {
        InitializeComponent();
        // Get references to XAML controls
        _patchInput = PatchInput;
        _logOutput = LogOutput;
        _targetFolderText = TargetFolderText;
    }

    private void AppendLog(string message)
    {
        _logOutput.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        _logOutput.ScrollToEnd();
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        _patchInput.Clear();
        _logOutput.Clear();
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select the folder to apply the patch to"
        };
        var result = dlg.ShowDialog();
        if (result == System.Windows.Forms.DialogResult.OK)
        {
            _targetFolder = dlg.SelectedPath;
            _targetFolderText.Text = _targetFolder;
            AppendLog($"Selected folder: {_targetFolder}");
        }
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_patchInput.Text))
        {
            AppendLog("No patch input provided.");
            return;
        }
        if (string.IsNullOrWhiteSpace(_targetFolder) || !System.IO.Directory.Exists(_targetFolder))
        {
            AppendLog("Please select a valid target folder.");
            return;
        }

        try
        {
            AppendLog("Parsing patch...");
            var parser = new PatchParser();
            var files = parser.Parse(_patchInput.Text);
            AppendLog($"Parsed {files.Count} file change(s).");

            AppendLog("Applying patch...");
            var applier = new PatchApplier();
            var result = await Task.Run(() => applier.ApplyToFolder(files, _targetFolder!));

            foreach (var msg in result.Messages)
                AppendLog(msg);

            if (result.Success)
                AppendLog("Patch applied successfully.");
            else
                AppendLog("Patch application completed with errors.");
        }
        catch (Exception ex)
        {
            AppendLog("Error: " + ex.Message);
        }
    }
}

public class PatchApplyResult
{
    public bool Success { get; set; }
    public List<string> Messages { get; } = new();
}

public enum PatchChangeType { Modify, Add, Delete, Rename }

public class PatchFile
{
    public string? OldPath { get; set; }
    public string? NewPath { get; set; }
    public PatchChangeType ChangeType { get; set; } = PatchChangeType.Modify;
    public List<PatchHunk> Hunks { get; } = new();
}

public class PatchHunk
{
    public int OldStart { get; set; }
    public int OldCount { get; set; }
    public int NewStart { get; set; }
    public int NewCount { get; set; }
    public List<PatchLine> Lines { get; } = new();
}

public class PatchLine
{
    public char Kind { get; set; } // ' ', '+', '-'
    public string Text { get; set; } = string.Empty; // without prefix, without trailing newline
}

public class PatchParser
{
    public List<PatchFile> Parse(string text)
    {
        text = text.Replace("\r\n", "\n");
        var lines = text.Split('\n');
        var files = new List<PatchFile>();

        PatchFile? current = null;
        PatchHunk? hunk = null;

        string? pendingOld = null;
        string? pendingNew = null;
        PatchChangeType? pendingType = null;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (line.StartsWith("diff --git "))
            {
                if (current != null)
                {
                    files.Add(current);
                }
                current = new PatchFile();
                hunk = null;
                pendingOld = null; pendingNew = null; pendingType = null;
                continue;
            }

            if (line.StartsWith("new file mode")) { pendingType = PatchChangeType.Add; continue; }
            if (line.StartsWith("deleted file mode")) { pendingType = PatchChangeType.Delete; continue; }
            if (line.StartsWith("rename from "))
            {
                pendingType = PatchChangeType.Rename;
                pendingOld = line.Substring("rename from ".Length).Trim();
                continue;
            }
            if (line.StartsWith("rename to "))
            {
                pendingType = PatchChangeType.Rename;
                pendingNew = line.Substring("rename to ".Length).Trim();
                continue;
            }

            if (line.StartsWith("--- "))
            {
                var path = line.Substring(4).Trim();
                if (path == "/dev/null") pendingOld = null; else pendingOld = TrimPrefix(path);
                continue;
            }
            if (line.StartsWith("+++ "))
            {
                var path = line.Substring(4).Trim();
                if (path == "/dev/null") pendingNew = null; else pendingNew = TrimPrefix(path);

                if (current != null)
                {
                    current.OldPath = pendingOld;
                    current.NewPath = pendingNew ?? pendingOld;
                    current.ChangeType = pendingType ?? InferType(pendingOld, pendingNew);
                }
                continue;
            }

            if (line.StartsWith("@@ "))
            {
                if (current == null)
                {
                    current = new PatchFile();
                }
                var header = line;
                var parts = header.Split(' ');
                var oldSpan = parts.First(p => p.StartsWith("-"));
                var newSpan = parts.First(p => p.StartsWith("+"));

                ParseSpan(oldSpan, out int os, out int oc);
                ParseSpan(newSpan, out int ns, out int nc);

                hunk = new PatchHunk { OldStart = os, OldCount = oc, NewStart = ns, NewCount = nc };
                current!.Hunks.Add(hunk);
                continue;
            }

            if (hunk != null && (line.StartsWith(" ") || line.StartsWith("+") || line.StartsWith("-")))
            {
                hunk.Lines.Add(new PatchLine { Kind = line[0], Text = line.Length > 1 ? line.Substring(1) : string.Empty });
                continue;
            }
        }

        if (current != null)
        {
            files.Add(current);
        }
        files = files.Where(f => f.OldPath != null || f.NewPath != null || f.Hunks.Count > 0 || f.ChangeType == PatchChangeType.Rename).ToList();
        return files;
    }

    private static string TrimPrefix(string path)
    {
        if (path.StartsWith("a/")) return path.Substring(2);
        if (path.StartsWith("b/")) return path.Substring(2);
        return path;
    }

    private static PatchChangeType InferType(string? oldPath, string? newPath)
    {
        if (oldPath == null && newPath != null) return PatchChangeType.Add;
        if (oldPath != null && newPath == null) return PatchChangeType.Delete;
        if (oldPath != null && newPath != null && !string.Equals(oldPath, newPath, StringComparison.Ordinal)) return PatchChangeType.Rename;
        return PatchChangeType.Modify;
    }

    private static void ParseSpan(string span, out int start, out int count)
    {
        var s = span.Substring(1);
        var parts = s.Split(',');
        start = int.Parse(parts[0]);
        count = parts.Length > 1 ? int.Parse(parts[1]) : 1;
    }
}

public class PatchApplier
{
    public PatchApplyResult ApplyToFolder(List<PatchFile> files, string folder)
    {
        var result = new PatchApplyResult { Success = true };
        foreach (var file in files)
        {
            try
            {
                switch (file.ChangeType)
                {
                    case PatchChangeType.Add:
                        ApplyAdd(file, folder, result);
                        break;
                    case PatchChangeType.Delete:
                        ApplyDelete(file, folder, result);
                        break;
                    case PatchChangeType.Rename:
                        ApplyRename(file, folder, result);
                        break;
                    case PatchChangeType.Modify:
                    default:
                        ApplyModify(file, folder, result);
                        break;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Messages.Add($"[{file.NewPath ?? file.OldPath}] Error: {ex.Message}");
            }
        }
        return result;
    }

    private static void EnsureDirectory(string path)
    {
        var dir = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }
    }

    private static void ApplyAdd(PatchFile file, string folder, PatchApplyResult result)
    {
        if (file.NewPath == null) throw new InvalidOperationException("Add missing new path");
        var target = System.IO.Path.Combine(folder, file.NewPath);
        EnsureDirectory(target);

        var sb = new StringBuilder();
        foreach (var h in file.Hunks)
        {
            foreach (var ln in h.Lines)
            {
                if (ln.Kind == ' ' || ln.Kind == '+')
                {
                    sb.AppendLine(ln.Text);
                }
            }
        }
        System.IO.File.WriteAllText(target, sb.ToString());
        result.Messages.Add($"[ADD] {file.NewPath}");
    }

    private static void ApplyDelete(PatchFile file, string folder, PatchApplyResult result)
    {
        if (file.OldPath == null) throw new InvalidOperationException("Delete missing old path");
        var target = System.IO.Path.Combine(folder, file.OldPath);
        if (System.IO.File.Exists(target))
        {
            System.IO.File.Delete(target);
            result.Messages.Add($"[DEL] {file.OldPath}");
        }
        else
        {
            result.Messages.Add($"[DEL] {file.OldPath} (already missing)");
        }
    }

    private static void ApplyRename(PatchFile file, string folder, PatchApplyResult result)
    {
        if (file.OldPath == null || file.NewPath == null) throw new InvalidOperationException("Rename missing paths");
        var oldFile = System.IO.Path.Combine(folder, file.OldPath);
        var newFile = System.IO.Path.Combine(folder, file.NewPath);
        EnsureDirectory(newFile);
        if (System.IO.File.Exists(oldFile))
        {
            if (System.IO.File.Exists(newFile)) System.IO.File.Delete(newFile);
            System.IO.File.Move(oldFile, newFile);
            result.Messages.Add($"[REN] {file.OldPath} -> {file.NewPath}");
        }
        else
        {
            result.Success = false;
            result.Messages.Add($"[REN] Source missing: {file.OldPath}");
        }
    }

    private static void ApplyModify(PatchFile file, string folder, PatchApplyResult result)
    {
        var targetRel = file.NewPath ?? file.OldPath ?? throw new InvalidOperationException("Modify missing path");
        var target = System.IO.Path.Combine(folder, targetRel);
        if (!System.IO.File.Exists(target))
        {
            EnsureDirectory(target);
            result.Messages.Add($"[WARN] {targetRel} not found; creating new file from patch contents.");
            var sbNew = new StringBuilder();
            foreach (var h in file.Hunks)
                foreach (var ln in h.Lines)
                    if (ln.Kind == ' ' || ln.Kind == '+') sbNew.AppendLine(ln.Text);
            System.IO.File.WriteAllText(target, sbNew.ToString());
            result.Success = false;
            result.Messages.Add($"[ADD*] {targetRel}");
            return;
        }

        var original = System.IO.File.ReadAllText(target);
        var newline = original.Contains("\r\n") ? "\r\n" : "\n";
        var oldLines = original.Replace("\r\n", "\n").Split('\n').ToList();
        if (oldLines.Count > 0 && oldLines[^1] == string.Empty) oldLines.RemoveAt(oldLines.Count - 1);

        int cursor = 0;
        var output = new List<string>();

        foreach (var h in file.Hunks)
        {
            int oldStartIdx = Math.Max(0, h.OldStart - 1);

            while (cursor < oldStartIdx && cursor < oldLines.Count)
            {
                output.Add(oldLines[cursor]);
                cursor++;
            }

            int tempCursor = cursor;
            foreach (var ln in h.Lines)
            {
                if (ln.Kind == ' ')
                {
                    if (tempCursor >= oldLines.Count || !NormalizedEquals(oldLines[tempCursor], ln.Text))
                    {
                        throw new InvalidOperationException($"Context mismatch near line {tempCursor + 1}");
                    }
                    output.Add(ln.Text);
                    tempCursor++;
                }
                else if (ln.Kind == '-')
                {
                    if (tempCursor >= oldLines.Count || !NormalizedEquals(oldLines[tempCursor], ln.Text))
                    {
                        throw new InvalidOperationException($"Delete mismatch near line {tempCursor + 1}");
                    }
                    tempCursor++;
                }
                else if (ln.Kind == '+')
                {
                    output.Add(ln.Text);
                }
            }
            cursor = tempCursor;
        }

        while (cursor < oldLines.Count)
        {
            output.Add(oldLines[cursor]);
            cursor++;
        }

        var newText = string.Join(newline, output) + newline;
        System.IO.File.WriteAllText(target, newText);
        result.Messages.Add($"[MOD] {targetRel}");
    }
}

private static bool NormalizedEquals(string line1, string line2)
{
    if (line1 == line2) return true;
    
    // Normalize whitespace: convert tabs to spaces and trim trailing whitespace
    var normalized1 = NormalizeWhitespace(line1);
    var normalized2 = NormalizeWhitespace(line2);
    
    return normalized1 == normalized2;
}

private static string NormalizeWhitespace(string line)
{
    // Convert tabs to 4 spaces and trim trailing whitespace
    return line.Replace("\t", "    ").TrimEnd();
}
