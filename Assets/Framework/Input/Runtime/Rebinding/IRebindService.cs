using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;

namespace AF.Input
{
    public enum RebindResult
    {
        /// <summary>The binding now uses the control the player actuated.</summary>
        Completed,

        /// <summary>The player pressed a cancel control; the binding is unchanged.</summary>
        Canceled,

        /// <summary>Nothing was actuated in time; the binding is unchanged.</summary>
        TimedOut,
    }

    /// <summary>
    /// Changes bindings as player overrides on the actions asset. Overrides are saved with the global save data (key
    /// <c>input.bindings</c>); writing them is the game's call (<c>ISaveService.SaveGlobal</c> when the settings screen closes),
    /// and suspending or quitting writes them if they changed.
    /// </summary>
    public interface IRebindService
    {
        bool IsRebinding { get; }

        /// <summary>
        /// Waits for the player to actuate a control and binds it to <c>action.bindings[bindingIndex]</c>. Every map is disabled
        /// meanwhile, so the control triggers nothing. Only devices of the binding's control scheme are accepted; the cancel
        /// controls of <see cref="RebindOptions"/> end it. For a composite, rebind each part. Cancelling the token throws
        /// <see cref="System.OperationCanceledException"/>.
        /// </summary>
        UniTask<RebindResult> Rebind(InputAction action, int bindingIndex, CancellationToken cancellation = default);

        /// <summary>Removes the player's override of one binding; for a composite, of all its parts.</summary>
        void Reset(InputAction action, int bindingIndex);

        /// <summary>Removes every override: the asset's own bindings apply again.</summary>
        void ResetAll();
    }
}
