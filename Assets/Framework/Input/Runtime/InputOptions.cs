using System;
using System.Collections.Generic;
using AF.Core;

namespace AF.Input
{
    /// <summary>Passed to <c>builder.UseInput(actions, messagePipe, options)</c> in the root scope.</summary>
    public sealed class InputOptions
    {
        private readonly Dictionary<Type, InputMode> _stateModes = new Dictionary<Type, InputMode>();

        /// <summary>
        /// Active while the game is paused. It goes above every request made before the pause, so they come back on resume;
        /// requests made during the pause (a settings screen, a rebind) go above it. Usually the menu mode.
        /// </summary>
        public InputMode Paused { get; set; } = InputMode.None;

        public RebindOptions Rebind { get; set; } = new RebindOptions();

        /// <summary>
        /// The mode while <typeparamref name="TState"/> is the current game state and no request is alive. Every registered state
        /// needs one, except <see cref="BootState"/>, which is always <see cref="InputMode.None"/> and throws here; a missing
        /// one fails when the root scope is built.
        /// </summary>
        public InputOptions ForState<TState>(InputMode mode) where TState : IGameState
        {
            if (typeof(TState) == typeof(BootState))
                throw new ArgumentException("Boot always has InputMode.None: nothing takes input before the boot steps are done.");
            _stateModes[typeof(TState)] = mode ?? throw new ArgumentNullException(nameof(mode));
            return this;
        }

        internal IReadOnlyDictionary<Type, InputMode> StateModes => _stateModes;
    }

    public sealed class RebindOptions
    {
        /// <summary>Controls that end a rebind without changing the binding. They can never be bound themselves.</summary>
        public List<string> CancelPaths { get; set; } = new List<string> { "<Keyboard>/escape", "<Gamepad>/start" };

        /// <summary>
        /// Controls that are never bound, in addition to the pointer position and delta controls the Input System always
        /// excludes from interactive rebinds.
        /// </summary>
        public List<string> ExcludedPaths { get; set; } = new List<string>();

        /// <summary>Seconds without a match before the rebind gives up with <see cref="RebindResult.TimedOut"/>; 0 waits forever.</summary>
        public float TimeoutSeconds { get; set; } = 10f;
    }
}
