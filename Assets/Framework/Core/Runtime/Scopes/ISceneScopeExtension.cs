using VContainer;
using VContainer.Unity;

namespace AF.Core
{
    /// <summary>
    /// Adds registrations to every <see cref="SceneScope"/>, like an installer the root applies to each scene. A framework
    /// module uses it to hook into scenes without the scene scope knowing the module. Register in the root scope with
    /// <c>builder.Register&lt;T&gt;(Lifetime.Singleton).As&lt;ISceneScopeExtension&gt;()</c>.
    /// </summary>
    /// <remarks>
    /// Runs before the scene's own registrations and entry points. Build callbacks run in registration order, so a callback
    /// added here runs before the scene's <c>IInitializable</c> entry points.
    /// </remarks>
    public interface ISceneScopeExtension
    {
        void Configure(IContainerBuilder builder, LifetimeScope scope);
    }
}
