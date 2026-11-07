using System;
using AF.Core;
using UnityEngine;

namespace HoldIt.Backdrop
{
    /// <summary>
    /// The gameplay backdrop (<see cref="BackdropPlayer"/>): the skybox material it paints with, the colour it starts from,
    /// the palettes the levels take in turn, and how long the change into one takes.
    /// </summary>
    [CreateAssetMenu(menuName = "HoldIt/Backdrop/Backdrop Config", fileName = "BackdropConfig")]
    public sealed class BackdropConfig : ScriptableObject
    {
        [Serializable]
        public struct Palette
        {
            [Tooltip("Only a label for the inspector.")]
            public string name;

            public Color top;
            public Color bottom;

            public BackdropColors Colors => new BackdropColors(top, bottom);
        }

        [SerializeField, Required, Tooltip("A material of the HoldIt/Backdrop shader. The scene paints with a copy of it.")]
        private Material _material;

        [SerializeField, Tooltip("What the backdrop is when a level opens, before it changes into the level's palette.")]
        private Palette _start = new Palette
        {
            name = "Start",
            top = new Color(0.42f, 0.45f, 0.50f),
            bottom = new Color(0.30f, 0.33f, 0.38f),
        };

        [SerializeField, Tooltip("Level 1 takes the first, level 2 the second, and so on; after the last they start over.")]
        private Palette[] _palettes =
        {
            new Palette { name = "Cream", top = Hex(0xF3E9D2), bottom = Hex(0xE2D3B4) },
            new Palette { name = "Mint", top = Hex(0xD6EFE2), bottom = Hex(0xB4DCC8) },
            new Palette { name = "Sky", top = Hex(0xD3E6F5), bottom = Hex(0xB1CCE6) },
            new Palette { name = "Peach", top = Hex(0xF8DCCB), bottom = Hex(0xEDBFA6) },
            new Palette { name = "Lilac", top = Hex(0xE3DAF2), bottom = Hex(0xC8BBE3) },
            new Palette { name = "Butter", top = Hex(0xF6EDB8), bottom = Hex(0xE8D98F) },
        };

        [SerializeField, Min(0.1f), Tooltip("Seconds of the change when the colours barely differ.")]
        private float _minSeconds = 1f;

        [SerializeField, Min(0.1f), Tooltip("Seconds of the change when the colours differ by Full Distance or more.")]
        private float _maxSeconds = 3f;

        [SerializeField, Min(0.01f), Tooltip("How far apart two gradients are (RGB distance, 0 to 1.7) for the change to take Max Seconds.")]
        private float _fullDistance = 0.5f;

        public Material Material => _material;
        public BackdropColors Start => _start.Colors;
        public int PaletteCount => _palettes.Length;
        public float MinSeconds => _minSeconds;
        public float MaxSeconds => _maxSeconds;
        public float FullDistance => _fullDistance;

        public BackdropColors PaletteAt(int index) => _palettes[index].Colors;

        private static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
    }
}
