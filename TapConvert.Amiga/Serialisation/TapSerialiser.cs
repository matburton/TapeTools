
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TapeTools.TapConvert.Amiga.Serialisation
{
    internal sealed class TapSerialiser
    {
        public IReadOnlyCollection<TimeSpan> ToPulseGaps(byte[] tapFileBytes)
        {
            if (tapFileBytes is null)
            {
                throw new ArgumentNullException(nameof(tapFileBytes));
            }

            VerifyCompatability(tapFileBytes);

            var gapsInCycles = GetPulseGapsInCycles(tapFileBytes).ToArray();

            var totalCycles = gapsInCycles.Select(Convert.ToUInt64)
                                          .Aggregate((c, a) => c + a);

            Console.WriteLine($"Total C64 cycles:     {totalCycles:n0}");

            return gapsInCycles.Select(ToTimeGap).ToArray();
        }

        private static IEnumerable<uint> GetPulseGapsInCycles
            (byte[] payloadBytes)
        {
            var enumerator = payloadBytes.GetEnumerator();

            for (var index = 20; index < payloadBytes.Length; ++index)
            {
                if (payloadBytes[index] == 0)
                {
                    if (index + 3 >= payloadBytes.Length)
                    {
                        throw new Exception("Byte sequence ended early");
                    }

                    yield return BitConverter.ToUInt32
                        (new byte[] { payloadBytes[++index],
                                      payloadBytes[++index],
                                      payloadBytes[++index],
                                      0 });
                }
                else
                {
                    yield return payloadBytes[index] * (uint)8;
                }
            }
        }

        private static TimeSpan ToTimeGap(uint cycles)
        {
            var ticks = Math.Round(cycles * 10.14972879924648);

            return TimeSpan.FromTicks(Convert.ToInt64(ticks));
        }

        private static int VerifyCompatability(byte[] tapFileBytes)
        {
            if (tapFileBytes.Length < 20)
            {
                throw new Exception(".tap file too short");
            }

            var signature = Encoding.ASCII.GetString(tapFileBytes, 0, 12);

            if (signature != "C64-TAPE-RAW")
            {
                throw new Exception($"Unsupported signature: {signature}");
            }

            var version = (int)tapFileBytes[12];

            if (tapFileBytes[12] != 1)
            {
                throw new Exception($"Unsupported version: {version}");
            }

            if (BitConverter.ToUInt32(tapFileBytes, 16) != tapFileBytes.Length - 20)
            {
                throw new Exception($"File length mis-match");
            }

            return version;
        }
    }
}