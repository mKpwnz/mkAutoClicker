using System.Windows;

namespace mkAutoClicker;

public partial class App : Application {
    protected override void OnStartup(StartupEventArgs e) {
        LocalizationService.ApplyLanguage("en");
        base.OnStartup(e);
    }
}