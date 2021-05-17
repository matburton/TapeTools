
using System;
using System.IO;
using System.Linq;

using TapeTools.WavConvert.Amiga.Serialisation;

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
                    Console.WriteLine("\nArguments are '.tap' file paths");
                    Console.WriteLine("'.amiga.bin' files will be written to"
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

            var amigaFileBytes = new AmigaSerialiser().ToBytes(pulseGaps,
                                                               startLow);

            var totalRunTime = pulseGaps.Aggregate((g, a) => g + a);

            Console.WriteLine($"Total pulse gaps:     {pulseGaps.Count:n0}");
            Console.WriteLine($"Non-stop run time:    {totalRunTime}");
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