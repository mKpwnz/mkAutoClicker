using System.Diagnostics;

namespace mkAutoClicker;

public sealed class AutoClickerEngine
{
    private static readonly double StopwatchTicksPerMillisecond = Stopwatch.Frequency / 1000.0;
    private const double ProgressUpdateIntervalMilliseconds = 40.0;

    private readonly Random random;
    private readonly NativeInput input;

    public AutoClickerEngine()
        : this(Random.Shared, new NativeInput())
    {
    }

    public AutoClickerEngine(Random random, NativeInput input)
    {
        this.random = random ?? throw new ArgumentNullException(nameof(random));
        this.input = input ?? throw new ArgumentNullException(nameof(input));
    }

    public Task<RunSummary> RunAsync(
        ClickProfile profile,
        CancellationToken cancellationToken,
        Action<ClickProgress>? progressCallback)
    {
        ArgumentNullException.ThrowIfNull(profile);

        IReadOnlyList<string> errors = Validation.Validate(profile);
        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join(Environment.NewLine, errors), nameof(profile));
        }

        return Task.Run(
            () => this.RunLoop(profile, cancellationToken, progressCallback),
            CancellationToken.None);
    }

    private RunSummary RunLoop(
        ClickProfile profile,
        CancellationToken cancellationToken,
        Action<ClickProgress>? progressCallback)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        int clickCount = 0;
        long nextCycleStartTicks = stopwatch.ElapsedTicks;
        long lastProgressTicks = 0;
        long progressIntervalTicks = MillisecondsToStopwatchTicks(ProgressUpdateIntervalMilliseconds);

        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return CreateSummary(StopReason.Cancelled, clickCount, stopwatch.Elapsed);
            }

            if (profile.TimeLimit is not null && stopwatch.Elapsed >= profile.TimeLimit.Value)
            {
                return CreateSummary(StopReason.TimeLimitReached, clickCount, stopwatch.Elapsed);
            }

            if (profile.ClickLimit is not null && clickCount >= profile.ClickLimit.Value)
            {
                return CreateSummary(StopReason.ClickLimitReached, clickCount, stopwatch.Elapsed);
            }

            double intervalMs = CalculateIntervalMilliseconds(
                profile.ClicksPerSecond,
                profile.SpeedVariationPercent,
                this.random);
            double holdMs = CalculateHoldMilliseconds(
                intervalMs,
                profile.DutyCycleMinPercent,
                profile.DutyCycleMaxPercent,
                this.random);

            long intervalTicks = MillisecondsToStopwatchTicks(intervalMs);
            long holdTicks = MillisecondsToStopwatchTicks(holdMs);
            long upAtTicks = nextCycleStartTicks + holdTicks;
            long nextTick = nextCycleStartTicks + intervalTicks;

            this.input.SendDown(profile.ActionType, profile.VirtualKeyCode);
            WaitUntil(upAtTicks, stopwatch, cancellationToken);
            this.input.SendUp(profile.ActionType, profile.VirtualKeyCode);

            clickCount++;
            if (progressCallback is not null)
            {
                long nowTicks = stopwatch.ElapsedTicks;
                bool shouldReport = clickCount == 1 || (nowTicks - lastProgressTicks) >= progressIntervalTicks;
                if (shouldReport)
                {
                    lastProgressTicks = nowTicks;
                    progressCallback(new ClickProgress
                    {
                        ClickCount = clickCount,
                        Elapsed = stopwatch.Elapsed
                    });
                }
            }

            WaitUntil(nextTick, stopwatch, cancellationToken);
            nextCycleStartTicks = nextTick;
        }
    }

    private static long MillisecondsToStopwatchTicks(double milliseconds)
    {
        double ticks = milliseconds * StopwatchTicksPerMillisecond;
        return (long)Math.Max(1.0, Math.Round(ticks));
    }

    private static double CalculateIntervalMilliseconds(double clicksPerSecond, double variationPercent, Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        Ensure.That(clicksPerSecond > 0.0, "Clicks per second must be greater than zero.");

        double baseInterval = 1000.0 / clicksPerSecond;
        double variation = Math.Clamp(variationPercent / 100.0, 0.0, 0.95);
        double jitter = (random.NextDouble() * 2.0) - 1.0;
        double multiplier = 1.0 + (jitter * variation);
        double interval = baseInterval * multiplier;

        return Math.Max(1.0, interval);
    }

    private static double CalculateHoldMilliseconds(double intervalMilliseconds, int minPercent, int maxPercent, Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        Ensure.That(intervalMilliseconds >= 1.0, "Interval must be >= 1ms.");
        Ensure.That(minPercent > 0, "Duty min must be greater than zero.");
        Ensure.That(maxPercent >= minPercent, "Duty max must be >= min.");

        double min = minPercent / 100.0;
        double max = maxPercent / 100.0;
        double duty = min + (random.NextDouble() * (max - min));
        double hold = intervalMilliseconds * duty;

        return Math.Clamp(hold, 0.0, intervalMilliseconds);
    }

    private static void WaitUntil(long targetTicks, Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            long remainingTicks = targetTicks - stopwatch.ElapsedTicks;
            if (remainingTicks <= 0)
            {
                return;
            }

            double remainingMs = remainingTicks / StopwatchTicksPerMillisecond;
            if (remainingMs >= 12.0)
            {
                int sleepMs = (int)Math.Max(1.0, Math.Floor(remainingMs - 1.5));
                Thread.Sleep(sleepMs);
                continue;
            }

            if (remainingMs >= 4.0)
            {
                Thread.Sleep(0);
                continue;
            }

            if (remainingMs >= 1.0)
            {
                Thread.SpinWait(220);
                continue;
            }

            Thread.SpinWait(60);
        }
    }

    private static RunSummary CreateSummary(StopReason reason, int clickCount, TimeSpan elapsed)
    {
        return new RunSummary
        {
            Reason = reason,
            ClickCount = clickCount,
            Elapsed = elapsed
        };
    }
}
