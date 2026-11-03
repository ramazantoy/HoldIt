using AF.Core;
using UnityEngine;

namespace HoldIt.Characters
{
    /// <summary>
    /// The parts of a character prefab, named and nested as in the character models: Leg_L, Leg_R, Body and Anchor_Top
    /// under the root; Head, Arm_L and Arm_R under Body. Each part's pivot is its joint and the character faces +Z. The legs
    /// hang from the root, so the body can bob and slump while the feet stay down. Anchor_Top is a fixed point above the
    /// head for the patience bar and the speech bubble.
    /// </summary>
    public sealed class CharacterRig : MonoBehaviour
    {
        [SerializeField, Required] private Transform _body;
        [SerializeField, Required] private Transform _head;
        [SerializeField, Required] private Transform _armLeft;
        [SerializeField, Required] private Transform _armRight;
        [SerializeField, Required] private Transform _legLeft;
        [SerializeField, Required] private Transform _legRight;
        [SerializeField, Required] private Transform _anchorTop;

        public Transform Body => _body;
        public Transform Head => _head;
        public Transform ArmLeft => _armLeft;
        public Transform ArmRight => _armRight;
        public Transform LegLeft => _legLeft;
        public Transform LegRight => _legRight;
        public Transform AnchorTop => _anchorTop;
    }
}
