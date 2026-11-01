using System;
using AF.Save;
using MessagePipe;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace AF.Input
{
    public static class InputInstaller
    {
        /// <summary>
        /// Registers the input system in the root scope: the input mode service, the control scheme tracker and rebinding.
        /// <paramref name="actions"/> is the imported actions asset itself, the one the UI input module on the root prefab uses;
        /// never a copy, such as the one a generated C# class creates. Call it after <c>UseSave</c>: binding overrides are saved
        /// with the global data.
        /// </summary>
        public static void UseInput(this IContainerBuilder builder, InputActionAsset actions, MessagePipeOptions messagePipe,
            InputOptions options = null)
        {
            if (actions == null)
                throw new ArgumentNullException(nameof(actions), "Assign the input actions asset on the root scope.");
            if (!builder.Exists(typeof(ISaveRegistry), includeInterfaceTypes: true))
                throw new InvalidOperationException("Call builder.UseSave() before builder.UseInput(): binding overrides are saved through it.");

            builder.RegisterMessageBroker<InputModeChanged>(messagePipe);
            builder.RegisterMessageBroker<ControlSchemeChanged>(messagePipe);
            builder.RegisterMessageBroker<BindingsChanged>(messagePipe);

            builder.RegisterInstance(actions);
            builder.RegisterInstance(options ?? new InputOptions());
            builder.Register<ControlSchemeTracker>(Lifetime.Singleton).As<IControlSchemeTracker>();
            builder.RegisterEntryPoint<InputModeService>().As<IInputModeService>();
            builder.Register<RebindService>(Lifetime.Singleton).As<IRebindService>();
            builder.Register<InputBindings>(Lifetime.Singleton).AsImplementedInterfaces();
        }
    }
}
