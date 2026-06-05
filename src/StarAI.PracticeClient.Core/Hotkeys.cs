using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace StarAI.PracticeClient.Core;

public sealed class HotkeyEntry
{
    public int StringId { get; set; }
    public string CommandId { get; set; } = string.Empty;
    public string Hotkey { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DefaultText { get; set; } = string.Empty;
    public string CurrentText { get; set; } = string.Empty;
}

public sealed class HotkeyApplyResult
{
    public bool AppliedMpq { get; init; }
    public string WorkingCsvPath { get; init; } = string.Empty;
    public string? PatchedTblPath { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed partial class HotkeyCsvStore
{
    public const string RelativeWorkingCsvPath = @"bwapi-data\read\sc_hotkeys.csv";

    public IReadOnlyList<HotkeyEntry> Load(string csvPath, string? messagesPath = null)
    {
        if (!File.Exists(csvPath))
        {
            return [];
        }

        var descriptions = LoadDescriptions(messagesPath);
        return File.ReadAllLines(csvPath, Encoding.UTF8)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => ParseLine(line, descriptions))
            .Where(entry => entry is not null)
            .Cast<HotkeyEntry>()
            .OrderBy(entry => entry.CommandId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string SaveWorkingCopy(string runtimeRoot, IReadOnlyList<HotkeyEntry> entries)
    {
        var path = Path.Combine(runtimeRoot, RelativeWorkingCsvPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllLines(path, entries.Select(Serialize), Encoding.UTF8);
        return path;
    }

    public string ImportFromSchnail(PracticePaths paths, string runtimeRoot)
    {
        var source = Path.Combine(paths.SchnailRoot, "res", "sc_hotkeys.csv");
        if (!File.Exists(source))
        {
            throw new FileNotFoundException("SCHNAIL hotkey CSV was not found.", source);
        }

        var target = Path.Combine(runtimeRoot, RelativeWorkingCsvPath);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(source, target, overwrite: true);
        return target;
    }

    public static HotkeyEntry? ParseLine(string line, IReadOnlyDictionary<string, string>? descriptions = null)
    {
        var parts = line.Split(',');
        if (parts.Length < 3 || !int.TryParse(parts[0].Trim(), out var stringId))
        {
            return null;
        }

        var commandId = parts[2].Trim();
        var currentText = parts.Length >= 4 ? parts[3].Trim() : parts[1].Trim();
        var hotkey = ExtractHotkey(currentText);
        string? description = null;
        descriptions?.TryGetValue($"hotkey_desc_{commandId}", out description);

        return new HotkeyEntry
        {
            StringId = stringId,
            CommandId = commandId,
            Hotkey = hotkey,
            Description = string.IsNullOrWhiteSpace(description)
                ? commandId.Replace('_', ' ')
                : description,
            DefaultText = parts[1].Trim(),
            CurrentText = currentText
        };
    }

    public static string Serialize(HotkeyEntry entry)
    {
        var rendered = RenderGameText(entry);
        return $"{entry.StringId},{entry.DefaultText},{entry.CommandId},{rendered}";
    }

    public static string RenderGameText(HotkeyEntry entry)
    {
        var key = NormalizeHotkey(entry.Hotkey);
        var upper = key.ToUpperInvariant();
        var description = SanitizeDescription(entry.Description);
        return $"{key}<1>{description}(<3>{upper}<1>)<0>";
    }

    private static string ExtractHotkey(string text)
    {
        var match = LeadingHotkeyRegex().Match(text);
        return match.Success ? match.Groups["key"].Value.ToLowerInvariant() : string.Empty;
    }

    private static string NormalizeHotkey(string value)
    {
        var trimmed = value.Trim();
        return string.IsNullOrEmpty(trimmed) ? " " : trimmed[..1].ToLowerInvariant();
    }

    private static string SanitizeDescription(string value)
    {
        return value.Replace(",", "", StringComparison.Ordinal).Trim();
    }

    private static Dictionary<string, string> LoadDescriptions(string? messagesPath)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(messagesPath) || !File.Exists(messagesPath))
        {
            return result;
        }

        foreach (var line in File.ReadAllLines(messagesPath, Encoding.UTF8))
        {
            var index = line.IndexOf('=');
            if (index <= 0)
            {
                continue;
            }

            result[line[..index].Trim()] = line[(index + 1)..].Trim();
        }

        return result;
    }

    [GeneratedRegex("^(?<key>.)<")]
    private static partial Regex LeadingHotkeyRegex();
}

public static class HotkeyStatTextPatcher
{
    public static string Patch(string statText, IReadOnlyList<HotkeyEntry> entries)
    {
        var lines = statText.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').ToList();
        foreach (var entry in entries)
        {
            var index = entry.StringId - 1;
            if (index < 0 || index >= lines.Count)
            {
                continue;
            }

            lines[index] = HotkeyCsvStore.RenderGameText(entry);
        }

        return string.Join(Environment.NewLine, lines).TrimEnd() + Environment.NewLine;
    }
}

public sealed class HotkeyPatchApplier
{
    private const string MpqTargetName = @"rez\stat_txt.tbl";
    private readonly HotkeyCsvStore _store = new();

    public HotkeyApplyResult SaveAndApply(
        PracticePaths paths,
        string runtimeRoot,
        IReadOnlyList<HotkeyEntry> entries,
        bool applyMpq)
    {
        var safety = RuntimeWritePolicy.CheckMutableRuntimeTarget(paths, runtimeRoot);
        if (!safety.Allowed)
        {
            throw new InvalidOperationException(safety.Message);
        }

        var csvPath = _store.SaveWorkingCopy(runtimeRoot, entries);
        if (!applyMpq)
        {
            return new HotkeyApplyResult
            {
                AppliedMpq = false,
                WorkingCsvPath = csvPath,
                Message = "작업용 핫키 CSV를 저장했습니다."
            };
        }

        var patchedTbl = BuildPatchedTbl(paths, entries);
        var patchRtMpq = Path.Combine(runtimeRoot, "patch_rt.mpq");
        InsertTblIntoRuntimeMpq(paths, patchRtMpq, patchedTbl);

        return new HotkeyApplyResult
        {
            AppliedMpq = true,
            WorkingCsvPath = csvPath,
            PatchedTblPath = patchedTbl,
            Message = "작업용 CSV 저장과 런타임 patch_rt.mpq 핫키 반영을 완료했습니다."
        };
    }

    public static string BuildPatchedTbl(PracticePaths paths, IReadOnlyList<HotkeyEntry> entries)
    {
        var sourceStatText = Path.Combine(paths.SchnailRoot, "res", "hotkey_data", "stat_txt.txt");
        var compiler = Path.Combine(paths.SchnailRoot, "res", "hotkey_data", "sctblcmp.exe");
        if (!File.Exists(sourceStatText))
        {
            throw new FileNotFoundException("SCHNAIL stat_txt.txt was not found.", sourceStatText);
        }

        if (!File.Exists(compiler))
        {
            throw new FileNotFoundException("SCHNAIL TBL compiler was not found.", compiler);
        }

        var workDir = Path.Combine(Path.GetTempPath(), "StarAI.PracticeClient", "hotkeys", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        var patchedTxt = Path.Combine(workDir, "stat_txt.txt");
        var patchedTbl = Path.Combine(workDir, "stat_txt.tbl");
        var patchedText = HotkeyStatTextPatcher.Patch(File.ReadAllText(sourceStatText, Encoding.UTF8), entries);
        File.WriteAllText(patchedTxt, patchedText, Encoding.UTF8);

        RunProcess(compiler, $"/i \"{patchedTxt}\" \"{patchedTbl}\"", workDir);
        if (!File.Exists(patchedTbl))
        {
            throw new InvalidOperationException("TBL compiler did not create stat_txt.tbl.");
        }

        return patchedTbl;
    }

    public static void InsertTblIntoRuntimeMpq(PracticePaths paths, string patchRtMpqPath, string statTxtTblPath)
    {
        var safety = RuntimeWritePolicy.CheckMutableRuntimeTarget(paths, patchRtMpqPath);
        if (!safety.Allowed)
        {
            throw new InvalidOperationException(safety.Message);
        }

        if (!File.Exists(patchRtMpqPath))
        {
            throw new FileNotFoundException("Runtime patch_rt.mpq was not found.", patchRtMpqPath);
        }

        if (!File.Exists(statTxtTblPath))
        {
            throw new FileNotFoundException("Patched stat_txt.tbl was not found.", statTxtTblPath);
        }

        var backup = patchRtMpqPath + ".starai-hotkey-original";
        if (!File.Exists(backup))
        {
            File.Copy(patchRtMpqPath, backup, overwrite: false);
        }

        var javaExe = ResolveJavaExe();
        var helperPath = WriteJavaHelper();
        var classPath = $"{paths.SchnailRoot}\\schnail-client.exe";
        if (!File.Exists(classPath))
        {
            throw new FileNotFoundException("SCHNAIL client executable was not found.", classPath);
        }

        var args = $"-cp \"{classPath}\" \"{helperPath}\" \"{patchRtMpqPath}\" \"{statTxtTblPath}\" \"{MpqTargetName}\"";
        RunProcess(javaExe, args, Path.GetDirectoryName(helperPath)!);
    }

    private static string ResolveJavaExe()
    {
        var candidates = new[]
        {
            "java.exe",
            @"C:\Java\jdk-25.0.1\bin\java.exe"
        };

        foreach (var candidate in candidates)
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = candidate,
                    Arguments = "-version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });
                process?.WaitForExit(3000);
                if (process is { ExitCode: 0 })
                {
                    return candidate;
                }
            }
            catch
            {
                // Try the next candidate.
            }
        }

        throw new InvalidOperationException("Java 11+ runtime was not found. Hotkey MPQ patching requires Java source-file mode.");
    }

    private static string WriteJavaHelper()
    {
        var helperDir = Path.Combine(Path.GetTempPath(), "StarAI.PracticeClient", "mpq-helper");
        Directory.CreateDirectory(helperDir);
        var helperPath = Path.Combine(helperDir, "StarAiMpqInsert.java");
        File.WriteAllText(helperPath, JavaHelperSource, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return helperPath;
    }

    private static void RunProcess(string fileName, string arguments, string workingDirectory)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }) ?? throw new InvalidOperationException($"Failed to start process: {fileName}");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{fileName} failed with exit code {process.ExitCode}.{Environment.NewLine}{output}{Environment.NewLine}{error}");
        }
    }

    private const string JavaHelperSource = """
        import java.nio.file.Files;
        import java.nio.file.Path;
        import systems.crigges.jmpq3.JMpqEditor;
        import systems.crigges.jmpq3.MPQOpenOption;
        import org.jasperge.mpq.MPQEditor;

        public class StarAiMpqInsert {
            public static void main(String[] args) throws Exception {
                if (args.length != 3) {
                    throw new IllegalArgumentException("Usage: StarAiMpqInsert <mpq> <source-file> <target-name>");
                }

                String mpqPath = args[0];
                String sourcePath = args[1];
                String targetName = args[2];

                try (MPQEditor editor = new MPQEditor(Path.of(mpqPath))) {
                    editor.addFile(sourcePath, targetName);
                    if (!editor.hasFile(targetName)) {
                        throw new IllegalStateException("Inserted target is not visible in MPQ: " + targetName);
                    }
                    if (!editor.hasFile("rez\\minimappreview.bin")) {
                        throw new IllegalStateException("Required StarCraft data file missing after insert: rez\\minimappreview.bin");
                    }
                }

                byte[] expected = Files.readAllBytes(Path.of(sourcePath));
                try (JMpqEditor verifier = new JMpqEditor(Path.of(mpqPath), MPQOpenOption.READ_ONLY)) {
                    if (!verifier.hasFile(targetName)) {
                        throw new IllegalStateException("Inserted target missing after MPQ close: " + targetName);
                    }
                    if (!verifier.hasFile("rez\\minimappreview.bin")) {
                        throw new IllegalStateException("Required StarCraft data file missing after MPQ close: rez\\minimappreview.bin");
                    }
                    byte[] actual = verifier.extractFileAsBytes(targetName);
                    if (actual.length != expected.length) {
                        throw new IllegalStateException("Inserted TBL size mismatch. expected=" + expected.length + " actual=" + actual.length);
                    }
                }
            }
        }
        """;
}
