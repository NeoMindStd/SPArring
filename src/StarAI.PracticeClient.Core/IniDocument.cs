using System.Text;
using System.Text.RegularExpressions;

namespace StarAI.PracticeClient.Core;

public sealed class IniDocument
{
    private readonly List<string> _lines;

    private IniDocument(IEnumerable<string> lines)
    {
        _lines = lines.ToList();
    }

    public static IniDocument Parse(string text)
    {
        return new IniDocument(text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'));
    }

    public static IniDocument LoadOrCreate(string path, string defaultSection)
    {
        return File.Exists(path)
            ? Parse(File.ReadAllText(path, Encoding.UTF8))
            : Parse($"[{defaultSection}]{Environment.NewLine}");
    }

    public string? Get(string section, string key)
    {
        var range = FindSection(section);
        if (range.Start < 0)
        {
            return null;
        }

        var regex = KeyRegex(key);
        for (var i = range.Start + 1; i < range.End; i++)
        {
            var match = regex.Match(_lines[i]);
            if (match.Success)
            {
                return match.Groups["value"].Value.Trim();
            }
        }

        return null;
    }

    public void Set(string section, string key, string value)
    {
        var range = FindSection(section);
        if (range.Start < 0)
        {
            if (_lines.Count > 0 && !string.IsNullOrWhiteSpace(_lines[^1]))
            {
                _lines.Add(string.Empty);
            }

            _lines.Add($"[{section}]");
            _lines.Add($"{key} = {value}");
            return;
        }

        var regex = KeyRegex(key);
        for (var i = range.Start + 1; i < range.End; i++)
        {
            if (regex.IsMatch(_lines[i]))
            {
                _lines[i] = $"{key} = {value}";
                return;
            }
        }

        _lines.Insert(range.End, $"{key} = {value}");
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, ToString(), Encoding.ASCII);
    }

    public override string ToString()
    {
        return string.Join(Environment.NewLine, _lines).TrimEnd() + Environment.NewLine;
    }

    private (int Start, int End) FindSection(string section)
    {
        var header = $"[{section}]";
        var start = -1;
        for (var i = 0; i < _lines.Count; i++)
        {
            if (string.Equals(_lines[i].Trim(), header, StringComparison.OrdinalIgnoreCase))
            {
                start = i;
                break;
            }
        }

        if (start < 0)
        {
            return (-1, -1);
        }

        var end = _lines.Count;
        for (var i = start + 1; i < _lines.Count; i++)
        {
            var trimmed = _lines[i].Trim();
            if (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal))
            {
                end = i;
                break;
            }
        }

        return (start, end);
    }

    private static Regex KeyRegex(string key)
    {
        return new Regex(@"^\s*" + Regex.Escape(key) + @"\s*=\s*(?<value>.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }
}
