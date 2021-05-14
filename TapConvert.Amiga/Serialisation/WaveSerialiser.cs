
using System;
using System.Collections.Generic;
using System.Linq;

namespace TapeTools.TapConvert.Amiga.Serialisation
{
    internal sealed class WaveSerialiser
    {
        public IReadOnlyCollection<TimeSpan> ToPulseGaps(byte[] wavFileBytes)
        {
            if (wavFileBytes is null)
            {
                throw new ArgumentNullException(nameof(wavFileBytes));
            }

            VerifyCompatibility(wavFileBytes);

            return ToSampleCountGaps(wavFileBytes)
                  .Select(c => c * 226.7573696145125)
                  .Select(t => Math.Round(t))
                  .Select(Convert.ToInt64)
                  .Select(TimeSpan.FromTicks)
                  .ToArray();
        }

        private static IEnumerable<int> ToSampleCountGaps(byte[] wavFileBytes)
        {
            var isLow = BitConverter.ToInt16(wavFileBytes, 44) < 0;

            var count = 1;

            for (var index = 46; index < wavFileBytes.Length; index += 2, ++count)
            {
                var nowLow = BitConverter.ToInt16(wavFileBytes, index) < 0;

                if (nowLow == isLow) continue;

                yield return count;

                count = 0;
                
                isLow = nowLow;
            }
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