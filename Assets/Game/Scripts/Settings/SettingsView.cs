using System;
using AF.Core;
using HoldIt.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HoldIt.Settings
{
    /// <summary>The settings screen (SettingsScreen prefab). Forwards the controls; SettingsPresenter decides what they do.</summary>
    public sealed class SettingsView : ScreenView, ISettingsView
    {
        [SerializeField, Tooltip("One per row of AudioChannels.SettingsSliders, top to bottom.")]
        private Slider[] _volumes = Array.Empty<Slider>();

        [SerializeField, Required] private Toggle _haptics;
        [SerializeField, Required] private Button _previousLanguage;
        [SerializeField, Required] private Button _nextLanguage;
        [SerializeField, Required] private TextMeshProUGUI _language;
        [SerializeField, Required] private Button _back;

        public event Action<int, float> VolumeChanged;
        public event Action<bool> HapticsChanged;
        public event Action<int> LanguageStepped;
        public event Action BackClicked;

        public int VolumeCount => _volumes.Length;

        public void SetVolume(int row, float volume) => _volumes[row].SetValueWithoutNotify(volume);

        public void SetHaptics(bool on) => _haptics.SetIsOnWithoutNotify(on);

        public void SetLanguage(string name) => _language.text = name;

        public void Show() => ShowScreen();

        public void Hide() => HideScreen();

        private void Awake()
        {
            for (var i = 0; i < _volumes.Length; i++)
            {
                var row = i;
                _volumes[i].onValueChanged.AddListener(value => VolumeChanged?.Invoke(row, value));
            }
            _haptics.onValueChanged.AddListener(on => HapticsChanged?.Invoke(on));
            _previousLanguage.onClick.AddListener(() => LanguageStepped?.Invoke(-1));
            _nextLanguage.onClick.AddListener(() => LanguageStepped?.Invoke(1));
            _back.onClick.AddListener(() => BackClicked?.Invoke());
        }
    }
}
