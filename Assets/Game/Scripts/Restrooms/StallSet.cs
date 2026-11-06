using AF.Core;
using UnityEngine;

namespace HoldIt.Restrooms
{
    /// <summary>
    /// The prefabs of a restroom's parts and how their views move. Partitions are one unit long along local +Z and are
    /// stretched to their length; every part is modelled facing +Z, out of the restroom.
    /// </summary>
    [CreateAssetMenu(menuName = "HoldIt/Restrooms/Stall Set", fileName = "StallSet")]
    public sealed class StallSet : ScriptableObject
    {
        [SerializeField, Required] private GameObject _partitionMale;
        [SerializeField, Required] private GameObject _partitionFemale;
        [SerializeField, Required] private StallDoorView _doorMale;
        [SerializeField, Required] private StallDoorView _doorFemale;
        [SerializeField, Required] private GameObject _toilet;
        [SerializeField, Required] private StallLampView _lamp;

        [SerializeField, Tooltip("The lamp of a free stall.")]
        private Color _freeColor = new Color32(0x2f, 0xe3, 0x6d, 0xff);

        [SerializeField, Tooltip("The lamp of a taken or shut stall.")]
        private Color _busyColor = new Color32(0xff, 0x3b, 0x3b, 0xff);

        [SerializeField, Range(0f, 1f), Tooltip("How strongly the lamp's light shows on the floor in front of it.")]
        private float _glowAlpha = 0.3f;

        [SerializeField, Range(0f, 120f), Tooltip("How far an open stall door stands out of the stall, in degrees.")]
        private float _doorOpenAngle = 83f;

        [SerializeField, Min(0.1f), Tooltip("How quickly a stall door swings: an exponential rate per second, the larger the sharper.")]
        private float _doorRate = 9f;

        [SerializeField, Min(0f), Tooltip("How much the lamp's bulb swells when its stall changes state (a fraction of its size).")]
        private float _lampPunch = 0.35f;

        [SerializeField, Min(0.01f), Tooltip("How long the bulb's pulse lasts, in seconds.")]
        private float _lampPunchSeconds = 0.25f;

        public float LampPunch => _lampPunch;
        public float LampPunchSeconds => _lampPunchSeconds;
        public GameObject Toilet => _toilet;
        public StallLampView Lamp => _lamp;
        public Color FreeColor => _freeColor;
        public Color BusyColor => _busyColor;
        public float GlowAlpha => _glowAlpha;
        public float DoorOpenAngle => _doorOpenAngle;
        public float DoorRate => _doorRate;

        public GameObject Partition(Gender gender) => gender == Gender.Male ? _partitionMale : _partitionFemale;
        public StallDoorView Door(Gender gender) => gender == Gender.Male ? _doorMale : _doorFemale;
    }
}
