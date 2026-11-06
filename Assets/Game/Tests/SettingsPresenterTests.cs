using System;
using System.Collections.Generic;
using System.Threading;
using AF.Audio;
using AF.Localization;
using Cysharp.Threading.Tasks;
using HoldIt.Feedback;
using HoldIt.Settings;
using NUnit.Framework;
using UnityEngine.Localization;

namespace HoldIt.Tests
{
    public class SettingsPresenterTests
    {
        private FakeSettingsView _view;
        private FakeVolumes _volumes;
        private FakeLocales _locales;
        private GameSettings _game;
        private FakeSave _save;
        private RecordingFeedback _feedback;
        private SettingsPresenter _presenter;
        private Locale _en;
        private Locale _tr;
        private int _closes;

        [SetUp]
        public void SetUpPresenter()
        {
            _closes = 0;
            _en = Locale.CreateLocale("en");
            _tr = Locale.CreateLocale("tr");
            _view = new FakeSettingsView(AudioChannels.SettingsSliders.Count);
            _volumes = new FakeVolumes();
            _locales = new FakeLocales(_en, _tr) { Current = _en };
            _game = new GameSettings();
            _save = new FakeSave();
            _feedback = new RecordingFeedback(() => _game.Haptics);
            _presenter = new SettingsPresenter(_view, _volumes, _locales, _game, _save, _feedback);
            _presenter.Closed += () => _closes++;
            _presenter.Initialize();
            _presenter.Start();
        }

        [TearDown]
        public void TearDownPresenter()
        {
            _presenter.Dispose();
            UnityEngine.Object.DestroyImmediate(_en);
            UnityEngine.Object.DestroyImmediate(_tr);
        }

        [Test]
        public void Start_HidesTheScreen()
        {
            Assert.IsFalse(_view.Visible);
            Assert.AreEqual(1, _view.Hides);
            Assert.IsFalse(_presenter.IsOpen);
        }

        [Test]
        public void Open_ShowsTheCurrentSettings()
        {
            _volumes.Set(AudioChannels.Master, 0.8f);
            _volumes.Set(AudioChannels.Music, 0.3f);
            _volumes.Set(AudioChannels.Sfx, 0.6f);
            _game.SetHaptics(false);
            _locales.Current = _tr;

            _presenter.Open();

            Assert.IsTrue(_view.Visible);
            Assert.IsTrue(_presenter.IsOpen);
            CollectionAssert.AreEqual(new[] { 0.8f, 0.3f, 0.6f }, _view.Volumes);
            Assert.IsFalse(_view.Haptics);
            Assert.AreEqual("Türkçe", _view.Language);
        }

        [Test]
        public void Slider_SetsItsChannel()
        {
            _presenter.Open();

            _view.MoveSlider(1, 0.25f);

            Assert.AreEqual(0.25f, _volumes.Get(AudioChannels.Music));
            Assert.AreEqual(1f, _volumes.Get(AudioChannels.Master), "Only the slider's own channel changes.");
        }

        [Test]
        public void Toggle_SetsVibration()
        {
            _presenter.Open();

            _view.Toggle(false);

            Assert.IsFalse(_game.Haptics);
        }

        [Test]
        public void Toggle_GivesFeedbackAfterTheSettingIsWritten()
        {
            _presenter.Open();

            _view.Toggle(false);
            _view.Toggle(true);

            CollectionAssert.AreEqual(new[] { FeedbackKind.Toggle, FeedbackKind.Toggle }, _feedback.Played);
            CollectionAssert.AreEqual(new[] { false, true }, _feedback.HapticsWhenPlayed);
        }

        [Test]
        public void LanguageArrows_GoRoundTheLanguages()
        {
            _presenter.Open();

            _view.Step(1);
            Assert.AreEqual("Türkçe", _view.Language);
            Assert.AreSame(_tr, _locales.Sets[0]);

            _view.Step(1);
            Assert.AreEqual("English", _view.Language, "Past the last language comes the first.");
            Assert.AreSame(_en, _locales.Sets[1]);

            _view.Step(-1);
            Assert.AreEqual("Türkçe", _view.Language, "Before the first comes the last.");
            Assert.AreEqual(3, _locales.Sets.Count);
        }

