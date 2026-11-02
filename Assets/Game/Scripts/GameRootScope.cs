using AF.Audio;
using AF.Core;
using AF.Input;
using AF.Localization;
using AF.Platform;
using AF.Save;
using AF.Settings;
using MessagePipe;
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

        protected override void ConfigureGame(IContainerBuilder builder, MessagePipeOptions messagePipe)
        {
            // First: its boot step loads the global and device data; the settings are applied as they load.
            builder.UseSave();
            builder.UseInput(_inputActions, messagePipe, InputModes.CreateOptions());
            builder.UseAudio(_audioMixer, messagePipe, AudioChannels.CreateOptions());
            // After UseSave: its boot step applies the saved language and waits for its tables.
            builder.UseLocalization(messagePipe);
            // Mock in the Editor and in builds without a store; a store backend is passed here.
            builder.UsePlatform(messagePipe);
            // After UseSave, UseAudio and UseInput: display settings, and saving the channel volumes.
            builder.UseSettings(messagePipe);
        }
    }
}
