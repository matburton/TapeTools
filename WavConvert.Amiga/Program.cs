
using System;
using System.IO;
using System.Linq;

using TapeTools.WavConvert.Amiga.Serialisation;

using static System.Environment;

namespace TapeTools.WavConvert.Amiga
{
    public static class Program
    {
        public static int Main(string[] arguments)
        {
            try
            {
                if (arguments.Length < 1)
                {
                    Console.WriteLine("\nArguments are '.wav' file paths");
                    Console.WriteLine("'.wav.amiga' files will be written to"
                                      + " current directory with same file names");
                }

                VerifyAssumptions();

                foreach (var tapFilePath in arguments)
                {
                    ConvertTapForAmiga(tapFilePath);
                }

                Console.WriteLine("\nDone");

                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Error: {exception}");

                return 1;
            }
        }

        private static void ConvertTapForAmiga(string tapFilePath)
        {
            var tapFilename = Path.GetFileName(tapFilePath);

            Console.WriteLine($"\nConverting '{tapFilename}'...");

            var tapFileBytes = File.ReadAllBytes(tapFilePath);

            var wavSerialiser = new WavSerialiser();

            var pulseGaps = wavSerialiser.ToPulseGaps(tapFileBytes,
                                                      out var startLow);

            if (   GetEnvironmentVariable("TAPE_PULSE_GAP_MULTIPLIER") is {} s
                && double.TryParse(s, out var pulseGapMultiplier))
            {
                Console.WriteLine($"Pulse gap multiplier: {pulseGapMultiplier}\n");

                pulseGaps = pulseGaps.Select(g => g.Ticks * pulseGapMultiplier)
                                     .Select(t => Math.Round(t))
                                     .Select(t => new TimeSpan((long)t))
                                     .ToArray();
            }

            var amigaFileBytes = new AmigaSerialiser().ToBytes(pulseGaps,
                                                               startLow);

            var totalRunTime = pulseGaps.Aggregate((g, a) => g + a);

            var maxFrequency = TimeSpan.TicksPerSecond
                             / pulseGaps.OrderBy(o => o).First().Ticks;

            Console.WriteLine($"Total pulse gaps:     {pulseGaps.Count:n0}");
            Console.WriteLine($"Non-stop run time:    {totalRunTime}");
            Console.WriteLine($"Maximum frequency:    {maxFrequency:n0} Hz");
            Console.WriteLine($"Original file bytes:  {tapFileBytes.Length:n0}");
            Console.WriteLine($"Converted file bytes: {amigaFileBytes.Length:n0}");

            File.WriteAllBytes($"{tapFilename}.amiga", amigaFileBytes);

            Console.WriteLine($"Saved '{tapFilename}.amiga'");
        }

        private static void VerifyAssumptions()
        {
            if (TimeSpan.TicksPerSecond != 10_000_000)
            {
                throw new PlatformNotSupportedException
                    ("Current platform has different ticks timing");
            }

            if (!BitConverter.IsLittleEndian)
            {
                throw new PlatformNotSupportedException
                    ("Current platform is not little endian");
            }
        }
    }
}