namespace mkAutoClicker;

public static class Validation
{
    public static IReadOnlyList<string> Validate(ClickProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        List<string> errors = new List<string>(8);

        if (profile.ClicksPerSecond < 0.1 || profile.ClicksPerSecond > 1000.0)
        {
            errors.Add("Clicks/s muss zwischen 0.1 und 1000 liegen.");
        }

        if (profile.SpeedVariationPercent < 0.0 || profile.SpeedVariationPercent > 95.0)
        {
            errors.Add("Variation muss zwischen 0 und 95 Prozent liegen.");
        }

        if (profile.DutyCycleMinPercent < 1 || profile.DutyCycleMinPercent > 99)
        {
            errors.Add("Duty min muss zwischen 1 und 99 Prozent liegen.");
        }

        if (profile.DutyCycleMaxPercent < 1 || profile.DutyCycleMaxPercent > 99)
        {
            errors.Add("Duty max muss zwischen 1 und 99 Prozent liegen.");
        }

        if (profile.DutyCycleMinPercent > profile.DutyCycleMaxPercent)
        {
            errors.Add("Duty min darf nicht groesser als Duty max sein.");
        }

        if (profile.ClickLimit is <= 0)
        {
            errors.Add("Click-Limit muss groesser als 0 sein.");
        }

        if (profile.TimeLimit is not null && profile.TimeLimit.Value < TimeSpan.FromSeconds(1))
        {
            errors.Add("Time-Limit muss mindestens 1 Sekunde sein.");
        }

        if (profile.ActionType == ActionType.KeyboardKey && profile.VirtualKeyCode <= 0)
        {
            errors.Add("Bitte eine Tastaturtaste auswaehlen.");
        }

        return errors;
    }
}
