using UnityEngine;

public static class FloatExtensions
{
    /// <summary>
    /// Returns this float as a duration string in the (HH):MM:SS(.mmm) format,
    /// where the float represents a duration in seconds.
    /// </summary>
    public static string GetAsDuration(this float seconds, int millisecondDigits = 0)
    {
        if (millisecondDigits < 0 || millisecondDigits > 3) throw new System.ArgumentOutOfRangeException(nameof(millisecondDigits), "millisecondDigits must be between 0 and 3.");

        bool isNegative = seconds < 0;
        double absSeconds = System.Math.Abs(seconds);

        // Round to the requested precision in seconds
        double factor = System.Math.Pow(10, millisecondDigits);
        double roundedSeconds = System.Math.Round(absSeconds * factor, 0, System.MidpointRounding.AwayFromZero) / factor;

        var ts = System.TimeSpan.FromSeconds(roundedSeconds);

        // Build HH:MM:SS // MM:SS // SS
        string core = ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}"
            : (ts.TotalMinutes >= 1 ? $"{ts.Minutes:00}:{ts.Seconds:00}" : $"{ts.Seconds}");

        // Append milliseconds if requested
        if (millisecondDigits > 0)
        {
            // ts.Milliseconds is 0–999, consistent with our roundedSeconds
            int ms = ts.Milliseconds;

            // Reduce to the requested number of digits (e.g. 3 -> /1, 2 -> /10, 1 -> /100)
            int divisor = (int)System.Math.Pow(10, 3 - millisecondDigits);
            int shortened = ms / divisor;

            core += "." + shortened.ToString(new string('0', millisecondDigits));
        }

        return isNegative ? "-" + core : core;
    }
}
