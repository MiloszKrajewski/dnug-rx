using NAudio.Wave;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace AudioPass
{
    public class Audio
    {
        private static readonly WaveFormat AudioFormat = new WaveFormat(44100, 2);
        private static readonly TimeSpan BufferSize = TimeSpan.FromMilliseconds(50);
        private const int NumberOfBuffers = 2;

        private static readonly ILogger Log = LogManager.GetLogger(nameof(Audio));

        private static IObservable<byte[]> ObserveWaveIn(int? deviceIndex)
        {
            return Observable.Create<byte[]>(output => {
                if (!deviceIndex.HasValue)
                {
                    Log.Warn("No input device found");
                    return Disposable.Empty;
                }

                var deviceName = WaveIn.GetCapabilities(deviceIndex.Value).ProductName;

                Log.Info("Connecting to input device: {0}", deviceName);

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

                return Disposable.Create(() => {
                    waveIn.Dispose();
                    Log.Warn("Disconnected from input device: {0}", deviceName);
                });
            });
        }

        private static IObservable<Unit> SendToWaveOut(int? deviceIndex, IObservable<byte[]> samples)
        {
            return Observable.Create<Unit>(output => {
                if (!deviceIndex.HasValue)
                {
                    Log.Warn("No output device found");
                    output.OnCompleted();
                    return Disposable.Empty;
                }

                var deviceName = WaveOut.GetCapabilities(deviceIndex.Value).ProductName;
                Log.Info("Connecting to output device: {0}", deviceName);

                var waveOut = new WaveOutEvent {
                    DeviceNumber = deviceIndex.Value,
                    NumberOfBuffers = NumberOfBuffers
                };

                var provider = new BufferedWaveProvider(AudioFormat);
                var subscription = samples.Subscribe(s => provider.AddSamples(s, 0, s.Length));

                waveOut.Init(provider);
                waveOut.Play();

                return Disposable.Create(() => {
                    subscription.Dispose();
                    waveOut.Dispose();
                    output.OnCompleted();
                    Log.Warn("Disconnected from output device: {0}", deviceName);
                });
            });
        }

        public static IObservable<byte[]> Record(string deviceName)
        {
            var deviceMatch = WildcardMatch(deviceName);
            return Observable
                .Interval(TimeSpan.FromSeconds(1)).Merge(Observable.Return(0L))
                .Select(_ => TryFindInputDeviceIndex(deviceMatch))
                .DistinctUntilChanged()
                .Select(ObserveWaveIn)
                .Switch()
                .Publish().RefCount();
        }

        public static IDisposable Play(
            string deviceName, IObservable<byte[]> samples)
        {
            var endOfSamples = samples.Select(_ => true).LastOrDefaultAsync();
            var deviceMatch = WildcardMatch(deviceName);
            var subscription = Observable
                .Interval(TimeSpan.FromSeconds(1)).Merge(Observable.Return(0L))
                .Select(_ => TryFindOutputDeviceIndex(deviceMatch))
                .DistinctUntilChanged()
                .Select(i => SendToWaveOut(i, samples).TakeUntil(endOfSamples))
                .Switch()
                .Publish();
            return subscription.Connect();
        }

        private static IEnumerable<string> EnumerateDeviceNames(
            int deviceCount, Func<int, string> deviceName)
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
                Log.Error(e, "Failed to scan devices");
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
    }
}
