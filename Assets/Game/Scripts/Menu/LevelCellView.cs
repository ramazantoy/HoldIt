using System;
using System.Globalization;
using AF.Core;
using HoldIt.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HoldIt.Menu
{
    /// <summary>
    /// One level in the level select (LevelCell prefab): a preview of its map, its number and best stars when open, a lock
    /// when not. The newest open level stands out by its color.
    /// </summary>
    public sealed class LevelCellView : MonoBehaviour
    {
        [SerializeField, Required] private Button _button;
        [SerializeField, Required] private Image _background;
        [SerializeField, Required] private TextMeshProUGUI _number;

        [SerializeField, Required, Tooltip("Shows the map's preview texture.")]
        private RawImage _preview;

        [SerializeField, Required, Tooltip("Keeps the preview in the map's proportions inside its area.")]
        private AspectRatioFitter _previewFit;

        [SerializeField, Tooltip("One per star, left to right; stars past the level's most are not shown.")]
        private Image[] _stars = Array.Empty<Image>();

        [SerializeField, Required] private Image _lock;

        public event Action Clicked;

        /// <param name="preview">The level's map as a picture; null for a level without a map.</param>
        public void Show(LevelSlot slot, int maxStars, GameplayUiStyle style, Texture2D preview)
        {
            _number.text = slot.Number.ToString(CultureInfo.InvariantCulture);
            _number.gameObject.SetActive(slot.IsOpen);
            _lock.gameObject.SetActive(!slot.IsOpen);
            for (var i = 0; i < _stars.Length; i++)
            {
                _stars[i].gameObject.SetActive(slot.IsOpen && i < maxStars);
                _stars[i].color = i < slot.Stars ? style.StarEarned : style.StarMissed;
            }
            _background.color = !slot.IsOpen ? style.CellLocked : slot.IsNewest ? style.CellNewest : style.CellOpen;
            _button.interactable = slot.IsOpen;

            _preview.gameObject.SetActive(preview != null);
            if (preview == null)
                return;
            _preview.texture = preview;
            _preview.color = slot.IsOpen ? Color.white : style.PreviewLocked;
            _previewFit.aspectRatio = (float)preview.width / preview.height;
        }

        private void Awake() => _button.onClick.AddListener(() => Clicked?.Invoke());
    }
}
