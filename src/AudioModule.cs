using NAudio.Wave;
using Prism.Mef.Modularity;
using Prism.Modularity;
using Sepura.Dispatcher.Common;
using Sepura.Dispatcher.Common.Interface;
using Sepura.Logging;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Sepura.Dispatcher.Audio
{
    /// <summary>
    /// Module initialisation class for the Sepura.Dispatcher.Audio assembly.
    /// </summary>
    /// <seealso cref="Prism.Modularity.IModule" />
    [ModuleExport(typeof(AudioModule))]
    public class AudioModule : IModule
    {
        private static readonly TimeSpan DeviceMonitorInterval = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan RecreatePassthroughDelay = TimeSpan.FromSeconds(1);

        private readonly ILogFactory _logFactory;
        private readonly ILogFacade _log;
        private readonly BehaviorSubject<float> _speakerVolume;
        private readonly BehaviorSubject<float> _microphoneVolume;
        private readonly ISettings _Settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioModule"/> class.
        /// </summary>
        /// <param name="logFactory">The log factory.</param>
        [ImportingConstructor]
        public AudioModule(ILogFactory logFactory, ISettings theSettings)
        {
            _logFactory = logFactory;
            _log = logFactory.GetLogger(typeof(AudioModule));
            _speakerVolume = new BehaviorSubject<float>(1.0f);
            _microphoneVolume = new BehaviorSubject<float>(1.0f);
            _Settings = theSettings;
        }

        /// <summary>
        /// Notifies the module that it has be initialized.
        /// </summary>
        public void Initialize()
        {
            // Log known audio devices, just for convenience, has no impact on functionality
            // will be extended when users are allowed to select device from UI
            LogAudioDevices("input", WaveIn.DeviceCount, i => WaveIn.GetCapabilities(i).ProductName);
            LogAudioDevices("output", WaveOut.DeviceCount, i => WaveOut.GetCapabilities(i).ProductName);

            _Settings.SettingsLoadedObservable.Where(loaded => loaded)
                .Subscribe((a) => LoadSettings());
        }

        private void LoadSettings()
        {
            _log.Info("Loading settings");

            var mobileToHeadsetSettings = CreatePassthrough("MobileToHeadset", SettingType.AudioMobileInput, SettingType.AudioHeadsetOutput, _speakerVolume);
            var headsetToMobileSettings = CreatePassthrough("HeadsetToMobile", SettingType.AudioHeadsetInput, SettingType.AudioMobileOutput, _microphoneVolume);
        }

        private AudioPassthrough CreatePassthrough(string name, SettingType inputSetting, SettingType outputSetting, IObservable<float> volume)
        {
            _log.InfoFormat("New passthrough. Name:{0} Input:{1} Output:{2}", name, inputSetting, outputSetting);

            return new AudioPassthrough(
                _logFactory, name, DeviceMonitorInterval, RecreatePassthroughDelay,
                _Settings.GetStringSettingObservable(inputSetting), _Settings.GetStringSettingObservable(outputSetting), volume);
        }

        private void LogAudioDevices(
            string category, int deviceCount, Func<int, string> deviceName)
        {
            try
            {
                Enumerable.Range(0, deviceCount).Select(i => deviceName(i))
                    .ForEach(n => _log.DebugFormat("Found {0} audio device: {1}", category, n));
            }
            catch (Exception e)
            {
                _log.WarnFormat("Failed to enumerate {0} audio devices", category);
                _log.Error(e);
            }
        }
    }
}