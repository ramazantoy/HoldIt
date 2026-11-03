using System;
using System.Collections.Generic;
using AF.Core;
using HoldIt.Gameplay;
using HoldIt.UI;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Menu
{
    /// <summary>
    /// The level select screen (LevelSelectScreen prefab): a grid of level cells in a vertical scroll view. The cells are
    /// made the first time the screen shows, one per level, and kept.
    /// </summary>
    public sealed class LevelSelectView : ScreenView, ILevelSelectView
    {
        [SerializeField, Required] private LevelCellView _cellPrefab;

        [SerializeField, Required, Tooltip("The scroll view's content: the cells go under it.")]
        private RectTransform _grid;

        [SerializeField, Required] private GameplayUiStyle _style;
        [SerializeField, Required] private Button _back;

        private readonly List<LevelCellView> _cells = new List<LevelCellView>();
        private IObjectResolver _resolver;

        public event Action<int> LevelClicked;
        public event Action BackClicked;

        [Inject]
        public void Construct(IObjectResolver resolver) => _resolver = resolver;

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
                    _cells[i].Show(slots[i], maxStars, _style);
            }
            ShowScreen();
        }

        public void Hide() => HideScreen();

        private void Awake() => _back.onClick.AddListener(() => BackClicked?.Invoke());
    }
}
