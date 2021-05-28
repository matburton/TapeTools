
using System;
using System.Collections.Generic;
using System.Linq;

namespace TapeTools.WavConvert.Amiga.Serialisation
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

            var totalCycles = colourCycleGaps.Select(Convert.ToUInt64)
                                             .Aggregate((c, a) => c + a);

            Console.WriteLine($"Total colour cycles:  {totalCycles:n0}");

            return GetFileHeader(startLow)
                  .Concat(GetFileBody(colourCycleGaps))
                  .ToArray();
        }

        private static uint ToColourCycles(TimeSpan pulseGap) =>
            Convert.ToUInt32(Math.Round(pulseGap.Ticks * 0.0709379));

        private static IEnumerable<byte> GetFileHeader(bool startLow)
        {
            yield return BitConverter.GetBytes(startLow).Single();
        }

        // Algorithm: Start with block type 3
        // - How many gaps can a single block X reach? How many bytes is that?
        // - How many bytes for block (X-1) followed by block X to reach that far?
        // - If 2nd is smaller reject type X and retry with type (X-1)

        // ^ Not ideal, could use a windowing algorithm?
        //   i.e. only consider next, say, 64 gaps? Decide on block for that?
        //   Then merge compatible adjacent blocks?
        //   (If we didn't merge then, with 64, the block identifier could be one byte)
        // ^ If so then should wrap each block behind an interface

        // Types: 3 = Override block, can express any gaps as a stream of byte triples
        //        2 = Quad block, can express stream of 4 unique gaps as stream of bit pairs
        //        1 = Toggle block, can express stream of 2 unique gaps as stream of bits
        //        0 = Repeat block, can express repetition of same gap as a count, no stream

        private static IEnumerable<byte> GetFileBody(IReadOnlyList<uint> gaps)
        {
            const int maxBlockLength = 16_384; // 2 ^ 14 (14 bits)

            while (gaps.Any())
            {
                IEnumerable<byte> blockBytes;

                if (gaps.Take(3).Distinct().Count() <= 2)
                {
                    var seen = new HashSet<uint>();

                    int UniqueSeen(uint gap) { seen.Add(gap);
                                               return seen.Count; }
                    var blockGaps = gaps
                                   .TakeWhile(g => UniqueSeen(g) < 3)
                                   .Take(maxBlockLength)
                                   .ToArray();

                    gaps = gaps.Skip(blockGaps.Length).ToArray();

                    blockBytes = GetBlockIdentifier(1, blockGaps.Length)
                                .Concat(GetToggleBlockBytes(blockGaps));
                }
                else
                {
                    var blockGaps = gaps
                                   .Select((gap, index) => (gap, index))
                                   .TakeWhile(t => gaps.Skip(t.index - 1).Take(3).Distinct().Count() is 3)
                                   .Select(t => t.gap)
                                   .Take(maxBlockLength)
                                   .ToArray();

                    gaps = gaps.Skip(blockGaps.Length).ToArray();

                    blockBytes = GetBlockIdentifier(3, blockGaps.Length)
                                .Concat(GetOverrideBlockBytes(blockGaps));
                }

                foreach (var blockByte in blockBytes)
                {
                    yield return blockByte;
                }
            }
        }

        private static IEnumerable<byte> GetBlockIdentifier(int type, int length) =>
            BitConverter.GetBytes((short)((length << 2) | type)).Reverse();

        private static IEnumerable<byte> GetOverrideBlockBytes
            (IEnumerable<uint> gaps) => gaps.SelectMany(Get3Bytes);

        private static IEnumerable<byte> GetToggleBlockBytes
            (IReadOnlyCollection<uint> gaps)
        {
            var distinctGaps = gaps.Distinct().ToArray();

            bool ToBool(uint gap) => gap != distinctGaps[0];

            byte ToByte(IEnumerable<uint> gapGroup) => (byte)
                gapGroup.Select(g => ToBool(g) ? 1 : 0)
                        .Reverse()
                        .Aggregate((a, b) => a << 1 | b);

            var streamBytes = gaps.Select((gap, index) => (gap, index))
                                  .GroupBy(t => t.index / 8, t => t.gap)
                                  .Select(ToByte);

            return distinctGaps.SelectMany(Get3Bytes).Concat(streamBytes);
        }

        private static IEnumerable<byte> Get3Bytes(uint cycles) =>
            GetBytes(cycles).Skip(1);

        private static IEnumerable<byte> GetBytes(uint cycles) =>
            BitConverter.GetBytes(cycles).Reverse();
    }
}