        [Test]
        public void Close_HidesAndWritesOnce()
        {
            _presenter.Open();

            _presenter.Close();
            _presenter.Close();

            Assert.IsFalse(_view.Visible);
            Assert.IsFalse(_presenter.IsOpen);
            Assert.AreEqual(1, _save.GlobalSaves);
            Assert.AreEqual(1, _closes);
        }

        [Test]
        public void BackButton_Closes()
        {
            _presenter.Open();

            _view.ClickBack();

            Assert.IsFalse(_presenter.IsOpen);
            Assert.AreEqual(1, _save.GlobalSaves);
        }

        [Test]
        public void WhileClosed_ControlsAndClosingDoNothing()
        {
            _view.MoveSlider(0, 0.1f);
            _view.Toggle(false);
            _view.Step(1);
            _presenter.Close();

            Assert.AreEqual(1f, _volumes.Get(AudioChannels.Master));
            Assert.IsTrue(_game.Haptics);
            Assert.AreEqual(0, _locales.Sets.Count);
            Assert.AreEqual(0, _save.GlobalSaves);
            Assert.AreEqual(0, _closes);
            Assert.IsEmpty(_feedback.Played);
        }

        private sealed class RecordingFeedback : IFeedback
        {
            private readonly Func<bool> _haptics;

            public RecordingFeedback(Func<bool> haptics) => _haptics = haptics;

            public List<FeedbackKind> Played { get; } = new List<FeedbackKind>();
            public List<bool> HapticsWhenPlayed { get; } = new List<bool>();

            public void Play(FeedbackKind kind)
            {
                Played.Add(kind);
                HapticsWhenPlayed.Add(_haptics());
            }
        }

        private sealed class FakeSettingsView : ISettingsView
        {
            public readonly float[] Volumes;
            public bool Visible;
            public int Hides;
            public bool Haptics = true;
            public string Language;

            public FakeSettingsView(int sliders) => Volumes = new float[sliders];

            public event Action<int, float> VolumeChanged;
            public event Action<bool> HapticsChanged;
            public event Action<int> LanguageStepped;
            public event Action BackClicked;

            public int VolumeCount => Volumes.Length;

            public void SetVolume(int row, float volume) => Volumes[row] = volume;
            public void SetHaptics(bool on) => Haptics = on;
            public void SetLanguage(string name) => Language = name;

            public void Show() => Visible = true;

            public void Hide()
            {
                Visible = false;
                Hides++;
            }

            public void MoveSlider(int row, float value) => VolumeChanged?.Invoke(row, value);
            public void Toggle(bool on) => HapticsChanged?.Invoke(on);
            public void Step(int step) => LanguageStepped?.Invoke(step);
            public void ClickBack() => BackClicked?.Invoke();
        }

        private sealed class FakeVolumes : IAudioVolumes
        {
            private readonly Dictionary<AudioChannel, float> _volumes = new Dictionary<AudioChannel, float>();

            public IReadOnlyList<AudioChannel> Channels { get; } =
                new[] { AudioChannels.Master, AudioChannels.Music, AudioChannels.Sfx, AudioChannels.Ui };

            public bool MuteWhenUnfocused { get; set; }

            public float Get(AudioChannel channel) => _volumes.TryGetValue(channel, out var volume) ? volume : 1f;

            public void Set(AudioChannel channel, float volume) => _volumes[channel] = volume;

            public IDisposable Mute(string reason) => throw new NotSupportedException();
        }

        private sealed class FakeLocales : ILocaleService
        {
            public readonly List<Locale> Sets = new List<Locale>();

            public FakeLocales(params Locale[] available) => Available = available;

            public Locale Current { get; set; }
            public IReadOnlyList<Locale> Available { get; }

            public UniTask Set(Locale locale, CancellationToken cancellation = default)
            {
                Sets.Add(locale);
                Current = locale;
                return UniTask.CompletedTask;
            }
        }
    }
}
