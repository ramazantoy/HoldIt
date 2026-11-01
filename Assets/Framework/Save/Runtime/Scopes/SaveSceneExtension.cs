using AF.Core;
using VContainer;
using VContainer.Unity;

namespace AF.Save
{
    /// <summary>Links the savables of every scene scope to the save registry.</summary>
    internal sealed class SaveSceneExtension : ISceneScopeExtension
    {
        public void Configure(IContainerBuilder builder, LifetimeScope scope) => SaveInstaller.LinkSavables(builder, scope);
    }
}
