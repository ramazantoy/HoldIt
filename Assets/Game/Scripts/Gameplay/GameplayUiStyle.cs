using UnityEngine;

namespace HoldIt.Gameplay
{
    /// <summary>How the HUD's hearts and the result screen's stars show what was kept and what was lost.</summary>
    [CreateAssetMenu(menuName = "HoldIt/Gameplay/Gameplay UI Style", fileName = "GameplayUiStyle")]
    public sealed class GameplayUiStyle : ScriptableObject
    {
        [SerializeField] private Color _heartFull = new Color(0xe8 / 255f, 0x48 / 255f, 0x55 / 255f);
        [SerializeField] private Color _heartLost = new Color(0xc3 / 255f, 0xc9 / 255f, 0xd0 / 255f);

        [SerializeField, Range(0.1f, 1f), Tooltip("A lost heart's size, as a share of a full one.")]
        private float _lostHeartScale = 0.8f;

        [SerializeField] private Color _starEarned = new Color(0xff / 255f, 0xc8 / 255f, 0x3d / 255f);
        [SerializeField] private Color _starMissed = new Color(0xc3 / 255f, 0xc9 / 255f, 0xd0 / 255f);

        public Color HeartFull => _heartFull;
        public Color HeartLost => _heartLost;
        public float LostHeartScale => _lostHeartScale;
        public Color StarEarned => _starEarned;
        public Color StarMissed => _starMissed;
    }
}
