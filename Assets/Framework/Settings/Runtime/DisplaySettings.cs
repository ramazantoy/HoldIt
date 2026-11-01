using System;
using System.Collections.Generic;
using System.Linq;
using AF.Core;
using AF.Input;
using AF.Save;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace AF.Settings
{
    /// <summary>
    /// The player's display choices, a framework-owned section of this device's save data. A null field means the player
    /// has not chosen: the default applies, also a later build's. Fields are only ever added; a breaking change uses a new key.
    /// </summary>
    public sealed class DisplaySettingsData
    {
        public int? width;
        public int? height;
        public WindowMode? windowMode;
        public bool? vsync;
        public int? frameRateLimit;
        public string quality;
        public bool? confineCursor;
    }

    internal sealed class DisplaySettings : Savable<DisplaySettingsData>, IDisplaySettings, IDisposable
    {
        internal const int MinFrameRateLimit = 15;
        private const int TitleBarAllowance = 64;

        private static readonly WindowMode[] _desktopModes = { WindowMode.Windowed, WindowMode.Borderless };
        private static readonly WindowMode[] _windowsModes = { WindowMode.Windowed, WindowMode.Borderless, WindowMode.Exclusive };

        private readonly SettingsOptions _options;
        private readonly IDisplayBackend _backend;
        private readonly IInputModeService _input;
        private readonly IPublisher<DisplaySettingsChanged> _changed;
        private DisplaySettingsData _data = new DisplaySettingsData();

        [Inject]
        public DisplaySettings(SettingsOptions options, IInputModeService input, IPublisher<DisplaySettingsChanged> changed)
            : this(options, new UnityDisplayBackend(), input, changed)
        {
        }

        internal DisplaySettings(SettingsOptions options, IDisplayBackend backend, IInputModeService input,
            IPublisher<DisplaySettingsChanged> changed)
        {
            _options = options;
            _backend = backend;
            _input = input;
            _changed = changed;
            Current = Resolve(_data, warn: false);
        }

        public override string Key => "settings.display";
        public override SaveDocument Document => SaveDocument.Device;

        public DisplayState Current { get; private set; }
        public bool HasWindow => !_backend.IsMobile;

        public IReadOnlyList<WindowMode> WindowModes =>
            !HasWindow ? Array.Empty<WindowMode>() : _backend.SupportsExclusive ? _windowsModes : _desktopModes;

        public IReadOnlyList<Vector2Int> Resolutions => HasWindow ? SizesFor(Current.WindowMode) : Array.Empty<Vector2Int>();
        public IReadOnlyList<string> QualityLevels => _backend.QualityLevels;

        public void SetResolution(Vector2Int size)
        {
            RequireWindow();
            var sizes = SizesFor(Current.WindowMode);
            if (!sizes.Contains(size))
                throw new ArgumentException($"{size.x}x{size.y} is not offered in {Current.WindowMode} mode; pick one of Resolutions.", nameof(size));
            var isDefault = size == DefaultSize(sizes);
            _data.width = isDefault ? (int?)null : size.x;
            _data.height = isDefault ? (int?)null : size.y;
            Apply(warn: false);
        }

        public void SetWindowMode(WindowMode mode)
        {
            RequireWindow();
            if (!WindowModes.Contains(mode))
                throw new ArgumentException($"{mode} is not offered on this platform; pick one of WindowModes.", nameof(mode));
            _data.windowMode = mode == _options.WindowMode ? (WindowMode?)null : mode;
            Apply(warn: false);
        }

        public void SetVSync(bool on)
        {
            RequireWindow();
            _data.vsync = on == _options.VSync ? (bool?)null : on;
            Apply(warn: false);
        }

        public void SetFrameRateLimit(int framesPerSecond)
        {
            if (!IsValidLimit(framesPerSecond))
                throw new ArgumentOutOfRangeException(nameof(framesPerSecond), framesPerSecond, $"Use 0 (no limit) or at least {MinFrameRateLimit}.");
            _data.frameRateLimit = framesPerSecond == DefaultFrameRateLimit ? (int?)null : framesPerSecond;
            Apply(warn: false);
        }

        public void SetQuality(string level)
        {
            if (!_backend.QualityLevels.Contains(level))
                throw new ArgumentException($"'{level}' is not a quality level of this platform; pick one of QualityLevels.", nameof(level));
            _data.quality = level == _backend.StartupQuality ? null : level;
            Apply(warn: false);
        }

        public void SetConfineCursor(bool on)
        {
            RequireWindow();
            _data.confineCursor = on == _options.ConfineCursor ? (bool?)null : on;
            Apply(warn: false);
        }

        public void ResetToDefaults()
        {
            _data = new DisplaySettingsData();
            Apply(warn: false);
        }

        public void Dispose() => _backend.Dispose();

        protected override DisplaySettingsData Capture() => _data;

        protected override void Restore(DisplaySettingsData data)
        {
            _data = data;
            Apply(warn: true);
            Log.Info($"Settings: display {Current} (display {_backend.DisplaySize.x}x{_backend.DisplaySize.y})");
        }

        private int DefaultFrameRateLimit => _backend.IsMobile ? _options.MobileFrameRateLimit : _options.FrameRateLimit;

        private void Apply(bool warn)
        {
            var next = Resolve(_data, warn);

            // Each step on its own: one that fails is logged and the others still apply.
            Try(() => _backend.ApplyQuality(next.Quality));
            // Changing the quality level resets vSyncCount to that level's value, so frame pacing comes after it.
            Try(() => _backend.ApplyFramePacing(VSyncCount(next), TargetFrameRate(next)));
            if (HasWindow)
                Try(() => _backend.ApplyWindow(next.Resolution, next.WindowMode));
            Try(() => _input.ConfineCursor = next.ConfineCursor);

            var previous = Current;
            Current = next;
            if (!previous.Equals(next))
                _changed.Publish(new DisplaySettingsChanged(previous, next));
        }

        private DisplayState Resolve(DisplaySettingsData data, bool warn)
        {
            var mobile = _backend.IsMobile;

            var mode = mobile ? WindowMode.Borderless : data.windowMode ?? _options.WindowMode;
            if (!mobile && !WindowModes.Contains(mode))
            {
                Warn(warn, $"{mode} is not offered on this platform");
                mode = WindowModes.Contains(_options.WindowMode) ? _options.WindowMode : WindowMode.Borderless;
            }

            var sizes = SizesFor(mode);
            var size = DefaultSize(sizes);
            if (!mobile && data.width.HasValue && data.height.HasValue)
            {
                var chosen = new Vector2Int(data.width.Value, data.height.Value);
                if (sizes.Contains(chosen))
                    size = chosen;
                else
                    Warn(warn, $"{chosen.x}x{chosen.y} is not offered in {mode} mode on this display ({_backend.DisplaySize.x}x{_backend.DisplaySize.y})");
            }

            var limit = data.frameRateLimit ?? DefaultFrameRateLimit;
            if (!IsValidLimit(limit))
            {
                Warn(warn, $"the frame rate limit {limit} is out of range");
                limit = DefaultFrameRateLimit;
            }

            var quality = data.quality ?? _backend.StartupQuality;
            if (!_backend.QualityLevels.Contains(quality))
            {
                Warn(warn, $"there is no quality level '{quality}'");
                quality = _backend.StartupQuality;
            }

            var vSync = !mobile && (data.vsync ?? _options.VSync);
            var confine = !mobile && (data.confineCursor ?? _options.ConfineCursor);
            return new DisplayState(size, mode, vSync, limit, quality, confine);
        }

        /// <summary>The display's supported sizes for <paramref name="mode"/>, largest first, each once.</summary>
        private IReadOnlyList<Vector2Int> SizesFor(WindowMode mode)
        {
            var display = _backend.DisplaySize;
            var work = _backend.WorkArea;
            var windowed = mode == WindowMode.Windowed;
            var sizes = _backend.SupportedSizes
                .Distinct()
                // A window fits the area the taskbar leaves, with room for its title bar.
                .Where(size => windowed
                    ? size.x < work.x && size.y <= work.y - TitleBarAllowance
                    : size.x <= display.x && size.y <= display.y)
                .OrderByDescending(size => size.x * size.y)
                .ThenByDescending(size => size.x)
                .ToList();
            if (!windowed && !sizes.Contains(display))
                sizes.Insert(0, display);
            if (sizes.Count == 0)
                sizes.Add(display);
            return sizes;
        }

        /// <summary>The display's own size for fullscreen, the largest window that fits otherwise.</summary>
        private static Vector2Int DefaultSize(IReadOnlyList<Vector2Int> sizes) => sizes[0];

        private static int VSyncCount(DisplayState state) => state.VSync ? 1 : 0;

        private int TargetFrameRate(DisplayState state)
        {
            if (state.VSync)
                return -1;
            if (state.FrameRateLimit > 0)
                return state.FrameRateLimit;
            // -1 is "no limit" on desktop but 30 frames per second on mobile.
            return _backend.IsMobile ? _backend.DisplayRefreshRate : -1;
        }

        private void RequireWindow()
        {
            if (!HasWindow)
                throw new InvalidOperationException("This platform has no window: resolution, window mode, vsync and cursor confinement do not apply.");
        }

        internal static bool IsValidLimit(int framesPerSecond) => framesPerSecond == 0 || framesPerSecond >= MinFrameRateLimit;

        private static void Warn(bool warn, string problem)
        {
            if (warn)
                Log.Warning($"Settings: {problem}; the default applies this session. The saved choice is kept.");
        }

        private static void Try(Action step)
        {
            try
            {
                step();
            }
            catch (Exception exception)
            {
                Log.Exception(exception);
            }
        }
    }
}
