
using System;
using System.Collections.Generic;
using System.Linq;

namespace TapeTools.WavConvert.Amiga.Serialisation
{
    internal sealed class WavSerialiser
    {
        public IReadOnlyCollection<TimeSpan> ToPulseGaps(byte[] wavFileBytes,
                                                         out bool startLow)
        {
            if (wavFileBytes is null)
            {
                throw new ArgumentNullException(nameof(wavFileBytes));
            }

            VerifyCompatibility(wavFileBytes);

            startLow = BitConverter.ToInt16(wavFileBytes, 44) < 0;

            var pulseGaps = ToSampleCountGaps(wavFileBytes)
                           .Select(c => c * 226.7573696145125)
                           .Select(t => Math.Round(t))
                           .Select(Convert.ToInt64)
                           .Select(TimeSpan.FromTicks)
                           .ToArray();

            pulseGaps[0] = TimeSpan.Zero;

            return pulseGaps;
        }

        /// <remarks>For debugging, WAV to WAV round-trip-ish</remarks>
        ///
        public byte[] ToBytes(IReadOnlyCollection<TimeSpan> pulseGaps,
                              bool startLow)
        {
            if (pulseGaps is null)
            {
                throw new ArgumentNullException(nameof(pulseGaps));
            }

            static uint ToColourCycles(TimeSpan pulseGap) =>
                Convert.ToUInt32(Math.Round(pulseGap.Ticks * 0.0709379));

            static int ToSampleCount(uint colourCycles) =>
                Convert.ToInt32(Math.Round(colourCycles * 0.0621670503355752));

            // ReSharper disable once IteratorNeverReturns
            IEnumerable<bool> Oscillator()
            {
                while (true)
                {
                    yield return  startLow;
                    yield return !startLow;
                }
            }

            return m_WavHeader.Concat(pulseGaps.Select(ToColourCycles)
                                               .Select(ToSampleCount)
                                               .Zip(Oscillator(), ToSamples)
                                               .SelectMany(s => s))
                              .ToArray();
        }

        private readonly IReadOnlyCollection<byte> m_WavHeader = new byte[]
            { 0x52, 0x49, 0x46, 0x46, 0xA4, 0x7E, 0x20, 0x03, 0x57, 0x41,
              0x56, 0x45, 0x66, 0x6D, 0x74, 0x20, 0x10, 0x00, 0x00, 0x00,
              0x01, 0x00, 0x01, 0x00, 0x44, 0xAC, 0x00, 0x00, 0x88, 0x58,
              0x01, 0x00, 0x02, 0x00, 0x10, 0x00, 0x64, 0x61, 0x74, 0x61,
              0x80, 0x7E, 0x20, 0x03, 0x62, 0xA5, 0x62, 0xA5, 0x62, 0xA5,
              0x62, 0xA5, 0x62, 0xA5, 0x62, 0xA5, 0x62, 0xA5, 0x62, 0xA5,
              0x62, 0xA5, 0x62, 0xA5, 0x62, 0xA5, 0x62, 0xA5 };

        private static byte[] ToSamples(int sampleCount, bool low)
        {
            var value = low ? (short)-23198 : (short)23198;

            return Enumerable.Repeat(value, sampleCount)
                             .SelectMany(BitConverter.GetBytes)
                             .ToArray();
        }

        private static IEnumerable<int> ToSampleCountGaps(byte[] wavFileBytes)
        {
            bool IsLow(int i) => BitConverter.ToInt16(wavFileBytes, i) < 0;

            var wasLow = IsLow(44);

            var count = 1;

            for (var index = 46; index < wavFileBytes.Length; index += 2, ++count)
            {
                var nowLow = IsLow(index);

                if (wasLow != nowLow)
                {
                    yield return count;

                    count = 0;
                }
                
                wasLow = nowLow;
            }

            if (count > 1) yield return count - 1;
        }

        private static void VerifyCompatibility(byte[] wavFileBytes)
        {
            if (BitConverter.ToUInt32(wavFileBytes, 24) != 44_100)
            {
                throw new Exception(".wav file has unsupported sample rate");
            }

            if (BitConverter.ToUInt16(wavFileBytes, 32) != 2)
            {
                throw new Exception(".wav file has unsupported block alignment");
            }

            if (BitConverter.ToUInt16(wavFileBytes, 34) != 16)
            {
                throw new Exception(".wav file has unsupported bits per sample");
            }
        }
    }
}