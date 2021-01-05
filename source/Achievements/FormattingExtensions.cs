using System;

namespace FitBot.Achievements
{
    public static class FormattingExtensions
    {
        private const decimal MilesPerMeter = 0.0006213712M;
        private const decimal PoundsPerKilogram = 2.204624M;

        public static string FormatDistance(this decimal? meters, bool isImperial = false) =>
            isImperial
                ? $"{meters * MilesPerMeter:#,##0.#} mi"
                : $"{meters / 1000:#,##0.#} km";

        public static string FormatWeight(this decimal? kilograms, bool isImperial = false) =>
            isImperial
                ? $"{kilograms * PoundsPerKilogram:#,##0.#} lb"
                : $"{kilograms:#,##0.#} kg";

        public static string FormatSpeed(this decimal? metersPerSecond, bool isImperial = false)
        {
            return isImperial
                ? $"{metersPerSecond * 3600M * MilesPerMeter:#,##0.#} mph"
                : $"{metersPerSecond * 3.6M:#,##0.#} km/h";
        }

        public static string FormatRepetitions(this decimal? reps) => $"{reps:N0} rep{(reps != 1 ? "s" : null)}";

        public static string FormatDuration(this decimal? seconds)
        {
            var duration = TimeSpan.FromSeconds((double) (seconds ?? 0));
            if (duration.TotalMinutes < 1)
            {
                return $"{duration.Seconds} second{(duration.Seconds > 1 ? "s" : null)}";
            }
            if (duration.TotalHours < 1)
            {
                if (duration.Seconds == 0)
                {
                    return $"{duration.Minutes} minute{(duration.Minutes > 1 ? "s" : null)}";
                }
                return $"{duration:m\\:ss} minutes";
            }
            if (duration.TotalDays < 1)
            {
                if (duration.Seconds == 0)
                {
                    if (duration.Minutes == 0)
                    {
                        return $"{duration.Hours} hour{(duration.Hours > 1 ? "s" : null)}";
                    }
                    return $"{duration:h\\:mm} hours";
                }
                return $"{duration:h\\:mm\\:ss} hours";
            }
            if (duration.Seconds == 0)
            {
                if (duration.Minutes == 0)
                {
                    return $"{duration.TotalHours:N0} hours";
                }
                return $"{duration.TotalHours:N0}{duration:\\:mm} hours";
            }
            return $"{duration.TotalHours:N0}{duration:\\:mm\\:ss} hours";
        }
    }
}