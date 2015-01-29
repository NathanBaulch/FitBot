using System;

namespace FitBot.Achievements
{
    public static class FormattingExtensions
    {
        private const decimal MilesPerMeter = 0.0006213712M;
        private const decimal PoundsPerKilogram = 2.204624M;

        public static string FormatDistance(this decimal? meters, bool isImperial = false)
        {
            return isImperial
                       ? string.Format("{0:#,##0.#} mi", meters*MilesPerMeter)
                       : string.Format("{0:#,##0.#} km", meters/1000);
        }

        public static string FormatWeight(this decimal? kilograms, bool isImperial = false)
        {
            return isImperial
                       ? string.Format("{0:#,##0.#} lb", kilograms*PoundsPerKilogram)
                       : string.Format("{0:#,##0.#} kg", kilograms);
        }

        public static string FormatSpeed(this decimal? metersPerSecond, bool isImperial = false)
        {
            return isImperial
                       ? string.Format("{0:#,##0.#} mph", metersPerSecond*3600M*MilesPerMeter)
                       : string.Format("{0:#,##0.#} km/h", metersPerSecond*3.6M);
        }

        public static string FormatRepetitions(this decimal? reps)
        {
            return string.Format("{0:N0} rep{1}", reps, reps != 1 ? "s" : null);
        }

        public static string FormatDuration(this decimal? seconds)
        {
            var duration = TimeSpan.FromSeconds((double) seconds.Value);
            if (duration.TotalMinutes < 1)
            {
                return string.Format("{0} second{1}", duration.Seconds, duration.Seconds != 1 ? "s" : null);
            }
            if (duration.TotalHours < 1)
            {
                if (duration.Seconds == 0)
                {
                    return string.Format("{0} minute{1}", duration.Minutes, duration.Minutes != 1 ? "s" : null);
                }
                return string.Format("{0:m\\:ss} minutes", duration);
            }
            if (duration.TotalDays < 1)
            {
                if (duration.Seconds == 0)
                {
                    if (duration.Minutes == 0)
                    {
                        return string.Format("{0} hour{1}", duration.Hours, duration.Hours != 1 ? "s" : null);
                    }
                    return string.Format("{0:h\\:mm} hours", duration);
                }
                return string.Format("{0:h\\:mm\\:ss} hours", duration);
            }
            if (duration.Seconds == 0)
            {
                if (duration.Minutes == 0)
                {
                    return string.Format("{0:N0} hours", duration.TotalHours);
                }
                return string.Format("{0:N0}{1:\\:mm} hours", duration.TotalHours, duration);
            }
            return string.Format("{0:N0}{1:\\:mm\\:ss} hours", duration.TotalHours, duration);
        }
    }
}