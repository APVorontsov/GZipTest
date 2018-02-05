using System;
using System.Diagnostics;
using System.IO;

namespace GZipTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var stopwatch = new Stopwatch();
            if (args == null || args.Length == 0)
            {
                //var source = @"E:\JOB\GZipTest\test files\test.txt";
                var source = @"e:\Video\Films\Dirk.Gently's.Holistic.Detective.Agency.S02.1080p.WEB-DL.Rus.Eng.sergiy_psp\Black Mirror - The Complete 4th Season - whip93\Black.Mirror.S04E01.USS.Callister.1080p.WEBRip.DD2.0.x264.-whip93.mkv";
                //var source = @"E:\JOB\GZipTest\test files\wallpaper.bmp";
                //var source = @"H:\test.txt";
                var dest = @"G:\test.txt.gz";
                dest = $"{source}.gz";

                stopwatch.Start();
                GzipCompressor.CompressFile(source, dest, SuccessCallback(stopwatch), ErrorCallback);
            }
            else
            {
                var sourceFile = args[1];
                var destinationFile = !string.IsNullOrEmpty(args[2]) ? args[2] : $"{sourceFile}.gz";
                if (string.IsNullOrEmpty(args[0]) && args[0].ToLowerInvariant() == "compress")
                {
                    if (!File.Exists(destinationFile) || AskOverrideFile(destinationFile))
                    {
                        GzipCompressor.CompressFile(sourceFile, destinationFile, SuccessCallback(stopwatch), ErrorCallback);
                    }
                }
                else
                {
                    if (!File.Exists(destinationFile) || AskOverrideFile(destinationFile))
                    {
                        GzipCompressor.CompressFile(sourceFile, destinationFile, SuccessCallback(stopwatch), ErrorCallback);
                    }
                }
            }
        }
        private static bool AskOverrideFile(string destinationFile)
        {
            do
            {
                Console.WriteLine($"File {destinationFile} is already exist. Do you want to override it? (y/n)");
                switch (Console.ReadLine()?.ToLowerInvariant())
                {
                    case "y":
                        return true;
                    case "n":
                        return false;
                }
            } while (true);
        }

        private static EventHandler<SuccessEventArgs> SuccessCallback(Stopwatch stopwatch)
        {
            return (sender, e) =>
            {
                stopwatch.Stop();
                Console.WriteLine("File compressed. Time elapsed: " + stopwatch.Elapsed);
                Console.WriteLine("Press the enter key to close the program");
                Console.ReadLine();
            };
        }

        private static void ErrorCallback(object sender, ErrorEventArgs e)
        {
            Console.WriteLine($"{e.Message}");
            Console.WriteLine("Press the enter key to close the program");
            Console.ReadLine();
        }
    }
}