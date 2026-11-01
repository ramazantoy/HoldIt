using UnityEngine;

namespace AF.Core.Tests
{
    /// <summary>Test component with a [Required] reference.</summary>
    [AddComponentMenu("")]
    public sealed class RequiredTarget : MonoBehaviour
    {
        [Required] public Transform target;
    }
}
