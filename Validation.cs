namespace mkAutoClicker;

public static class Validation {
    public static IReadOnlyList<string> Validate(ClickProfile profile) {
        ArgumentNullException.ThrowIfNull(profile);

        List<string> errors = new List<string>(8);

        if (profile.ClicksPerSecond < 0.1 || profile.ClicksPerSecond > 1000.0) {
            errors.Add(LocalizationService.GetString("Ui.Validation.ClicksPerSecond", "Clicks/s must be between 0.1 and 1000."));
        }

        if (profile.SpeedVariationPercent < 0.0 || profile.SpeedVariationPercent > 95.0) {
            errors.Add(LocalizationService.GetString("Ui.Validation.Variation", "Variation must be between 0 and 95 percent."));
        }

        if (profile.DutyCycleMinPercent < 1 || profile.DutyCycleMinPercent > 99) {
            errors.Add(LocalizationService.GetString("Ui.Validation.DutyMin", "Duty min must be between 1 and 99 percent."));
        }

        if (profile.DutyCycleMaxPercent < 1 || profile.DutyCycleMaxPercent > 99) {
            errors.Add(LocalizationService.GetString("Ui.Validation.DutyMax", "Duty max must be between 1 and 99 percent."));
        }

        if (profile.DutyCycleMinPercent > profile.DutyCycleMaxPercent) {
            errors.Add(LocalizationService.GetString("Ui.Validation.DutyRange", "Duty min must not be greater than Duty max."));
        }

        if (profile.ClickLimit is <= 0) {
            errors.Add(LocalizationService.GetString("Ui.Validation.ClickLimit", "Click limit must be greater than 0."));
        }

        if (profile.TimeLimit is not null && profile.TimeLimit.Value < TimeSpan.FromSeconds(1)) {
            errors.Add(LocalizationService.GetString("Ui.Validation.TimeLimit", "Time limit must be at least 1 second."));
        }

        if (profile.ActionType == ActionType.KeyboardKey && profile.VirtualKeyCode <= 0) {
            errors.Add(LocalizationService.GetString("Ui.Validation.KeyboardKey", "Please select a keyboard key."));
        }

        return errors;
    }
}
