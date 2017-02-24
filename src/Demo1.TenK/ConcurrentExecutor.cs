using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Demo1.TenK
{
    public class ConcurrentExecutor
    {
        private int _iterationsDone;
        private int _iterationsLeft;
        private int _concurrent;
        private int _maximumConcurrent;
        private readonly ManualResetEvent _doneSignal;
        private Stopwatch _stopwatch;

        private ConcurrentExecutor(int iterationsLeft)
        {
            _iterationsLeft = iterationsLeft;
            _doneSignal = new ManualResetEvent(false);
            _stopwatch = Stopwatch.StartNew();
        }

        public int MaximumConcurrent { get { return _maximumConcurrent; } }
        public TimeSpan Elapsed { get { return _stopwatch.Elapsed; } }
        public WaitHandle Done { get { return _doneSignal; } }

        public static void Wrap(string name, int limit, Action<ConcurrentExecutor> action)
        {
            var done = new ManualResetEvent(false);
            var executor = new ConcurrentExecutor(limit);
            for (int i = 0; i < limit; i++)
                action(executor);
            executor.Done.WaitOne();
            Console.WriteLine(
                "{0}: elapsed: {1:0.0ms}, parallel: {2}", 
                name, 
                executor.Elapsed.TotalMilliseconds, 
                executor.MaximumConcurrent);
        }

        public void Execute(Action action)
        {
            var concurrent = Interlocked.Increment(ref _concurrent);
            UpdateMaximum(concurrent);

            action();

            Interlocked.Decrement(ref _concurrent);

            Interlocked.Increment(ref _iterationsDone);
            if (Interlocked.Decrement(ref _iterationsLeft) == 0)
                _doneSignal.Set();
        }

        public async void ExecuteAsync(Func<Task> action)
        {
            var concurrent = Interlocked.Increment(ref _concurrent);
            UpdateMaximum(concurrent);

            await action();

            Interlocked.Decrement(ref _concurrent);

            Interlocked.Increment(ref _iterationsDone);
            if (Interlocked.Decrement(ref _iterationsLeft) == 0)
                _doneSignal.Set();
        }


        private void UpdateMaximum(int concurrent)
        {
            while (true)
            {
                var maximumConcurrent = Interlocked.CompareExchange(ref _maximumConcurrent, 0, 0);
                if (concurrent <= maximumConcurrent)
                    break;
                if (Interlocked.CompareExchange(ref _maximumConcurrent, concurrent, maximumConcurrent) == maximumConcurrent)
                    break;
            }
        }
    }
}