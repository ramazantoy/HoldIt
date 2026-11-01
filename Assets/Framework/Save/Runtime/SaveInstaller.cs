using AF.Core;
using VContainer;
using VContainer.Unity;

namespace AF.Save
{
    public static class SaveInstaller
    {
        /// <summary>
        /// Registers the save system in the root scope. Call it early in <c>ConfigureGame</c>: its boot step loads global data,
        /// and boot steps run in registration order, so later steps (settings) can read that data.
        /// </summary>
        public static void UseSave(this IContainerBuilder builder, SaveOptions options = null)
        {
            options ??= new SaveOptions();
            builder.RegisterInstance(options);
            builder.RegisterInstance(options.Storage ?? new FileSaveStorage());
            builder.Register<SaveRegistry>(Lifetime.Singleton).As<ISaveRegistry>().AsSelf();
            builder.Register<SaveService>(Lifetime.Singleton).As<ISaveService>().AsSelf();
            builder.Register<SaveBootStep>(Lifetime.Singleton).As<IBootStep>();
            builder.RegisterEntryPoint<SaveLifecycle>();
            builder.Register<SaveSceneExtension>(Lifetime.Singleton).As<ISceneScopeExtension>();
            LinkSavables(builder, builder.ApplicationOrigin as LifetimeScope);
        }

        /// <summary>Links the savables registered in this builder's scope once it is built, and checks how they are registered.</summary>
        internal static void LinkSavables(IContainerBuilder builder, LifetimeScope scope)
        {
            SavableAudit.Attach(builder, scope);
            builder.Register<SavableScopeLink>(Lifetime.Singleton);
            builder.RegisterBuildCallback(container => container.Resolve<SavableScopeLink>());
        }
    }
}
