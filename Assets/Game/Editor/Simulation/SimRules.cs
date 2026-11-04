using System;
using HoldIt.Characters;
using HoldIt.Levels;
using HoldIt.Restrooms;
using HoldIt.Spawning;
using UnityEditor;
using UnityEngine;

namespace HoldIt.Editor.Simulation
{
    /// <summary>The game's tuning a simulated level is played with: the same config assets the gameplay scene uses.</summary>
    public sealed class SimRules
    {
        public SimRules(CharacterConfig characters, RestroomConfig restrooms, SpawnConfig spawning, RunConfig run)
        {
            Characters = characters ? characters : throw new ArgumentNullException(nameof(characters));
            Restrooms = restrooms ? restrooms : throw new ArgumentNullException(nameof(restrooms));
            Spawning = spawning ? spawning : throw new ArgumentNullException(nameof(spawning));
            Run = run ? run : throw new ArgumentNullException(nameof(run));
        }

        public CharacterConfig Characters { get; }
        public RestroomConfig Restrooms { get; }
        public SpawnConfig Spawning { get; }
        public RunConfig Run { get; }

        /// <summary>The project's config assets; throws when one is missing.</summary>
        public static SimRules FromProject() => new SimRules(Find<CharacterConfig>(), Find<RestroomConfig>(),
            Find<SpawnConfig>(), Find<RunConfig>());

        public static T Find<T>() where T : ScriptableObject
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets" }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                    return asset;
            }
            throw new InvalidOperationException($"There is no {typeof(T).Name} asset in the project.");
        }
    }
}
