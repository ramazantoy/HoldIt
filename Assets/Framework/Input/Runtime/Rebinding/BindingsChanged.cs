using UnityEngine.InputSystem;

namespace AF.Input
{
    /// <summary>Published after the player's binding overrides changed: a rebind, a reset, or saved bindings restored.</summary>
    public readonly struct BindingsChanged
    {
        /// <summary>The action whose bindings changed; null when any binding may have changed.</summary>
        public readonly InputAction Action;

        public BindingsChanged(InputAction action) => Action = action;
    }
}
