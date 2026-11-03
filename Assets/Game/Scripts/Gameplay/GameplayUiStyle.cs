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

        [Header("Level select")]
        [SerializeField, Tooltip("An open level's cell.")]
        private Color _cellOpen = new Color(0x46 / 255f, 0x82 / 255f, 0xdc / 255f);

        [SerializeField, Tooltip("The last open level's cell: where the player got to.")]
        private Color _cellNewest = new Color(0x34 / 255f, 0xb3 / 255f, 0x6b / 255f);

        [SerializeField, Tooltip("A locked level's cell.")]
        private Color _cellLocked = new Color(0x3a / 255f, 0x41 / 255f, 0x50 / 255f);

        public Color HeartFull => _heartFull;
        public Color HeartLost => _heartLost;
        public float LostHeartScale => _lostHeartScale;
        public Color StarEarned => _starEarned;
        public Color StarMissed => _starMissed;
        public Color CellOpen => _cellOpen;
        public Color CellNewest => _cellNewest;
        public Color CellLocked => _cellLocked;
    }
}
