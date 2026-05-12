using System.Globalization;
using System.Windows;

namespace mkAutoClicker;

public static class LocalizationService {
    private const string DefaultLanguageCode = "en";
    private const string OverlayDictionaryMarkerKey = "Localization.Overlay";

    public static string CurrentLanguageCode { get; private set; } = DefaultLanguageCode;

    public static void ApplyLanguage(string? languageCode) {
        string normalizedCode = NormalizeLanguageCode(languageCode);
        ResourceDictionary applicationResources = Application.Current.Resources;
        int existingIndex = -1;
        for (int index = 0; index < applicationResources.MergedDictionaries.Count; index++) {
            ResourceDictionary dictionary = applicationResources.MergedDictionaries[index];
            if (dictionary.Contains(OverlayDictionaryMarkerKey)) {
                existingIndex = index;
                break;
            }
        }

        if (normalizedCode == DefaultLanguageCode) {
            if (existingIndex >= 0) {
                applicationResources.MergedDictionaries.RemoveAt(existingIndex);
            }
            CurrentLanguageCode = normalizedCode;
            return;
        }

        string dictionaryPath = $"Themes/Strings.{normalizedCode}.xaml";
        ResourceDictionary replacementDictionary = new ResourceDictionary {
            Source = new Uri(dictionaryPath, UriKind.Relative)
        };

        if (existingIndex >= 0) {
            applicationResources.MergedDictionaries[existingIndex] = replacementDictionary;
        } else {
            applicationResources.MergedDictionaries.Insert(1, replacementDictionary);
        }

        CurrentLanguageCode = normalizedCode;
    }

    public static string GetString(string key, string? fallback = null) {
        if (string.IsNullOrWhiteSpace(key)) {
            return string.Empty;
        }

        object? value = Application.Current.TryFindResource(key);
        if (value is string text && !string.IsNullOrWhiteSpace(text)) {
            return text;
        }

        if (!string.IsNullOrWhiteSpace(fallback)) {
            return fallback;
        }

        return key;
    }

    public static string Format(string key, params object[] args) {
        string formatText = GetString(key, key);
        return string.Format(CultureInfo.CurrentCulture, formatText, args);
    }

    private static string NormalizeLanguageCode(string? languageCode) {
        if (string.Equals(languageCode, "de", StringComparison.OrdinalIgnoreCase)) {
            return "de";
        }

        return DefaultLanguageCode;
    }
}
