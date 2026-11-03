using UnityEngine;

namespace HoldIt.Characters
{
    /// <summary>
    /// A character prefab's own local pose and size of its moving parts, which the walk, the sitting and the shrinking are
    /// put on top of. Anchor_Top is left alone, so the bubble and the bar keep their place and size.
    /// </summary>
    internal struct RestPose
    {
        private Vector3 _body;
        private Quaternion _bodyRotation;
        private Vector3 _bodyScale;
        private Quaternion _legLeft;
        private Quaternion _legRight;
        private Vector3 _legLeftScale;
        private Vector3 _legRightScale;
        private Quaternion _armLeft;
        private Quaternion _armRight;

        public static RestPose Of(CharacterRig rig) => new RestPose
        {
            _body = rig.Body.localPosition,
            _bodyRotation = rig.Body.localRotation,
            _bodyScale = rig.Body.localScale,
            _legLeft = rig.LegLeft.localRotation,
            _legRight = rig.LegRight.localRotation,
            _legLeftScale = rig.LegLeft.localScale,
            _legRightScale = rig.LegRight.localScale,
            _armLeft = rig.ArmLeft.localRotation,
            _armRight = rig.ArmRight.localRotation,
        };

        /// <summary>
        /// Forward is +Z, and a positive X rotation swings a hanging limb back, so the pose's angles flip.
        /// <paramref name="shake"/> moves the body sideways, in tiles.
        /// </summary>
        public void Apply(CharacterRig rig, WalkPose pose, float shake)
        {
            rig.Body.localPosition = _body + Vector3.up * pose.BodyRise + Vector3.right * shake;
            rig.Body.localRotation = _bodyRotation * Quaternion.Euler(0f, 0f, pose.BodyRock);
            rig.LegLeft.localRotation = _legLeft * Quaternion.Euler(-pose.LegLeft, 0f, 0f);
            rig.LegRight.localRotation = _legRight * Quaternion.Euler(-pose.LegRight, 0f, 0f);
            rig.ArmLeft.localRotation = _armLeft * Quaternion.Euler(-pose.ArmLeft, 0f, 0f);
            rig.ArmRight.localRotation = _armRight * Quaternion.Euler(-pose.ArmRight, 0f, 0f);
        }

        /// <summary>Sizes the body (head and arms with it) and the legs; 1 is their own size.</summary>
        public void Scale(CharacterRig rig, float scale)
        {
            rig.Body.localScale = _bodyScale * scale;
            rig.LegLeft.localScale = _legLeftScale * scale;
            rig.LegRight.localScale = _legRightScale * scale;
        }
    }
}
