using System;
using System.Collections.Generic;
using AF.Core;
using HoldIt.Board;
using HoldIt.Gameplay;
using HoldIt.Levels;
using HoldIt.UI;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Menu
{
    /// <summary>
    /// The level select screen (LevelSelectScreen prefab): a grid of level cells in a vertical scroll view. The cells are
    /// made the first time the screen shows, one per level, and kept. Each shows a small picture of its level's map, made
    /// once per level (<see cref="MapPreview"/>) and destroyed with the screen.
    /// </summary>
    public sealed class LevelSelectView : ScreenView, ILevelSelectView
    {
        [SerializeField, Required] private LevelCellView _cellPrefab;

        [SerializeField, Required, Tooltip("The scroll view's content: the cells go under it.")]
        private RectTransform _grid;

        [SerializeField, Required] private GameplayUiStyle _style;
        [SerializeField, Required] private Button _back;

        private readonly List<LevelCellView> _cells = new List<LevelCellView>();
        private readonly List<Texture2D> _previews = new List<Texture2D>();
        private IObjectResolver _resolver;
        private LevelList _levels;

        public event Action<int> LevelClicked;
        public event Action BackClicked;

        [Inject]
        public void Construct(IObjectResolver resolver, LevelList levels)
        {
            _resolver = resolver;
            _levels = levels;
        }

        public void Show(IReadOnlyList<LevelSlot> slots, int maxStars)
        {
            while (_cells.Count < slots.Count)
            {
                var index = _cells.Count;
                var cell = _resolver.Instantiate(_cellPrefab, _grid);
                cell.name = $"Level {index + 1}";
                cell.Clicked += () => LevelClicked?.Invoke(index);
                _cells.Add(cell);
            }
            for (var i = 0; i < _cells.Count; i++)
            {
                _cells[i].gameObject.SetActive(i < slots.Count);
                if (i < slots.Count)
                    _cells[i].Show(slots[i], maxStars, _style, PreviewOf(i));
            }
            ShowScreen();
        }

        public void Hide() => HideScreen();

        private void Awake() => _back.onClick.AddListener(() => BackClicked?.Invoke());

        protected override void OnDestroy()
        {
            base.OnDestroy();
            foreach (var preview in _previews)
            {
                if (preview != null)
                    Destroy(preview);
            }
            _previews.Clear();
        }

        /// <summary>The level's map picture, made the first time it is asked for; null for a level without a readable map.</summary>
        private Texture2D PreviewOf(int index)
        {
            while (_previews.Count <= index)
                _previews.Add(null);
            if (_previews[index] != null)
                return _previews[index];

            var levels = _levels.Levels;
            if (index >= levels.Count || levels[index] == null || levels[index].Map == null)
                return null;
            try
            {
                var grid = MapParser.Parse(levels[index].Map.Rows);
                var scale = _style.PreviewPixelsPerTile;
                var texture = new Texture2D(grid.Cols * scale, grid.Rows * scale, TextureFormat.RGBA32, false)
                {
                    name = $"Map preview {index + 1}",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                };
                texture.SetPixels32(MapPreview.Pixels(grid, _style.MapColors, scale));
                texture.Apply(false, true);
                _previews[index] = texture;
                return texture;
            }
            catch (MapFormatException exception)
            {
                Log.Warning($"Level {index + 1} has no preview: {exception.Message}");
                return null;
            }
        }
    }
}
