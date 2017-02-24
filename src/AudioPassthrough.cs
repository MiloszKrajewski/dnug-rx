using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using NAudio.Wave;
using Sepura.Logging;
using Sepura.Utilities;

namespace Sepura.Dispatcher.Audio
{
    /// <summary>
    /// One direction audio passthrough.
    /// </summary>
    public class AudioPassthrough : IDisposable
    {
        private static readonly WaveFormat AudioFormat = new WaveFormat(44100, 2);
        private static readonly TimeSpan BufferSize = TimeSpan.FromMilliseconds(50);
        private const int NumberOfBuffers = 2;

        private readonly ILogFacade _log;

        private BehaviorSubject<bool> _shutdownSignal = new BehaviorSubject<bool>(false);

        IDisposable _passthrough;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioPassthrough" /> class.
        /// </summary>
        /// <param name="logFactory">The log factory.</param>
        /// <param name="passthroughName">Name of the passthrough (presentation).</param>
        /// <param name="monitorInterval">The interval for monitoring audio devices.</param>
        /// <param name="recreateDelay">The delay used to avoid race conditions when devices are plugged in.</param>
        /// <param name="inputDeviceName">Observable name of the input device (wildcards allowed).</param>
        /// <param name="outputDeviceName">Observable name of the output device (wildcards allowed).</param>
        /// <param name="volume">The volume observable. Use <c>Observable.Return(1.0f)</c> for constant value.</param>
        /// <param name="scheduler">The scheduler (optional).</param>
        public AudioPassthrough(
            ILogFactory logFactory,
            string passthroughName,
            TimeSpan monitorInterval, TimeSpan recreateDelay,
            IObservable<string> inputDeviceName,
            IObservable<string> outputDeviceName,
            IObservable<float> volume,
            IScheduler scheduler = null)
        {
            _log = logFactory.GetLogger(typeof(AudioPassthrough));

            var inputSelector = Observable
                .Interval(monitorInterval, scheduler ?? Scheduler.Default)
                .TakeUntil(_shutdownSignal)
                .CombineLatest(inputDeviceName.Select(name => WildcardMatch(name)), (_, matcher) => matcher)
                .Select(matcher => TryFindInputDeviceIndex(matcher))
                .DistinctUntilChanged();

            var outputSelector = Observable
                .Interval(monitorInterval, scheduler ?? Scheduler.Default)
                .TakeUntil(_shutdownSignal)
                .CombineLatest(outputDeviceName.Select(name => WildcardMatch(name)), (_, matcher) => matcher)
                .Select(matcher => TryFindOutputDeviceIndex(matcher))
                .DistinctUntilChanged();

            _passthrough = Disposable.Empty;
            inputSelector
                .CombineLatest(outputSelector, (i, o) => new { input = i, output = o })
                .TakeUntil(_shutdownSignal)
                .Throttle(recreateDelay)
                .Subscribe(p => RecreatePassthrough(passthroughName, p.input, p.output, volume, ref _passthrough));
        }

        /// <summary>
        /// Recreates the passthrough chain after device has been connected (or reconnected).
        /// </summary>
        /// <param name="passthroughName">Name of the passthrough.</param>
        /// <param name="input">The input device index.</param>
        /// <param name="output">The output device index.</param>
        /// <param name="volume">The volume observable.</param>
        /// <param name="disposable">The disposable.</param>
        private void RecreatePassthrough(
            string passthroughName,
            int? input, int? output,
            IObservable<float> volume,
            ref IDisposable disposable)
        {
            if (disposable != null) disposable.Dispose();
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
        private IDisposable CreatePassthrough(
            string passthroughName,
            int? input, int? output,
            IObservable<float> volume)
        {
            if (!input.HasValue)
            {
                _log.WarnFormat(
                    "Passthrough '{0}' has not been created as input device cannot be found",
                    passthroughName);
                return Disposable.Empty;
            }

            if (!output.HasValue)
            {
                _log.WarnFormat(
                    "Passthrough '{0}' has not been created as output device cannot be found",
                    passthroughName);
                return Disposable.Empty;
            }

            var disposables = DisposableBag.Create();

            try
            {
                // create wave in
                var waveIn = disposables.Add(new WaveInEvent
                {
                    DeviceNumber = input.Value,
                    BufferMilliseconds = (int)BufferSize.TotalMilliseconds,
                    NumberOfBuffers = NumberOfBuffers,
                    WaveFormat = AudioFormat
                });
                var waveInName = WaveInEvent.GetCapabilities(waveIn.DeviceNumber).ProductName;

                // create wave out
                var waveOut = disposables.Add(new WaveOutEvent
                {
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

                _log.InfoFormat("Passthrough '{0}' between '{1}' ({3}) and '{2}' ({4}) has been established",
                    passthroughName,
                    input, output,
                    waveInName, waveOutName);

                disposables.Add(
                    Disposable.Create(() => _log.WarnFormat("Passthrough '{0}' has been disposed", passthroughName)));

                return disposables;
            }
            catch (Exception e)
            {
                _log.ErrorFormat("Passthrough '{0}' initialization failed", passthroughName);
                _log.Error("Exception", e);
                disposables.Dispose();
                return Disposable.Empty;
            }
        }

        private int? TryFindDeviceIndex(int deviceCount, Func<int, string> deviceName, Func<string, bool> nameMatch)
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
                _log.Error("Failed to scan devices", e);
                return null;
            }
        }

        private int? TryFindInputDeviceIndex(Func<string, bool> nameMatch)
        {
            return TryFindDeviceIndex(
                WaveIn.DeviceCount,
                i => WaveIn.GetCapabilities(i).ProductName,
                nameMatch);
        }

        private int? TryFindOutputDeviceIndex(Func<string, bool> nameMatch)
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
            return new Func<string, bool>(text => regex.IsMatch(text));
        }

        #region IDisposable Support

        private bool _IsDisposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_IsDisposed)
            {
                if (disposing)
                {
                    _shutdownSignal.OnNext(true);
                    _shutdownSignal.OnCompleted();
                    _shutdownSignal.Dispose();
                    _passthrough?.Dispose();
                }

                _IsDisposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion IDisposable Support
    }
}