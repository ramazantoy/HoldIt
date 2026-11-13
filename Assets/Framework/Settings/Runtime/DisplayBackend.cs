using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AF.Settings
{
    /// <summary>What <see cref="DisplaySettings"/> reads from and writes to Unity; tests use their own.</summary>
    internal interface IDisplayBackend : IDisposable
    {
        bool IsMobile { get; }
        bool SupportsExclusive { get; }

        /// <summary>The native size of the display the window is on, whatever the game's own resolution.</summary>
        Vector2Int DisplaySize { get; }

        /// <summary>The part of that display left for windows: without the taskbar.</summary>
        Vector2Int WorkArea { get; }

        int DisplayRefreshRate { get; }

        /// <summary>Fullscreen sizes the display supports, once per refresh rate.</summary>
        IEnumerable<Vector2Int> SupportedSizes { get; }

        IReadOnlyList<string> QualityLevels { get; }

        /// <summary>The level active when the app started: the platform's default quality level.</summary>
        string StartupQuality { get; }

        void ApplyQuality(string level);
        void ApplyFramePacing(int vSyncCount, int targetFrameRate);
        void ApplyWindow(Vector2Int size, WindowMode mode);
    }

    /// <summary>
    /// The only code that writes the screen, quality and frame rate settings. In the Editor the Game view keeps its own size
    /// and vsync toggle, so the window and <c>vSyncCount</c> are left alone, and the quality level goes back to what it was
    /// when Play stops (it would otherwise end up in QualitySettings.asset).
    /// </summary>
    internal sealed class UnityDisplayBackend : IDisplayBackend
    {
        private readonly string[] _qualityNames;
        private readonly int _startupLevel;
        private Vector2Int _displaySize;
        private Vector2Int _workArea;
        private bool _frameRateWritten;

        public UnityDisplayBackend()
        {
            _qualityNames = QualitySettings.names;
            _startupLevel = QualitySettings.GetQualityLevel();
            QualityLevels = PlatformLevels(_qualityNames);
            StartupQuality = _qualityNames[_startupLevel];
        }

        public bool IsMobile => Application.isMobilePlatform;

        public bool SupportsExclusive =>
            Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor;

        public Vector2Int DisplaySize
        {
            get
            {
                ReadDisplay();
                if (_displaySize.x > 0)
                    return _displaySize;
                // Started in exclusive fullscreen (Unity restores the last mode itself): the primary display's native size.
                // Screen.currentResolution would be the game's own resolution.
                var main = Display.main;
                if (main.systemWidth > 0 && main.systemHeight > 0)
                    return new Vector2Int(main.systemWidth, main.systemHeight);
                var current = Screen.currentResolution;
                return new Vector2Int(current.width, current.height);
            }
        }

        public Vector2Int WorkArea
        {
            get
            {
                ReadDisplay();
                return _workArea.x > 0 ? _workArea : DisplaySize;
            }
        }

        public int DisplayRefreshRate
        {
            get
            {
                var rate = (int)Math.Round(Screen.currentResolution.refreshRateRatio.value);
                return rate > 0 ? rate : 60;
            }
        }

        public IEnumerable<Vector2Int> SupportedSizes => Screen.resolutions.Select(resolution => new Vector2Int(resolution.width, resolution.height));
        public IReadOnlyList<string> QualityLevels { get; }
        public string StartupQuality { get; }

        public void ApplyQuality(string level)
        {
            var index = Array.IndexOf(_qualityNames, level);
            if (index >= 0 && index != QualitySettings.GetQualityLevel())
                QualitySettings.SetQualityLevel(index, applyExpensiveChanges: true);
        }

        public void ApplyFramePacing(int vSyncCount, int targetFrameRate)
        {
            // vSyncCount is a field of the active quality level: writing it in the Editor changes QualitySettings.asset.
            if (!Application.isEditor && QualitySettings.vSyncCount != vSyncCount)
                QualitySettings.vSyncCount = vSyncCount;
            if (Application.targetFrameRate == targetFrameRate)
                return;
            Application.targetFrameRate = targetFrameRate;
            _frameRateWritten = true;
        }

        public void ApplyWindow(Vector2Int size, WindowMode mode)
        {
            if (Application.isEditor)
                return;
            var fullScreenMode = mode switch
            {
                WindowMode.Windowed => FullScreenMode.Windowed,
                WindowMode.Exclusive => FullScreenMode.ExclusiveFullScreen,
                _ => FullScreenMode.FullScreenWindow,
            };
            // Unchanged settings are not set again: the window would flicker at startup.
            if (Screen.width == size.x && Screen.height == size.y && Screen.fullScreenMode == fullScreenMode)
                return;
            if (fullScreenMode == FullScreenMode.ExclusiveFullScreen)
                Screen.SetResolution(size.x, size.y, fullScreenMode, HighestRefreshRate(size));
            else
                Screen.SetResolution(size.x, size.y, fullScreenMode);
        }

        public void Dispose()
        {
            if (!Application.isEditor)
                return;
            if (QualitySettings.GetQualityLevel() != _startupLevel)
                QualitySettings.SetQualityLevel(_startupLevel, applyExpensiveChanges: true);
            if (_frameRateWritten)
                Application.targetFrameRate = -1;
        }

        // In exclusive fullscreen the display runs at the game's resolution, so its size is the one read before.
        private void ReadDisplay()
        {
            // Screen.mainWindowDisplayInfo exists on desktop standalone only; on a phone it throws, and the display is the screen.
            if (Application.isMobilePlatform || Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen)
                return;
            var display = Screen.mainWindowDisplayInfo;
            if (display.width <= 0 || display.height <= 0)
                return;
            _displaySize = new Vector2Int(display.width, display.height);
            _workArea = display.workArea.width > 0 && display.workArea.height > 0
                ? new Vector2Int(display.workArea.width, display.workArea.height)
                : _displaySize;
        }

        private static RefreshRate HighestRefreshRate(Vector2Int size)
        {
            var best = Screen.currentResolution.refreshRateRatio;
            var found = false;
            foreach (var resolution in Screen.resolutions)
            {
                if (resolution.width != size.x || resolution.height != size.y)
                    continue;
                if (!found || resolution.refreshRateRatio.value > best.value)
                    best = resolution.refreshRateRatio;
                found = true;
            }
            return best;
        }

        private static IReadOnlyList<string> PlatformLevels(string[] names)
        {
#if UNITY_EDITOR
            // The Editor knows every level; a build contains only those of its platform.
            var group = UnityEditor.BuildPipeline.GetBuildTargetGroup(UnityEditor.EditorUserBuildSettings.activeBuildTarget);
            var platform = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group).TargetName;
            return QualitySettings.GetActiveQualityLevelsForPlatform(platform).Select(index => names[index]).ToArray();
#else
            return names;
#endif
        }
    }
}
