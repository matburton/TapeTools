
using System;
using System.Collections.Generic;
using System.Linq;

using BlockMethod =
    System.Func<System.ArraySegment<byte>, System.Collections.Generic.List<System.TimeSpan>, System.ArraySegment<byte>>;

namespace TapeTools.TzxConvert.Amiga.Serialisation
{
    /// <remarks>
    /// See: http://k1.spdns.de/Develop/Projects/zasm/Info/TZX%20format.html
    /// </remarks>
    internal sealed class TzxSerialiser
    {
        public IReadOnlyCollection<TimeSpan> ToPulseGaps(byte[] fileBytes,
                                                         out bool startLow)
        {
            if (fileBytes is null)
            {
                throw new ArgumentNullException(nameof(fileBytes));
            }

            startLow = true;

            var remainingBytes = VerifyHeader(fileBytes);

            var allPulseGaps = new List<TimeSpan>();

            while (remainingBytes.Any())
            {
                BlockMethod blockMethod = remainingBytes[0] switch
                {
                    0x11  => TurboSpeedDataBlock,
                    0x20  => PauseBlock,
                    {} id => throw new Exception($"Unsupported TZX block {id:X}")
                };

                remainingBytes = blockMethod(remainingBytes[1 ..], allPulseGaps);
            }

            return allPulseGaps;
        }

        private static ArraySegment<byte> PauseBlock
            (ArraySegment<byte> remainingBytes,
             List<TimeSpan> allPulseGaps)
        {
            var pause = TimeSpan.FromMilliseconds(TakeWord(ref remainingBytes));

            if (pause == TimeSpan.Zero)
            {
                // Ignored
            }
            else if (!allPulseGaps.Any())
            {
                allPulseGaps.Add(pause); // We start low
            }
            else if (allPulseGaps.Count % 2 is 1) // Already low
            {
                allPulseGaps[^1] += pause; // Stay low
            }
            else
            {
                var highTime = allPulseGaps[^1];

                if (highTime < TimeSpan.FromMilliseconds(1))
                {
                    allPulseGaps[^1] = TimeSpan.FromMilliseconds(1);

                    pause -= TimeSpan.FromMilliseconds(1) - highTime;

                    if (pause < TimeSpan.Zero)
                    {
                        throw new Exception("Pause low reduced to less than zero");
                    }
                }

                allPulseGaps.Add(pause); // Transition to low
            }

            return remainingBytes;
        }

        private static ArraySegment<byte> TurboSpeedDataBlock
            (ArraySegment<byte> remainingBytes,
             List<TimeSpan> allPulseGaps)
        {
            var pilotPulseLength      = TakeWord(ref remainingBytes);
            var firstSyncPulseLength  = TakeWord(ref remainingBytes);
            var secondSyncPulseLength = TakeWord(ref remainingBytes);
            var zeroBitLength         = TakeWord(ref remainingBytes);
            var oneBitLength          = TakeWord(ref remainingBytes);
            var pilotPulseCount       = TakeWord(ref remainingBytes);

            allPulseGaps.AddRange
                (Enumerable.Repeat(ToPulseGap(pilotPulseLength),
                                   pilotPulseCount));

            allPulseGaps.Add(ToPulseGap(firstSyncPulseLength));
            allPulseGaps.Add(ToPulseGap(secondSyncPulseLength));

            var bitsUsedInLastByte    = TakeByte(ref remainingBytes);
            var pauseAferMilliseconds = TakeWord(ref remainingBytes);
            
            if (remainingBytes.Count < 3)
            {
                throw new Exception("Not enough bytes for data length");
            }

            var dataByteCount = BitConverter.ToInt32
                (remainingBytes[.. 3].Append((byte)0).ToArray(), 0);

            if (remainingBytes.Count < dataByteCount)
            {
                throw new Exception("Not enough bytes for data");
            }

            remainingBytes = remainingBytes[3 ..];

            void AddPulseGaps(byte @byte, int bitCount = 8)
            {
                for (var i = 0; i < bitCount; ++i)
                {
                    var tStates = (@byte & 0b1000_0000) switch
                        { 0 => zeroBitLength,
                          _ => oneBitLength };

                    var pulseGap = ToPulseGap(tStates);

                    allPulseGaps.Add(pulseGap);
                    allPulseGaps.Add(pulseGap);

                    @byte <<= 1;
                }
            }

            for (var index = 0; index < dataByteCount - 1; ++index)
            {
                AddPulseGaps(remainingBytes[index]);
            }

            if (dataByteCount is not 0)
            {
                AddPulseGaps(remainingBytes[dataByteCount - 1],
                             bitsUsedInLastByte);
            }

            if (allPulseGaps.Any()) // TODO: Is this dumb?
            {
                allPulseGaps[^1] += TimeSpan.FromMilliseconds
                    (pauseAferMilliseconds);
            } // TODO: Else?

            return remainingBytes[dataByteCount ..];
        }

        private static ArraySegment<byte> VerifyHeader(byte[] fileBytes)
        {
            if (fileBytes.Length < 10)
            {
                throw new Exception("TZX header was too short");
            }

            if (!fileBytes[.. 8].SequenceEqual(TzxHeaderPrefix))
            {
                throw new Exception("TZX header was invalid");
            }

            var version = new Version(fileBytes[8], fileBytes[9]);

            if (version > new Version(1, 20))
            {
                throw new Exception($"TZX version {version} is too high");
            }

            return fileBytes[10 ..];
        }

        private static ushort TakeWord(ref ArraySegment<byte> remainingBytes)
        {
            if (remainingBytes.Count < 2)
            {
                throw new Exception("Not enough bytes to take word");
            }

            var word = BitConverter.ToUInt16(remainingBytes[.. 2].ToArray(), 0);

            remainingBytes = remainingBytes[2 ..];

            return word;
        }

        private static byte TakeByte(ref ArraySegment<byte> remainingBytes)
        {
            if (!remainingBytes.Any())
            {
                throw new Exception("Not enough bytes to take byte");
            }

            var @byte = remainingBytes[0];

            remainingBytes = remainingBytes[1 ..];

            return @byte;
        }

        private static TimeSpan ToPulseGap(ushort tStates)
        {
            var ticks = (long)Math.Round(tStates * 2.857142857142857);

            return TimeSpan.FromTicks(ticks);
        }

        private static readonly IReadOnlyList<byte> TzxHeaderPrefix = new byte[]
            { 0x5A, 0x58, 0x54, 0x61, 0x70, 0x65, 0x21, 0x1A };
    }
}