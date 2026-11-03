using System;
using System.Collections.Generic;
using AF.Audio;
using AF.Core;
using AF.Localization;
using AF.Save;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization;
using VContainer.Unity;

namespace HoldIt.Settings
{
    /// <summary>
    /// The settings screen: the channel volumes (<see cref="IAudioVolumes"/>), vibration (<see cref="IGameSettings"/>) and
    /// the language (<see cref="ILocaleService"/>, cycled through its languages by their own names). Every change applies at
    /// once; closing the screen writes the global and device data. The screen is only reachable once the game has booted
    /// (no input mode takes clicks before), so the languages are known when it opens.
    /// </summary>
    internal sealed class SettingsPresenter : ISettingsScreen, IInitializable, IStartable, IDisposable
    {
        private readonly ISettingsView _view;
        private readonly IAudioVolumes _volumes;
        private readonly ILocaleService _locales;
        private readonly IGameSettings _game;
        private readonly ISaveService _save;
        private readonly IReadOnlyList<AudioChannel> _channels = AudioChannels.SettingsSliders;
        private int _language;

        public SettingsPresenter(ISettingsView view, IAudioVolumes volumes, ILocaleService locales, IGameSettings game,
            ISaveService save)
        {
            _view = view;
            _volumes = volumes;
            _locales = locales;
            _game = game;
            _save = save;
        }

        public bool IsOpen { get; private set; }

        public event Action Closed;

        public void Initialize()
        {
            _view.VolumeChanged += SetVolume;
            _view.HapticsChanged += SetHaptics;
            _view.LanguageStepped += StepLanguage;
            _view.BackClicked += Close;
        }

        // In Start, not Initialize: the view sets itself up in Awake, after the scope is built.
        public void Start()
        {
            if (_view.VolumeCount != _channels.Count)
                Log.Error($"Settings: the screen has {_view.VolumeCount} volume slider(s) for the {_channels.Count} channel(s) of AudioChannels.SettingsSliders.");
            _view.Hide();
        }

        public void Dispose()
        {
            _view.VolumeChanged -= SetVolume;
            _view.HapticsChanged -= SetHaptics;
            _view.LanguageStepped -= StepLanguage;
            _view.BackClicked -= Close;
        }

        public void Open()
        {
            if (IsOpen)
                return;
            IsOpen = true;

            for (var i = 0; i < Math.Min(_view.VolumeCount, _channels.Count); i++)
                _view.SetVolume(i, _volumes.Get(_channels[i]));
            _view.SetHaptics(_game.Haptics);
            _language = IndexOf(_locales.Current);
            ShowLanguage();
            _view.Show();
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            IsOpen = false;
            _view.Hide();
            _save.SaveGlobal().Forget();
            Closed?.Invoke();
        }

        private void SetVolume(int row, float volume)
        {
            if (IsOpen && row >= 0 && row < _channels.Count)
                _volumes.Set(_channels[row], volume);
        }

        private void SetHaptics(bool on)
        {
            if (IsOpen)
                _game.SetHaptics(on);
        }

        // The name changes at once; the texts follow once the language's tables are loaded. Pressed quickly, the last
        // language asked for wins (ILocaleService).
        private void StepLanguage(int step)
        {
            var available = _locales.Available;
            if (!IsOpen || available.Count == 0)
                return;
            _language = ((_language + step) % available.Count + available.Count) % available.Count;
            ShowLanguage();
            _locales.Set(available[_language]).Forget();
        }

        private void ShowLanguage()
        {
            var available = _locales.Available;
            _view.SetLanguage(available.Count > 0 ? LanguageNames.Of(available[_language]) : string.Empty);
        }

        private int IndexOf(Locale locale)
        {
            if (locale == null)
                return 0;
            var available = _locales.Available;
            for (var i = 0; i < available.Count; i++)
                if (available[i].Identifier == locale.Identifier)
                    return i;
            return 0;
        }
    }
}
