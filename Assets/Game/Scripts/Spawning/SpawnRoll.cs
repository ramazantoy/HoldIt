using System;
using HoldIt.Characters;
using HoldIt.Levels;

namespace HoldIt.Spawning
{
    /// <summary>Who comes in next: a gender by the share of men, a body size by the level's weights.</summary>
    public static class SpawnRoll
    {
        public static Gender Gender(Random random, float maleShare) =>
            random.NextDouble() < maleShare ? HoldIt.Gender.Male : HoldIt.Gender.Female;

        /// <summary>Each size in proportion to its weight; Normal when every weight is 0.</summary>
        public static BodySize Size(Random random, SizeWeights weights)
        {
            var total = weights.Total;
            if (total <= 0f)
                return BodySize.Normal;

            var roll = random.NextDouble() * total;
            if (roll < weights.Thin)
                return BodySize.Thin;
            return roll < weights.Thin + weights.Normal ? BodySize.Normal : BodySize.Fat;
        }
    }
}
