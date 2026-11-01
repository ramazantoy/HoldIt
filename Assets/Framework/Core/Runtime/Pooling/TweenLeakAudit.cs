using System.Diagnostics;
using PrimeTween;
using UnityEngine;

namespace AF.Core
{
    /// <summary>
    /// Reports tweens that survived their pooled instance's despawn. Tweens on a pooled object stop through the life's token;
    /// one started without it keeps animating the next life, the bug the token rule exists for.
    /// </summary>
    internal static class TweenLeakAudit
    {
        /// <summary>
        /// Checks the instance about to be spawned again. A token-bound tween stops at PrimeTween's next update (FixedUpdate
        /// ones at the next fixed step), so the check waits for two frames and one fixed step after the despawn; while the
        /// game is paused no fixed step runs and the check is skipped.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Check(GameObject instance, string prefabName, FrameStamp despawned, FrameStamp now)
        {
            if (now.Frame < despawned.Frame + 2 || now.FixedTime <= despawned.FixedTime || Tween.GetTweensCount() == 0)
                return;

            var count = 0;
            foreach (var component in instance.GetComponentsInChildren<Component>(true))
            {
                // A missing script.
                if (component == null)
                    continue;
                count += Tween.GetTweensCount(component);
                if (component is Transform)
                    count += Tween.GetTweensCount(component.gameObject);
            }
            if (count > 0)
            {
                Log.Error($"{prefabName}: {count} tween(s) from a previous life still run on the instance being spawned. Bind every " +
                          "tween on a pooled object to its life: .SetCancellationToken(life.Token), or .ToUniTask(life.Token) when awaited.",
                    instance);
            }
        }
    }
}
