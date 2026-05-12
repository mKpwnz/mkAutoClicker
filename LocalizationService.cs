using System.Globalization;
using System.Windows;

namespace mkAutoClicker;

public static class LocalizationService {
    private const string DefaultLanguageCode = "en";
    private const string OverlayDictionaryMarkerKey = "Localization.Overlay";

    public static string CurrentLanguageCode { get; private set; } = DefaultLanguageCode;

    public static void ApplyLanguage(string? languageCode) {
        var normalizedCode = NormalizeLanguageCode(languageCode);
        var applicationResources = Application.Current.Resources;
        var existingIndex = -1;
        for (var index = 0; index < applicationResources.MergedDictionaries.Count; index++) {
            var dictionary = applicationResources.MergedDictionaries[index];
            if (dictionary.Contains(OverlayDictionaryMarkerKey)) {
                existingIndex = index;
                break;
            }
        }

        if (normalizedCode == DefaultLanguageCode) {
            if (existingIndex >= 0) applicationResources.MergedDictionaries.RemoveAt(existingIndex);
            CurrentLanguageCode = normalizedCode;
            return;
        }

        var dictionaryPath = $"Language/Strings.{normalizedCode}.xaml";
        var replacementDictionary = new ResourceDictionary {
            Source = new Uri(dictionaryPath, UriKind.Relative)
        };

        if (existingIndex >= 0)
            applicationResources.MergedDictionaries[existingIndex] = replacementDictionary;
        else
            applicationResources.MergedDictionaries.Insert(1, replacementDictionary);

        CurrentLanguageCode = normalizedCode;
    }

    public static string GetString(string key, string? fallback = null) {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;

        var value = Application.Current.TryFindResource(key);
        if (value is string text && !string.IsNullOrWhiteSpace(text)) return text;

        if (!string.IsNullOrWhiteSpace(fallback)) return fallback;

        return key;
    }

    public static string Format(string key, params object[] args) {
        var formatText = GetString(key, key);
        return string.Format(CultureInfo.CurrentCulture, formatText, args);
    }

    private static string NormalizeLanguageCode(string? languageCode) {
        if (string.Equals(languageCode, "de", StringComparison.OrdinalIgnoreCase)) return "de";
        if (string.Equals(languageCode, "fr", StringComparison.OrdinalIgnoreCase)) return "fr";
        if (string.Equals(languageCode, "zh", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(languageCode, "zh-cn", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(languageCode, "cn", StringComparison.OrdinalIgnoreCase)) return "zh";

        return DefaultLanguageCode;
    }
}
