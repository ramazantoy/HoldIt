using System;
using HoldIt.Board;
using UnityEngine;

namespace HoldIt.UI
{
    /// <summary>The colour of every kind of map tile in a level's preview (<see cref="MapPreview"/>).</summary>
    [Serializable]
    public struct MapPreviewPalette
    {
        [SerializeField] private Color32 _floor;
        [SerializeField] private Color32 _lobby;
        [SerializeField] private Color32 _entrance;
        [SerializeField] private Color32 _wall;
        [SerializeField] private Color32 _narrow;
        [SerializeField] private Color32 _buildingMale;
        [SerializeField] private Color32 _buildingFemale;
        [SerializeField] private Color32 _doorMale;
        [SerializeField] private Color32 _doorFemale;

        public MapPreviewPalette(Color32 floor, Color32 lobby, Color32 entrance, Color32 wall, Color32 narrow,
            Color32 buildingMale, Color32 buildingFemale, Color32 doorMale, Color32 doorFemale)
        {
            _floor = floor;
            _lobby = lobby;
            _entrance = entrance;
            _wall = wall;
            _narrow = narrow;
            _buildingMale = buildingMale;
            _buildingFemale = buildingFemale;
            _doorMale = doorMale;
            _doorFemale = doorFemale;
        }

        /// <summary>Light floor, dark walls, blue and pink restrooms, as the board looks.</summary>
        public static MapPreviewPalette Default => new MapPreviewPalette(
            new Color32(0xe4, 0xe8, 0xee, 0xff), new Color32(0xb9, 0xd3, 0xf0, 0xff), new Color32(0x34, 0xb3, 0x6b, 0xff),
            new Color32(0x5a, 0x6b, 0x7d, 0xff), new Color32(0xc9, 0xa5, 0x6a, 0xff), new Color32(0x8f, 0xb9, 0xee, 0xff),
            new Color32(0xf3, 0xa5, 0xc7, 0xff), new Color32(0x2f, 0x5f, 0xb8, 0xff), new Color32(0xc4, 0x3f, 0x80, 0xff));

        public Color32 Of(Tile tile)
        {
            var male = tile.Gender == Gender.Male;
            switch (tile.Type)
            {
                case TileType.Lobby:
                    return _lobby;
                case TileType.Entrance:
                    return _entrance;
                case TileType.Wall:
                    return _wall;
                case TileType.Narrow:
                    return _narrow;
                case TileType.Building:
                    return male ? _buildingMale : _buildingFemale;
                case TileType.Door:
                    return male ? _doorMale : _doorFemale;
                default:
                    return _floor;
            }
        }
    }
}
