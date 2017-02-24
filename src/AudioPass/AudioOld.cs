using NAudio.Wave;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;

namespace AudioPass
{
    public class AudioOld
    {
        private static readonly WaveFormat AudioFormat = new WaveFormat(44100, 2);
        private static readonly TimeSpan BufferSize = TimeSpan.FromMilliseconds(50);
        private const int NumberOfBuffers = 2;

        private static readonly ILogger _log = LogManager.GetLogger(nameof(AudioOld));

        public static IObservable<byte[]> Record(IObservable<string> deviceName)
        {
            var samples = new Subject<byte[]>();
            var deviceMatch = deviceName.DistinctUntilChanged().Select(WildcardMatch);
            var session = Disposable.Empty;
            Observable
                .Interval(TimeSpan.FromSeconds(1))
                .CombineLatest(deviceMatch, (_, m) => m)
                .Select(TryFindInputDeviceIndex)
                .DistinctUntilChanged()
                .Subscribe(i => session = RecreateWaveIn(session, i, samples));
            return samples;
        }

        public static void Play(IObservable<string> deviceName, IObservable<byte[]> samples)
        {
            var deviceMatch = deviceName.DistinctUntilChanged().Select(WildcardMatch);
            var session = Disposable.Empty;
            Observable
                .Interval(TimeSpan.FromSeconds(1))
                .CombineLatest(deviceMatch, (_, m) => m)
                .Select(TryFindOutputDeviceIndex)
                .DistinctUntilChanged()
                .Subscribe(i => session = RecreateWaveOut(session, i, samples));
        }

        private static IDisposable RecreateWaveOut(
            IDisposable session, int? deviceIndex, IObservable<byte[]> samples)
        {
            session.Dispose();
            if (!deviceIndex.HasValue)
                return Disposable.Empty;

            var waveOut = new WaveOutEvent {
                DeviceNumber = deviceIndex.Value,
                NumberOfBuffers = NumberOfBuffers
            };

            var provider = new BufferedWaveProvider(AudioFormat);
            var subscription = samples.Subscribe(s => provider.AddSamples(s, 0, s.Length));

            waveOut.Init(provider);
            waveOut.Play();

            return DisposableBag.Create(subscription, waveOut);
        }

        private static IDisposable RecreateWaveIn(
            IDisposable session, int? deviceIndex, IObserver<byte[]> output)
        {
            session.Dispose();
            if (!deviceIndex.HasValue)
                return Disposable.Empty;

            var waveIn = new WaveInEvent {
                DeviceNumber = deviceIndex.Value,
                BufferMilliseconds = (int)BufferSize.TotalMilliseconds,
                NumberOfBuffers = NumberOfBuffers,
                WaveFormat = AudioFormat
            };

            waveIn.DataAvailable += (s, e) => {
                var buffer = new byte[e.BytesRecorded];
                Array.Copy(e.Buffer, buffer, buffer.Length);
                output.OnNext(buffer);
            };

            waveIn.StartRecording();

            return waveIn;
        }

        private static IEnumerable<string> EnumerateDeviceNames(int deviceCount, Func<int, string> deviceName)
        {
            try
            {
                return Enumerable
                    .Range(0, deviceCount)
                    .Select(deviceName)
                    .ToArray();

            }
            catch (Exception e)
            {
                _log.Error(e, "Failed to scan devices");
                return null;
            }
        }

        public static IEnumerable<string> EnumerateInputDeviceNames()
        {
            return EnumerateDeviceNames(
                WaveIn.DeviceCount, 
                i => WaveIn.GetCapabilities(i).ProductName);
        }

        public static IEnumerable<string> EnumerateOutputDeviceNames()
        {
            return EnumerateDeviceNames(
                WaveOut.DeviceCount, 
                i => WaveOut.GetCapabilities(i).ProductName);
        }

        private static int? TryFindDeviceIndex(int deviceCount, Func<int, string> deviceName, Func<string, bool> nameMatch)
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
                _log.Error(e, "Failed to scan devices");
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

    }
}
