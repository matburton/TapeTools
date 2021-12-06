
using System;
using System.IO;
using System.Linq;

using TapeTools.TzxConvert.Amiga.Serialisation;

using static System.Environment;

namespace TapeTools.TzxConvert.Amiga
{
    public static class Program
    {
        public static int Main(string[] arguments)
        {
            try
            {
                if (arguments.Length < 1)
                {
                    Console.WriteLine("\nArguments are '.tzx' file paths");
                    Console.WriteLine("'.tzx.amiga' files will be written to"
                                      + " current directory with same file names");
                }

                VerifyAssumptions();

                foreach (var filePath in arguments)
                {
                    ConvertForAmiga(filePath);
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

        private static void ConvertForAmiga(string filePath)
        {
            var filename = Path.GetFileName(filePath);

            Console.WriteLine($"\nConverting '{filename}'...");

            var fileBytes = File.ReadAllBytes(filePath);

            var tzxSerialiser = new TzxSerialiser();

            var pulseGaps = tzxSerialiser.ToPulseGaps(fileBytes,
                                                      out var startLow);
            if (!pulseGaps.Any())
            {
                throw new Exception("Source contained no transitions");
            }

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
            Console.WriteLine($"Original file bytes:  {fileBytes.Length:n0}");
            Console.WriteLine($"Converted file bytes: {amigaFileBytes.Length:n0}");

            File.WriteAllBytes($"{filename}.amiga", amigaFileBytes);

            Console.WriteLine($"Saved '{filename}.amiga'");

            if (GetEnvironmentVariable("TAPE_DEBUG_WAV") is not null)
            {
                var waveSerialiser = new WavSerialiser();

                var wavFileBytes = waveSerialiser.ToBytes(pulseGaps, startLow);

                File.WriteAllBytes($"{filename}.debug.wav", wavFileBytes);

                Console.WriteLine($"Saved '{filename}.debug.wav'");
            }
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