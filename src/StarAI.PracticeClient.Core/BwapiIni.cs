using System.Text.RegularExpressions;

namespace StarAI.PracticeClient.Core;

public sealed class BwapiIni
{
    private readonly List<string> _lines;

    private BwapiIni(IEnumerable<string> lines)
    {
        _lines = lines.ToList();
    }

    public static BwapiIni Parse(string text)
    {
        return new BwapiIni(text.Replace("\r\n", "\n").Split('\n'));
    }

    public static BwapiIni Load(string path)
    {
        return Parse(File.ReadAllText(path));
    }

    public string? Get(string section, string key)
    {
        var range = FindSection(section);
        if (range.Start < 0)
        {
            return null;
        }

        var keyRegex = KeyRegex(key);
        for (var i = range.Start + 1; i < range.End; i++)
        {
            var match = keyRegex.Match(_lines[i]);
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
                _lines.Add("");
            }

            _lines.Add($"[{section}]");
            _lines.Add($"{key} = {value}");
            return;
        }

        var keyRegex = KeyRegex(key);
        for (var i = range.Start + 1; i < range.End; i++)
        {
            if (keyRegex.IsMatch(_lines[i]))
            {
                _lines[i] = $"{key} = {value}";
                return;
            }
        }

        _lines.Insert(range.End, $"{key} = {value}");
    }

    public void Save(string path)
    {
        File.WriteAllText(path, ToString(), System.Text.Encoding.ASCII);
    }

    public override string ToString()
    {
        return string.Join(Environment.NewLine, _lines).TrimEnd() + Environment.NewLine;
    }

    private (int Start, int End) FindSection(string section)
    {
        var sectionLine = $"[{section}]";
        var start = -1;
        for (var i = 0; i < _lines.Count; i++)
        {
            if (string.Equals(_lines[i].Trim(), sectionLine, StringComparison.OrdinalIgnoreCase))
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
