using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Demo1.TenK
{
    class Program
    {
        public static void Measure(string name, Action action)
        {
            var stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;
            Console.WriteLine("{0}: {1:0.0ms}", name, elapsed);
        }

        public static double Spin()
        {
            return Enumerable.Range(1, 10000000).Select(i => Math.Sin(i)).Sum();
        }

        static void Main(string[] args)
        {
            const int limit100K = 10000;
            Action none = () => { };
            Action wait = () => Thread.Sleep(1000);
            Action spin = () => Spin();
            Func<Task> waitAsync = async () => await Task.Delay(1000);

            //ConcurrentExecutor.Wrap(
            //	"100k wait threads", limit100K,
            //	e => new Thread(() => e.Execute(wait)).Start());

            ConcurrentExecutor.Wrap(
                "100k threads", limit100K,
                e => new Thread(() => e.Execute(none)).Start());

            ConcurrentExecutor.Wrap(
                "100k tasks", limit100K,
                e => Task.Run(() => e.Execute(none)));

            Console.WriteLine(new string('-', 79));

            ConcurrentExecutor.Wrap(
                "100 spin threads", 100,
                e => new Thread(() => e.Execute(spin)).Start());

            ConcurrentExecutor.Wrap(
                "100 spin tasks", 100,
                e => Task.Run(() => e.Execute(spin)));

            Console.WriteLine(new string('-', 79));

            ConcurrentExecutor.Wrap(
                "100 wait threads", 100,
                e => new Thread(() => e.Execute(wait)).Start());

            ConcurrentExecutor.Wrap(
                "100 wait tasks", 100,
                e => Task.Run(() => e.Execute(wait)));

            ConcurrentExecutor.Wrap(
                "100 async wait tasks", 100,
                e => e.ExecuteAsync(waitAsync));

            Console.WriteLine(new string('-', 79));

            Console.WriteLine("Press <enter>...");
            Console.ReadLine();
        }
    }
}
