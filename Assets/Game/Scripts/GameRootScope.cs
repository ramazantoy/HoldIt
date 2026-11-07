using AF.Audio;
using AF.Core;
using AF.Input;
using AF.Localization;
using AF.Platform;
using AF.Save;
using AF.Settings;
using HoldIt.Backdrop;
using HoldIt.Feedback;
using HoldIt.Haptics;
using HoldIt.Input;
using HoldIt.Levels;
using HoldIt.Settings;
using MessagePipe;
using PrimeTween;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using VContainer;

namespace HoldIt
{
    /// <summary>
    /// The game's project-wide composition root. It sits on the root scope prefab that VContainerSettings points to.
    /// Register game-wide services and the message types they own here.
    /// </summary>
    public sealed class GameRootScope : RootScope
    {
        [SerializeField, FormerlySerializedAs("inputActions"), Required, Tooltip("The game's actions asset. The UI input module of the EventSystem below uses the same one.")]
        private InputActionAsset _inputActions;

        [SerializeField, Required, Tooltip("The game's mixer. Its groups and volume parameters are listed in AudioChannels.")]
        private AudioMixer _audioMixer;

        [SerializeField, Required, Tooltip("Every level, in the order they are played.")]
        private LevelList _levels;

        [SerializeField, Required, Tooltip("How the phone vibrates.")]
        private HapticsConfig _haptics;

        [SerializeField, Required, Tooltip("The sound and the vibration of every event the game makes heard and felt.")]
        private FeedbackSet _feedback;

        // Room for a board's wave (a tween per piece) so the pool does not grow, and warn, while it plays.
        private const int TweensCapacity = 400;

        protected override void ConfigureGame(IContainerBuilder builder, MessagePipeOptions messagePipe)
        {
            PrimeTweenConfig.SetTweensCapacity(TweensCapacity);
            // First: its boot step loads the global and device data; the settings are applied as they load.
            builder.UseSave();
            builder.UseInput(_inputActions, messagePipe, InputModes.CreateOptions());
            // UI/Cancel is the game's only back action: Escape, and the Android back button.
            builder.UseBackButton(messagePipe);
            builder.UseAudio(_audioMixer, messagePipe, AudioChannels.CreateOptions());
            // After UseSave: its boot step applies the saved language and waits for its tables.
            builder.UseLocalization(messagePipe);
            // Mock in the Editor and in builds without a store; a store backend is passed here.
            builder.UsePlatform(messagePipe);
            // After UseSave, UseAudio and UseInput: display settings, and saving the channel volumes.
            builder.UseSettings(messagePipe);
            // The game's own settings (vibration), next to the language in the global data.
            builder.UseGameSettings();
            // Vibration reads the game settings; feedback pairs a sound with a vibration and plays through the audio.
            builder.UseHaptics(_haptics);
            builder.UseFeedback(_feedback);
            // The levels, and the progress through them in the global data.
            builder.UseLevels(_levels);
            // The backdrop's palette outlasts the gameplay scene, which the menu unloads.
            builder.UseBackdropMemory();
        }
    }
}
