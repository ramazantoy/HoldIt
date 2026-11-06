using UnityEngine;

namespace HoldIt.Characters
{
    /// <summary>
    /// The limbs' offsets from their rest pose at one moment of the walk cycle, or seated (<see cref="Sitting"/>). The legs and arms swing about X, the body
    /// rises and rocks about Z. The three peaks fall at different moments: the stride at a quarter cycle, the rise while
    /// the legs pass each other, the rock in between.
    /// </summary>
    public readonly struct WalkPose
    {
        /// <summary>Degrees about X; positive swings the foot forward.</summary>
        public readonly float LegLeft;

        public readonly float LegRight;

        /// <summary>Degrees about X; positive swings the hand forward.</summary>
        public readonly float ArmLeft;

        public readonly float ArmRight;

        /// <summary>How far the body rises, in tiles.</summary>
        public readonly float BodyRise;

        /// <summary>Degrees about Z.</summary>
        public readonly float BodyRock;

        private WalkPose(float legLeft, float legRight, float armLeft, float armRight, float bodyRise, float bodyRock)
        {
            LegLeft = legLeft;
            LegRight = legRight;
            ArmLeft = armLeft;
            ArmRight = armRight;
            BodyRise = bodyRise;
            BodyRock = bodyRock;
        }

        /// <param name="phase">Where in the cycle, in cycles: 0 and 0.5 have the legs side by side, 0.25 the left one ahead.</param>
        /// <param name="blend">0 is the rest pose, 1 the full walk.</param>
        public static WalkPose At(float phase, float blend, WalkStyle style)
        {
            blend = Mathf.Clamp01(blend);
            var angle = phase * 2f * Mathf.PI;
            var stride = Mathf.Sin(angle);
            var legs = stride * style.LegSwing * blend;
            var arms = stride * style.ArmSwing * blend;
            var rise = (Mathf.Cos(2f * angle) + 1f) * 0.5f * style.Bob * blend;
            var rock = Mathf.Sin(angle + Mathf.PI * 0.25f) * style.Sway * blend;
            return new WalkPose(legs, -legs, -arms, arms, rise, rock);
        }

        /// <summary>The same pose with the right arm at <paramref name="degrees"/> instead (positive swings the hand forward).</summary>
        public WalkPose WithArmRight(float degrees) => new WalkPose(LegLeft, LegRight, ArmLeft, degrees, BodyRise, BodyRock);

        /// <summary>
        /// Seated: both legs swung forward by the style's angle, arms and body at rest. <paramref name="blend"/> 0 is the rest
        /// pose, 1 fully seated.
        /// </summary>
        public static WalkPose Sitting(float blend, SitStyle style)
        {
            var legs = style.LegAngle * Mathf.Clamp01(blend);
            return new WalkPose(legs, legs, 0f, 0f, 0f, 0f);
        }
    }
}
