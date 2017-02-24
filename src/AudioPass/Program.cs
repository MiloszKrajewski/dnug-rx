using System;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.IO;
using System.Reactive.Linq;
using System.Linq;

namespace AudioPass
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfigureLogging(Path.GetFullPath("."), "AudioPass");
            Audio.EnumerateInputDeviceNames().ForEach(n => Console.WriteLine($"in: {n}"));
            Audio.EnumerateOutputDeviceNames().ForEach(n => Console.WriteLine($"out: {n}"));
            Console.ReadLine();

            var inputName = "Desktop*Microphone*";
            var outputName = "Speakers*Realtek*";

            var samples = Audio.Record(inputName);
            Audio.Play(outputName, samples);

            Console.ReadLine();
        }

        private static void ConfigureLogging(string folder, string productName)
        {
            var config = new LoggingConfiguration();
            ConfigureConsoleLogging(config);
            ConfigureFileLogging(config, folder, productName);
            LogManager.Configuration = config;
        }

        private static void ConfigureFileLogging(
            LoggingConfiguration config, string folder, string productName)
        {
            Directory.CreateDirectory(folder);

            var target = new FileTarget() {
                Name = "file",
                Layout = "${date:format=yyyyMMdd.HHmmss} ${threadid}> [${level}] (${logger}) ${message}",
                ArchiveEvery = FileArchivePeriod.Day,
                MaxArchiveFiles = 7,
                FileName = Path.Combine(folder, productName + ".log"),
                ArchiveFileName = Path.Combine(folder, productName + ".bak"),
            };
            config.AddTarget("file", target);
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, target));
        }

        private static void ConfigureConsoleLogging(LoggingConfiguration config)
        {
            var console = new ColoredConsoleTarget {
                Name = "console",
                Layout = "${message}",
                UseDefaultRowHighlightingRules = true,
                ErrorStream = false,
            };
            console.RowHighlightingRules.Add(
                new ConsoleRowHighlightingRule("level == LogLevel.Trace", ConsoleOutputColor.DarkGray, ConsoleOutputColor.NoChange));
            console.RowHighlightingRules.Add(
                new ConsoleRowHighlightingRule("level == LogLevel.Debug", ConsoleOutputColor.Gray, ConsoleOutputColor.NoChange));
            console.RowHighlightingRules.Add(
                new ConsoleRowHighlightingRule("level == LogLevel.Info", ConsoleOutputColor.Cyan, ConsoleOutputColor.NoChange));
            console.RowHighlightingRules.Add(
                new ConsoleRowHighlightingRule("level == LogLevel.Warn", ConsoleOutputColor.Yellow, ConsoleOutputColor.NoChange));
            console.RowHighlightingRules.Add(
                new ConsoleRowHighlightingRule("level == LogLevel.Error", ConsoleOutputColor.Red, ConsoleOutputColor.NoChange));
            console.RowHighlightingRules.Add(
                new ConsoleRowHighlightingRule("level == LogLevel.Fatal", ConsoleOutputColor.Magenta, ConsoleOutputColor.NoChange));
            config.AddTarget("console", console);

            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, console));
        }
    }
}
