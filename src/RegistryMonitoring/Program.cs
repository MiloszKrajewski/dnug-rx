using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace RegistryMonitoring
{
    class Program
    {
        static void Main(string[] args)
        {
            //Task.Factory.StartNew(
            //	() => ProcessWithLogging1(GetLoggingEnabled()),
            //	TaskCreationOptions.LongRunning);

            //Task.Factory.StartNew(
            //	() => ProcessWithLogging2(GetLoggingEnabled), 
            //	TaskCreationOptions.LongRunning);

            var loggingEnabled =
                Observable.Return(GetLoggingEnabled())
                .Merge(Observable.Interval(TimeSpan.FromSeconds(1)).Select(_ => GetLoggingEnabled()));
            Task.Factory.StartNew(
                () => ProcessWithLogging3(loggingEnabled),
                TaskCreationOptions.LongRunning);

            Console.ReadLine();
        }

        public static bool GetLoggingEnabled()
        {
            var key = Registry.CurrentUser.CreateSubKey(@"Software\\Test");
            bool value = false;
            if (key != null)
                value = Convert.ToInt32(key.GetValue("logging", 0)) != 0;
            Console.WriteLine("Logging: {0}", value);
            return value;
        }

        public static void ProcessWithLogging1(bool loggingEnabled)
        {
            foreach (var text in RandomTextStream())
            {
                if (loggingEnabled)
                    Console.WriteLine(text);
            }
        }

        public static void ProcessWithLogging2(Func<bool> loggingEnabled)
        {
            foreach (var text in RandomTextStream())
            {
                if (loggingEnabled())
                    Console.WriteLine(text);
            }
        }

        public static void ProcessWithLogging3(IObservable<bool> loggingEnabled)
        {
            var capture = new BehaviorSubject<bool>(false);
            loggingEnabled.Subscribe(capture);

            foreach (var text in RandomTextStream())
            {
                if (capture.Value)
                    Console.WriteLine(text);
            }
        }

        private static IEnumerable<string> RandomTextStream()
        {
            var random = new Random();
            while (true)
            {
                var buffer = new byte[20 + random.Next(40)];
                random.NextBytes(buffer);
                yield return Convert.ToBase64String(buffer);
            }
        }
    }
}
