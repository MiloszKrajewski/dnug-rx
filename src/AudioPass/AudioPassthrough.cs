using NAudio.Wave;
using NLog;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;

namespace AudioPass
{
    /// <summary>
    /// One direction audio passthrough.
    /// </summary>
    public class AudioPassthrough : IDisposable
    {
        private static readonly WaveFormat AudioFormat = new WaveFormat(44100, 2);
        private static readonly TimeSpan BufferSize = TimeSpan.FromMilliseconds(50);
        private const int NumberOfBuffers = 2;
        private static readonly Logger Log = LogManager.GetLogger(nameof(AudioPassthrough));

        private readonly BehaviorSubject<bool> _shutdownSignal = new BehaviorSubject<bool>(false);

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioPassthrough" /> class.
        /// </summary>
        /// <param name="passthroughName">Name of the passthrough (presentation).</param>
        /// <param name="monitorInterval">The interval for monitoring audio devices.</param>
        /// <param name="recreateDelay">The delay used to avoid race conditions when devices are plugged in.</param>
        /// <param name="inputDeviceName">Observable name of the input device (wildcards allowed).</param>
        /// <param name="outputDeviceName">Observable name of the output device (wildcards allowed).</param>
        /// <param name="volume">The volume observable. Use <c>Observable.Return(1.0f)</c> for constant value.</param>
        /// <param name="scheduler">The scheduler (optional).</param>
        [SuppressMessage("ReSharper", "ImplicitlyCapturedClosure")]
        public AudioPassthrough(
            string passthroughName,
            TimeSpan monitorInterval, TimeSpan recreateDelay,
            IObservable<string> inputDeviceName,
            IObservable<string> outputDeviceName,
            IObservable<float> volume,
            IScheduler scheduler = null)
        {
            var inputSelector = Observable
                .Interval(monitorInterval, scheduler ?? Scheduler.Default)
                .TakeUntil(_shutdownSignal)
                .CombineLatest(inputDeviceName.Select(WildcardMatch), (_, matcher) => matcher)
                .Select(TryFindInputDeviceIndex)
                .DistinctUntilChanged();

            var outputSelector = Observable
                .Interval(monitorInterval, scheduler ?? Scheduler.Default)
                .TakeUntil(_shutdownSignal)
                .CombineLatest(outputDeviceName.Select(WildcardMatch), (_, matcher) => matcher)
                .Select(TryFindOutputDeviceIndex)
                .DistinctUntilChanged();

            var passthrough = Disposable.Empty;
            inputSelector
                .CombineLatest(outputSelector, (i, o) => new { input = i, output = o })
                .TakeUntil(_shutdownSignal)
                .Throttle(recreateDelay)
                .Subscribe(
                    p => RecreatePassthrough(passthroughName, p.input, p.output, volume, ref passthrough),
                    () => passthrough.Dispose());
        }

        /// <summary>
        /// Recreates the passthrough chain after device has been connected (or reconnected).
        /// </summary>
        /// <param name="passthroughName">Name of the passthrough.</param>
        /// <param name="input">The input device index.</param>
        /// <param name="output">The output device index.</param>
        /// <param name="volume">The volume observable.</param>
        /// <param name="disposable">The disposable.</param>
        private static void RecreatePassthrough(
            string passthroughName,
            int? input, int? output,
            IObservable<float> volume,
            ref IDisposable disposable)
        {
            disposable?.Dispose();
            disposable = CreatePassthrough(passthroughName, input, output, volume);
        }

        /// <summary>
        /// Creates the passthrough.
        /// </summary>
        /// <param name="passthroughName">Name of the passthrough.</param>
        /// <param name="input">The input device index.</param>
        /// <param name="output">The output device index.</param>
        /// <param name="volume">The volume observable.</param>
        /// <returns>Disposable to dispose passthrough chain.</returns>
        private static IDisposable CreatePassthrough(
            string passthroughName,
            int? input, int? output,
            IObservable<float> volume)
        {
            if (!input.HasValue)
            {
                Log.Warn(
                    "Passthrough '{0}' has not been created as input device cannot be found",
                    passthroughName);
                return Disposable.Empty;
            }

            if (!output.HasValue)
            {
                Log.Warn(
                    "Passthrough '{0}' has not been created as output device cannot be found",
                    passthroughName);
                return Disposable.Empty;
            }

            var disposables = DisposableBag.Create();

            try
            {
                // create wave in
                var waveIn = disposables.Add(new WaveInEvent {
                    DeviceNumber = input.Value,
                    BufferMilliseconds = (int)BufferSize.TotalMilliseconds,
                    NumberOfBuffers = NumberOfBuffers,
                    WaveFormat = AudioFormat
                });
                var waveInName = WaveInEvent.GetCapabilities(waveIn.DeviceNumber).ProductName;

                // create wave out
                var waveOut = disposables.Add(new WaveOutEvent {
                    DeviceNumber = output.Value,
                    NumberOfBuffers = NumberOfBuffers,
                });
                var waveOutName = WaveOut.GetCapabilities(waveOut.DeviceNumber).ProductName;

                // create volume control stream (between in and out)
                var volumeStream = new VolumeWaveProvider16(new WaveInProvider(waveIn));
                disposables.Add(volume.Subscribe(v => volumeStream.Volume = v));

                // bind volume stream to output
                waveOut.Init(volumeStream);

                // start passing sound through
                waveIn.StartRecording();
                waveOut.Play();

                Log.Info("Passthrough '{0}' between '{1}' ({3}) and '{2}' ({4}) has been established",
                    passthroughName,
                    input, output,
                    waveInName, waveOutName);

                disposables.Add(
                    Disposable.Create(() => Log.Warn("Passthrough '{0}' has been disposed", passthroughName)));

                return disposables;
            }
            catch (Exception e)
            {
                Log.Error("Passthrough '{0}' initialization failed", passthroughName);
                Log.Error(e, "Exception");
                disposables.Dispose();
                return Disposable.Empty;
            }
        }

        private static int? TryFindDeviceIndex(
            int deviceCount, Func<int, string> deviceName, Func<string, bool> nameMatch)
        {
            try
            {
                return Enumerable
                    .Range(0, deviceCount)
                    .Where(i => nameMatch(deviceName(i)))
                    .Select(i => (int?)i)
                    .FirstOrDefault();
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to scan devices");
                return null;
            }
        }

        private static int? TryFindInputDeviceIndex(Func<string, bool> nameMatch)
        {
            return TryFindDeviceIndex(
                WaveIn.DeviceCount,
                i => WaveIn.GetCapabilities(i).ProductName,
                nameMatch);
        }

        private static int? TryFindOutputDeviceIndex(Func<string, bool> nameMatch)
        {
            return TryFindDeviceIndex(
                WaveOut.DeviceCount,
                i => WaveOut.GetCapabilities(i).ProductName,
                nameMatch);
        }

        private static Func<string, bool> WildcardMatch(string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) return _ => false;
            pattern = string.Format("^{0}$", Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".?"));
            var regex = new Regex(pattern);
            return text => regex.IsMatch(text);
        }

        public void Dispose()
        {
            _shutdownSignal.OnNext(true);
            _shutdownSignal.OnCompleted();
        }
    }
}