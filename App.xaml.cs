using System.Windows;

namespace mkAutoClicker;

public partial class App : Application {
    protected override void OnStartup(StartupEventArgs e) {
        var settings = SettingsStore.Load();
        LocalizationService.ApplyLanguage(settings.LanguageCode);
        base.OnStartup(e);
    }
}
