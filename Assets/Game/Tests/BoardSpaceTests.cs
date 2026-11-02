using HoldIt.Board;
using NUnit.Framework;
using UnityEngine;

namespace HoldIt.Tests
{
    public class BoardSpaceTests
    {
        private readonly BoardSpace _space = new BoardSpace(10, 14);

        [Test]
        public void CellCenter_CentresTheBoardWithRowZeroFarthest()
        {
            Assert.AreEqual(new Vector3(-4.5f, 0f, 6.5f), _space.CellCenter(new Cell(0, 0)));
            Assert.AreEqual(new Vector3(4.5f, 0f, -6.5f), _space.CellCenter(new Cell(9, 13)));
            Assert.AreEqual(new Vector3(0.5f, 0f, -0.5f), _space.CellCenter(new Cell(5, 7)));
        }

        [Test]
        public void TryGetCell_FindsEveryCellNearItsEdges()
        {
            for (var row = 0; row < 14; row++)
            {
                for (var col = 0; col < 10; col++)
                {
                    var cell = new Cell(col, row);
                    var center = _space.CellCenter(cell);
                    foreach (var offset in new[] { Vector3.zero, new Vector3(-0.49f, 3f, 0.49f), new Vector3(0.49f, 0f, -0.49f) })
                    {
                        Assert.IsTrue(_space.TryGetCell(center + offset, out var found), $"{cell} + {offset}");
                        Assert.AreEqual(cell, found, $"{cell} + {offset}");
                    }
                }
            }
        }

        [TestCase(-5.01f, 0f)]
        [TestCase(5.01f, 0f)]
        [TestCase(0f, 7.01f)]
        [TestCase(0f, -7.01f)]
        public void TryGetCell_OffTheBoard_IsFalse(float x, float z)
        {
            Assert.IsFalse(_space.TryGetCell(new Vector3(x, 0f, z), out _));
        }

        [Test]
        public void TryHitFloor_FindsWhereARayMeetsTheFloor()
        {
            var ray = new Ray(new Vector3(1f, 10f, -5f), new Vector3(0f, -1f, 1f));

            Assert.IsTrue(BoardSpace.TryHitFloor(ray, out var point));
            Assert.AreEqual(0f, point.y, 1e-5f);
            Assert.AreEqual(1f, point.x, 1e-5f);
            Assert.AreEqual(5f, point.z, 1e-4f);
        }

        [Test]
        public void TryHitFloor_ParallelOrAwayFromTheFloor_IsFalse()
        {
            Assert.IsFalse(BoardSpace.TryHitFloor(new Ray(new Vector3(0f, 1f, 0f), Vector3.forward), out _));
            Assert.IsFalse(BoardSpace.TryHitFloor(new Ray(new Vector3(0f, 1f, 0f), Vector3.up), out _));
        }
    }
}
