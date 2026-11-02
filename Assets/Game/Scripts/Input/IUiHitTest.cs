using UnityEngine;

namespace HoldIt.Input
{
    /// <summary>Tells whether a screen position is over an interface element that takes the pointer.</summary>
    public interface IUiHitTest
    {
        bool IsOverUi(Vector2 screenPosition);
    }
}
