namespace Diffinity;

public static class DiffTagsLoader
{
    public static Dictionary<string, List<string>> LoadObjectTags()
    {
        string filePath = ".difftags";
        var tagsDictionary = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(filePath))
            return tagsDictionary;

        var lines = File.ReadAllLines(filePath);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                continue;

            var parts = trimmed.Split(',');
            if (parts.Length < 2)
                continue;

            string objectName = parts[0].Trim().Replace("[", "").Replace("]", "");

            var tags = parts.Skip(1)
                           .Select(t => t.Trim())
                           .Where(t => !string.IsNullOrWhiteSpace(t))
                           .ToList();

            if (tags.Any())
            {
                tagsDictionary[objectName] = tags;
            }
        }

        return tagsDictionary;
    }
}