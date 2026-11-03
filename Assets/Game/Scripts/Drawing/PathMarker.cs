using AF.Core;
using UnityEngine;

namespace HoldIt.Drawing
{
    /// <summary>
    /// A flat mark on one cell of a path, in the walker's colour or red where the path cannot end. A stand-in until the
    /// cell effect is chosen (M5).
    /// </summary>
    public sealed class PathMarker : MonoBehaviour
    {
        [SerializeField, Required] private MeshRenderer _renderer;
        [SerializeField, Required] private Material _male;
        [SerializeField, Required] private Material _female;
        [SerializeField, Required] private Material _blocked;

        private MarkerLook _look = (MarkerLook)(-1);

        public void Place(Vector3 position, MarkerLook look)
        {
            transform.position = position;
            if (look == _look)
                return;
            _look = look;
            _renderer.sharedMaterial = look switch
            {
                MarkerLook.Male => _male,
                MarkerLook.Female => _female,
                _ => _blocked,
            };
        }
    }
}
