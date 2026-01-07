using System.Text.RegularExpressions;

namespace Diffinity;

public static class DocfinityLoader
{
    public static Dictionary<string, string> LoadObjectDocumentation()
    {
        string filePath = ".docfinity";
        if (!File.Exists(filePath))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var documentation = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var lines = File.ReadAllLines(filePath);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                continue;

            // Parse format: schema.objectname = Documentation text
            var match = Regex.Match(trimmed, @"^([^\s=]+)\s*=\s*(.+)$");
            if (match.Success)
            {
                string objectName = match.Groups[1].Value.Trim();
                string docText = match.Groups[2].Value.Trim();
                documentation[objectName] = docText;
            }
        }

        return documentation;
    }
}