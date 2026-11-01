using UnityEngine;
using VContainer;

namespace AF.Core.Tests
{
    /// <summary>Test component with an [Inject] method.</summary>
    [AddComponentMenu("")]
    public sealed class InjectTarget : MonoBehaviour
    {
        [Inject]
        public void Construct(IPauseService pause)
        {
        }
    }
}
