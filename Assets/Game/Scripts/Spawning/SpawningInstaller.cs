using System;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Spawning
{
    public static class SpawningInstaller
    {
        /// <summary>Registers the spawner of the scene's level; after UseCharacters and UseLevelRun.</summary>
        public static void UseSpawning(this IContainerBuilder builder, SpawnConfig config)
        {
            builder.RegisterInstance(config);
            builder.RegisterEntryPoint<Spawner>().WithParameter(new Random());
        }
    }
}
