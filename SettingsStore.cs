using System.IO;
using System.Text.Json;

namespace mkAutoClicker;

public static class SettingsStore {
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions {
        WriteIndented = true
    };

    public static AppSettings Load() {
        string path = GetPath();
        if (!File.Exists(path)) {
            return new AppSettings();
        }

        try {
            string json = File.ReadAllText(path);
            AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            return settings ?? new AppSettings();
        } catch {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings) {
        ArgumentNullException.ThrowIfNull(settings);

        string path = GetPath();
        string directory = Path.GetDirectoryName(path) ?? AppContext.BaseDirectory;
        Directory.CreateDirectory(directory);

        string json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(path, json);
    }

    public static string GetPath() {
        string baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(baseDirectory, "mkClickerWpfSingle", "settings.json");
    }
}
