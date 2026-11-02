using System;
using AF.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace HoldIt.Board
{
    /// <summary>
    /// The prefab behind every <see cref="PieceKind"/> (wall blocks one per <see cref="WallShape"/>), and the base the board
    /// stands on. A piece's root sits at its tile's centre on the floor (y = 0); edge pieces are modelled on the tile's north
    /// edge (+Z). Floors are 0.1 thick, so their bottom is at y = -0.1. Swapping a prefab here restyles every map.
    /// </summary>
    [CreateAssetMenu(menuName = "HoldIt/Board/Tile Set", fileName = "TileSet")]
    public sealed class TileSet : ScriptableObject
    {
        [SerializeField, Required] private GameObject _floor;
        [SerializeField, Required] private GameObject _lobbyFloor;
        [SerializeField, Required] private GameObject _entranceFloor;

        [SerializeField, Required, FormerlySerializedAs("_wallBlock")]
        private GameObject _wallBlockSingle;

        [SerializeField, Required] private GameObject _wallBlockEnd;
        [SerializeField, Required] private GameObject _wallBlockStraight;
        [SerializeField, Required] private GameObject _wallBlockCorner;
        [SerializeField, Required] private GameObject _wallBlockT;
        [SerializeField, Required] private GameObject _wallBlockCross;
        [SerializeField, Required] private GameObject _narrowPosts;
        [SerializeField, Required] private GameObject _buildingFloorMale;
        [SerializeField, Required] private GameObject _buildingFloorFemale;
        [SerializeField, Required] private GameObject _buildingWallMale;
        [SerializeField, Required] private GameObject _buildingWallFemale;
        [SerializeField, Required] private GameObject _buildingCornerMale;
        [SerializeField, Required] private GameObject _buildingCornerFemale;
        [SerializeField, Required] private GameObject _doorwayMale;
        [SerializeField, Required] private GameObject _doorwayFemale;

        [SerializeField, Required, Tooltip("A 1 x 1 x 1 block with its root at the centre of its top face; scaled to the board.")]
        private GameObject _baseSlab;

        [SerializeField, Min(0f), Tooltip("How far the base reaches past the map on every side, in tiles.")]
        private float _baseMargin = 0.4f;

        [SerializeField, Min(0.01f), Tooltip("The base's height below the floor pieces.")]
        private float _baseDepth = 0.6f;

        [SerializeField, Tooltip("Height of the base's top: the floor pieces' bottom.")]
        private float _baseTop = -0.1f;

        public GameObject BaseSlab => _baseSlab;
        public float BaseMargin => _baseMargin;
        public float BaseDepth => _baseDepth;
        public float BaseTop => _baseTop;

        public GameObject Prefab(PiecePlacement piece) => piece.Kind switch
        {
            PieceKind.Floor => _floor,
            PieceKind.LobbyFloor => _lobbyFloor,
            PieceKind.EntranceFloor => _entranceFloor,
            PieceKind.WallBlock => WallBlock(piece.Shape),
            PieceKind.NarrowPosts => _narrowPosts,
            PieceKind.BuildingFloor => Gendered(piece, _buildingFloorMale, _buildingFloorFemale),
            PieceKind.BuildingWall => Gendered(piece, _buildingWallMale, _buildingWallFemale),
            PieceKind.BuildingCorner => Gendered(piece, _buildingCornerMale, _buildingCornerFemale),
            PieceKind.Doorway => Gendered(piece, _doorwayMale, _doorwayFemale),
            _ => throw new ArgumentOutOfRangeException(nameof(piece), piece.Kind, null),
        };

        private GameObject WallBlock(WallShape? shape) => shape switch
        {
            WallShape.Single => _wallBlockSingle,
            WallShape.End => _wallBlockEnd,
            WallShape.Straight => _wallBlockStraight,
            WallShape.Corner => _wallBlockCorner,
            WallShape.T => _wallBlockT,
            WallShape.Cross => _wallBlockCross,
            null => throw new ArgumentException($"{PieceKind.WallBlock} needs a shape.", nameof(shape)),
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
        };

        private static GameObject Gendered(PiecePlacement piece, GameObject male, GameObject female)
        {
            if (!piece.Gender.HasValue)
                throw new ArgumentException($"{piece.Kind} needs a gender.", nameof(piece));
            return piece.Gender.Value == Gender.Male ? male : female;
        }
    }
}
