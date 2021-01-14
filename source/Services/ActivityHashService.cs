using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using FitBot.Model;

namespace FitBot.Services
{
    public class ActivityHashService : IActivityHashService
    {
        private static readonly ThreadLocal<HashAlgorithm> _hasher = new(() => HashAlgorithm.Create("MD5"));

        public int Hash(IEnumerable<Activity> activities)
        {
            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                foreach (var activity in activities)
                {
                    bw.Write(activity.Name ?? string.Empty);
                    foreach (var set in activity.Sets)
                    {
                        bw.Write(set.Points);
                        bw.Write(NormalizeDecimal(set.Distance));
                        bw.Write(NormalizeDecimal(set.Duration));
                        bw.Write(NormalizeDecimal(set.Speed));
                        bw.Write(NormalizeDecimal(set.Repetitions));
                        bw.Write(NormalizeDecimal(set.Weight));
                        bw.Write(NormalizeDecimal(set.HeartRate));
                        bw.Write(set.Difficulty ?? string.Empty);
                        bw.Write(set.IsPr);
                        bw.Write(set.IsImperial);
                    }
                }
            }

            ms = new MemoryStream(_hasher.Value.ComputeHash(ms.ToArray()));
            var hash = 0;
            using (var br = new BinaryReader(ms))
            {
                while (ms.Position + 4 <= ms.Length)
                {
                    hash ^= br.ReadInt32();
                }
            }
            return hash;
        }

        private static decimal NormalizeDecimal(decimal? value)
        {
            if (value == null)
            {
                return 0;
            }

            var n = value.Value;
            var scale = decimal.GetBits(n)[3] >> 16 & 31;
            if (scale == 0)
            {
                return n;
            }

            while (Math.Round(n, 0) != n)
            {
                n *= 10;
                scale--;
            }

            if (scale == 0)
            {
                return n;
            }

            return SqlDecimal.AdjustScale(n, -scale, false).Value;
        }
    }
}