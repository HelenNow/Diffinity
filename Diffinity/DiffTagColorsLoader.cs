namespace Diffinity;

public static class DiffTagColorsLoader
{
    public static Dictionary<string, string> LoadTagColors()
    {
        string filePath = ".difftagcolors";
        var colorsDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(filePath))
            return colorsDictionary;

        var lines = File.ReadAllLines(filePath);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                continue;

            var parts = trimmed.Split(',');
            if (parts.Length != 2)
                continue;

            string tagName = parts[0].Trim();
            string color = parts[1].Trim();

            if (!string.IsNullOrWhiteSpace(tagName) && !string.IsNullOrWhiteSpace(color))
            {
                colorsDictionary[tagName] = color;
            }
        }
        return colorsDictionary;
    }
}