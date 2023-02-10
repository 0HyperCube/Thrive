using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Controller for variation in sunlight during an in-game day for the current game.
/// </summary>
[JsonObject(IsReference = true)]
[UseThriveSerializer]
public class DayNightCycle
{
    [JsonProperty]
    private bool isEnabled;

    /// <summary>
    ///   Number of real-time seconds per in-game day.
    /// </summary>
    [JsonProperty]
    private int realTimePerDay;

    /// <summary>
    ///   Current position in the day/night cycle, expressed as a fraction of the day elapsed so far.
    /// </summary>
    [JsonProperty]
    private double fractionOfDayElapsed;

    /// <summary>
    ///   Multiplier used for calculating DayLightFraction.
    /// </summary>
    /// <remarks>
    ///   This exists as it only needs to be calculated once and the calculation for it is confusing.
    /// </remarks>
    [JsonIgnore]
    private float daytimeMultiplier;

    /// <summary>
    ///   Controller for variation in sunlight during an in-game day for the current game.
    /// </summary>
    public DayNightCycle()
    {
        isEnabled = false;
        AverageSunlight = 1.0f;

        // Start the game at noon
        fractionOfDayElapsed = 0.5f;
    }

    [JsonIgnore]
    public float AverageSunlight { get; private set; }

    /// <summary>
    ///   The fraction of daylight you should get.
    ///   light = max(-(FractionOfDayElapsed - 0.5)^2 * daytimeMultiplier + 1, 0)
    ///   desmos: https://www.desmos.com/calculator/vrrk1bkac2
    /// </summary>
    [JsonIgnore]
    public double DayLightFraction => isEnabled ? CalculatePointwiseSunlight(fractionOfDayElapsed) : 1.0f;

    /// <summary>
    ///   Applies the world settings. This needs to be called when this object is created (and not loaded from JSON)
    /// </summary>
    /// <param name="worldSettings">
    ///   The settings to apply. Note that changes to the settings object won't apply before calling this method again
    /// </param>
    public void ApplyWorldSettings(WorldGenerationSettings worldSettings)
    {
        isEnabled = worldSettings.DayNightCycleEnabled;
        realTimePerDay = worldSettings.DayLength;

        CalculateDependentLightData(worldSettings);
    }

    /// <summary>
    ///   Calculates some dependent values that are not saved, this is public to allow recomputing these after loading
    ///   a save
    /// </summary>
    public void CalculateDependentLightData(WorldGenerationSettings worldSettings)
    {
        // This converts the fraction in DaytimeFraction to the power of two needed for DayLightFraction
        daytimeMultiplier = Mathf.Pow(2, 2 / worldSettings.DaytimeFraction);

        AverageSunlight = isEnabled ? CalculateAverageSunlight() : 1.0f;
    }

    public void Process(double delta)
    {
        if (isEnabled)
        {
            fractionOfDayElapsed = (fractionOfDayElapsed + delta / realTimePerDay) % 1;
        }
    }

    /// <summary>
    ///   Calculates sunlight value (on a scale from 0 to 1) at a given point during the day.
    /// </summary>
    /// <remarks>
    ///   If this equation is changed, <see cref="IntegratePointwiseSunlight"/> must also be updated.
    /// </remarks>
    /// <param name="x">Fraction of the day completed, between 0-1</param>
    private double CalculatePointwiseSunlight(double x)
    {
        return Math.Max(1 - daytimeMultiplier * Mathf.Pow(x - 0.5f, 2), 0);
    }

    /// <summary>
    ///   Calculates average sunlight over the course of a day. A relatively expensive operation so should be used
    ///   sparingly.
    /// </summary>
    private float CalculateAverageSunlight()
    {
        // Average is the integral across the interval divided by length of the interval. Since the interval is
        // [0, 1] and hence has length 1, we just return the integral. The current function is only non-zero in the
        // interval [0.5 - 1 / squareRoot(daytimeMultiplier), 0.5 + 1 / squareRoot(daytimeMultiplier)], so we can
        // reduce to only integrating over this interval.
        var daytimeMultiplierRootReciprocal = 1.0f / Mathf.Sqrt(daytimeMultiplier);
        var start = 0.5f - daytimeMultiplierRootReciprocal;
        var end = 0.5f + daytimeMultiplierRootReciprocal;
        return IntegratePointwiseSunlight(end) - IntegratePointwiseSunlight(start);
    }

    /// <summary>
    ///   Calculates the antiderivative of the sunlight function at a given point.
    /// </summary>
    /// <remarks>
    ///   Based on <see cref="CalculatePointwiseSunlight"/>, so must be updated if that is updated.
    /// </remarks>
    /// <param name="x">Fraction of the day completed</param>
    private float IntegratePointwiseSunlight(float x)
    {
        return x - daytimeMultiplier * (Mathf.Pow(x, 3) / 3 - Mathf.Pow(x, 2) / 2 + 0.25f * x);
    }
}
