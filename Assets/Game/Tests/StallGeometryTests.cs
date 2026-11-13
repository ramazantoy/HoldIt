using HoldIt.Restrooms;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class StallGeometryTests
    {
        [Test]
        public void LaneDepth_IsThePartitionPlusTheDoorsReachPlusTheApproach()
        {
            var geometry = new StallGeometry(2f, 1f, 0.1f, 0.2f, 0.5f, 0.1f, 1f);

            Assert.AreEqual(3.5f, geometry.LaneDepth, 1e-5f);
        }

        [Test]
        public void LaneDepth_Default_IsBeyondAnOpenDoor()
        {
            var geometry = StallGeometry.Default;

            Assert.Greater(geometry.LaneDepth, geometry.StallDepth + geometry.DoorWidth);
        }

        [Test]
        public void MinStallWidth_IsTheDoorWithRoomOnBothSides()
        {
            var geometry = new StallGeometry(1.8f, 0.9f, 0.14f, 0.3f, 0.4f, 0.15f, 0.95f);

            Assert.AreEqual(1.5f, geometry.MinStallWidth, 1e-5f);
        }

        [Test]
        public void Default_EndsInsideBehindThePartition()
        {
            var geometry = StallGeometry.Default;

            Assert.Less(geometry.InsideDepth, geometry.StallDepth);
            Assert.Greater(geometry.StallDepth - geometry.Pass, geometry.InsideDepth);
        }
    }
}
