using System.Text;

namespace StarAI.PracticeClient.Core;

public sealed class HotkeyEntry
{
    public HotkeyEntry(string stringId, string commandId, string hotkey, string description, string originalLine)
    {
        StringId = stringId;
        CommandId = commandId;
        Hotkey = hotkey;
        Description = description;
        OriginalLine = originalLine;
    }

    public string StringId { get; set; }
    public string CommandId { get; set; }
    public string Hotkey { get; set; }
    public string Description { get; set; }
    public string OriginalLine { get; set; }
}

public sealed class HotkeyCsvStore
{
    public const string SchnailCsvPath = @"C:\Program Files (x86)\SCHNAIL Client\res\sc_hotkeys.csv";
    public const string SchnailMessagesPath = @"C:\Program Files (x86)\SCHNAIL Client\res\messages_en.properties";

    public IReadOnlyList<HotkeyEntry> Load(string? csvPath = null, string? messagesPath = null)
    {
        csvPath ??= SchnailCsvPath;
        messagesPath ??= SchnailMessagesPath;
        if (!File.Exists(csvPath))
        {
            return Array.Empty<HotkeyEntry>();
        }

        var descriptions = LoadDescriptions(messagesPath);
        return File.ReadAllLines(csvPath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => ParseLine(line, descriptions))
            .Where(entry => entry is not null)
            .Cast<HotkeyEntry>()
            .OrderBy(entry => entry.CommandId)
            .ToArray();
    }

    public void SaveWorkingCopy(string starCraftRoot, IReadOnlyList<HotkeyEntry> entries)
    {
        var path = Path.Combine(starCraftRoot, "bwapi-data", "read", "sc_hotkeys.csv");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllLines(path, entries.Select(Serialize));
    }

    private static HotkeyEntry? ParseLine(string line, Dictionary<string, string> descriptions)
    {
        var parts = line.Split(',');
        if (parts.Length < 3)
        {
            return null;
        }

        var commandId = parts[2].Trim();
        var text = parts.Length >= 4 ? parts[3].Trim() : parts[1].Trim();
        var key = text.Length > 0 ? text[..1] : "";
        descriptions.TryGetValue($"hotkey_desc_{commandId}", out var description);
        description ??= commandId.Replace('_', ' ');

        return new HotkeyEntry(parts[0].Trim(), commandId, key, description, line);
    }

    private static string Serialize(HotkeyEntry entry)
    {
        var key = string.IsNullOrWhiteSpace(entry.Hotkey) ? " " : entry.Hotkey.Trim()[..1].ToLowerInvariant();
        var upper = key.ToUpperInvariant();
        var description = entry.Description.Replace(",", "");
        return $"{entry.StringId},{key}<1>{description}(<3>{upper}<1>)<0>,{entry.CommandId}";
    }

    private static Dictionary<string, string> LoadDescriptions(string? messagesPath)
    {
        if (messagesPath is null || !File.Exists(messagesPath))
        {
            return new Dictionary<string, string>();
        }

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
}
