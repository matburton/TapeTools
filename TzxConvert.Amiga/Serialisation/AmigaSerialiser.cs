
using System;
using System.Collections.Generic;
using System.Linq;

namespace TapeTools.TzxConvert.Amiga.Serialisation
{
    internal sealed class AmigaSerialiser
    {
        public byte[] ToBytes(IReadOnlyCollection<TimeSpan> pulseGaps,
                              bool startLow)
        {
            if (pulseGaps is null)
            {
                throw new ArgumentNullException(nameof(pulseGaps));
            }

            var colourCycleGaps = pulseGaps.Select(ToColourCycles).ToArray();

            var totalCycles = colourCycleGaps
                             .Select(Convert.ToUInt64)
                             .Aggregate((ulong)0, (c, a) => c + a);

            Console.WriteLine($"Total colour cycles:  {totalCycles:n0}");

            var cyclesToIndex = colourCycleGaps
                .GroupBy(c => c)
                .OrderByDescending(g => g.Count())
                .Select((g, i) => new { Cycles = g.Key, Index = i })
                .Take(15)
                .ToDictionary(r => r.Cycles, r => r.Index);

            int? GetIndex(uint c) => cyclesToIndex.TryGetValue(c, out var index)
                                   ? index : (int?)null;

            return ToHeaderBytes(cyclesToIndex)
                  .Append(BitConverter.GetBytes(startLow).Single())
                  .Concat(GetBytes(colourCycleGaps, GetIndex))
                  .ToArray();
        }

        private static uint ToColourCycles(TimeSpan pulseGap) =>
            Convert.ToUInt32(Math.Round(pulseGap.Ticks * 0.0709379));

        private static IEnumerable<byte> GetBytes
            (IReadOnlyList<uint> cycles, Func<uint, int?> getIndex)
        {
            var bytes = new List<byte>();

            for (var index = 0; index < cycles.Count; index += 2)
            {
                var lastGap = index + 1 == cycles.Count;
                
                var currentIndex = getIndex(cycles[index]);

                var nextIndex = lastGap ? null
                              : getIndex(cycles[index + 1]);

                bytes.Add(Convert.ToByte( ((currentIndex ?? 0xF) << 4)
                                         | (nextIndex    ?? 0xF)));

                if (currentIndex == null)
                {
                    bytes.AddRange(Get3Bytes(cycles[index]));
                }

                if (nextIndex == null && !lastGap)
                {
                    bytes.AddRange(Get3Bytes(cycles[index + 1]));
                }
            }

            return bytes;
        }

        private static IEnumerable<byte> ToHeaderBytes
            (IReadOnlyDictionary<uint, int> cyclesToIndex)
        {
            var bytes = new List<byte>();

            var orderedCycles = cyclesToIndex.OrderBy(p => p.Value)
                                             .Select(p => p.Key);

            foreach(var cycles in orderedCycles)
            {
                bytes.AddRange(GetBytes(cycles));
            }

            for (var index = 15; index > cyclesToIndex.Count; --index)
            {
                bytes.AddRange(GetBytes(0)); // Padding
            }

            return bytes;
        }

        private static IEnumerable<byte> Get3Bytes(uint cycles) =>
            GetBytes(cycles).Skip(1);

        private static IEnumerable<byte> GetBytes(uint cycles)
        {
            var bytes = new byte[4];

            BitConverter.TryWriteBytes(bytes, cycles);

            return bytes.Reverse();
        }
    }
}