using HoldIt.UI;
using UnityEngine;

namespace HoldIt.Gameplay
{
    /// <summary>
    /// The colors of the game's screens: how the HUD's hearts and the stars (result screen, level select) show what was kept
    /// and what was lost, and how the level select marks open, newest and locked levels.
    /// </summary>
    [CreateAssetMenu(menuName = "HoldIt/Gameplay/Gameplay UI Style", fileName = "GameplayUiStyle")]
    public sealed class GameplayUiStyle : ScriptableObject
    {
        [SerializeField] private Color _heartFull = new Color(0xe8 / 255f, 0x48 / 255f, 0x55 / 255f);
        [SerializeField] private Color _heartLost = new Color(0xc3 / 255f, 0xc9 / 255f, 0xd0 / 255f);

        [SerializeField, Range(0.1f, 1f), Tooltip("A lost heart's size, as a share of a full one.")]
        private float _lostHeartScale = 0.8f;

        [SerializeField] private Color _starEarned = new Color(0xff / 255f, 0xc8 / 255f, 0x3d / 255f);
        [SerializeField] private Color _starMissed = new Color(0xc3 / 255f, 0xc9 / 255f, 0xd0 / 255f);

        [Header("Gameplay feedback")]
        [SerializeField, Tooltip("The colour the screen flashes in when a life is lost; its alpha is ignored.")]
        private Color _flashColor = new Color(0xe8 / 255f, 0x48 / 255f, 0x55 / 255f);

        [SerializeField, Range(0f, 1f), Tooltip("How opaque the flash starts.")]
        private float _flashAlpha = 0.32f;

        [SerializeField, Min(0.01f), Tooltip("How long the flash takes to fade, in seconds.")]
        private float _flashSeconds = 0.6f;

        [SerializeField, Min(0f), Tooltip("How much a HUD pill swells when its number changes (a fraction of its size).")]
        private float _bumpScale = 0.18f;

        [SerializeField, Min(0.01f), Tooltip("How long that swell lasts, in seconds.")]
        private float _bumpSeconds = 0.35f;

        [SerializeField, Min(0.01f), Tooltip("How long a heart takes to grey and shrink after a life is lost, in seconds.")]
        private float _heartSeconds = 0.3f;

        [SerializeField, Min(0f), Tooltip("Seconds after the result screen shows before the first star pops.")]
        private float _starPopDelay = 0.25f;

        [SerializeField, Min(0f), Tooltip("Seconds between one star's pop and the next.")]
        private float _starPopGap = 0.12f;

        [SerializeField, Min(0.01f), Tooltip("How long one star takes to pop in, in seconds.")]
        private float _starPopSeconds = 0.3f;

        [Header("Level select")]
        [SerializeField, Tooltip("An open level's cell.")]
        private Color _cellOpen = new Color(0x46 / 255f, 0x82 / 255f, 0xdc / 255f);

        [SerializeField, Tooltip("The last open level's cell: where the player got to.")]
        private Color _cellNewest = new Color(0x34 / 255f, 0xb3 / 255f, 0x6b / 255f);

        [SerializeField, Tooltip("A locked level's cell.")]
        private Color _cellLocked = new Color(0x3a / 255f, 0x41 / 255f, 0x50 / 255f);

        [SerializeField, Tooltip("The colour of every kind of tile in a level's map preview.")]
        private MapPreviewPalette _mapPreview = MapPreviewPalette.Default;

        [SerializeField, Tooltip("What a locked level's preview is multiplied by, so it reads as out of reach.")]
        private Color _previewLocked = new Color(1f, 1f, 1f, 0.35f);

        [SerializeField, Range(1, 8), Tooltip("Pixels a map tile takes in its preview texture. The texture is shown with point filtering.")]
        private int _previewPixelsPerTile = 4;

        public Color HeartFull => _heartFull;
        public Color HeartLost => _heartLost;
        public float LostHeartScale => _lostHeartScale;
        public Color StarEarned => _starEarned;
        public Color StarMissed => _starMissed;
        public Color FlashColor => _flashColor;
        public float FlashAlpha => _flashAlpha;
        public float FlashSeconds => _flashSeconds;
        public float BumpScale => _bumpScale;
        public float BumpSeconds => _bumpSeconds;
        public float HeartSeconds => _heartSeconds;
        public MapPreviewPalette MapColors => _mapPreview;
        public Color PreviewLocked => _previewLocked;
        public int PreviewPixelsPerTile => _previewPixelsPerTile;
        public float StarPopDelay => _starPopDelay;
        public float StarPopGap => _starPopGap;
        public float StarPopSeconds => _starPopSeconds;
        public Color CellOpen => _cellOpen;
        public Color CellNewest => _cellNewest;
        public Color CellLocked => _cellLocked;
    }
}
