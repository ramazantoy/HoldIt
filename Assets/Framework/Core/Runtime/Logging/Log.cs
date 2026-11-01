using System;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace AF.Core
{
    /// <summary>
    /// Single logging entry point. Errors and exceptions are always compiled, because the exception in a shipped game's
    /// Player.log is the most valuable data there is. Info and Warning are development traces: compiled in the Editor and
    /// in builds that define SHOW_LOGS.
    /// </summary>
    public static class Log
    {
        [Conditional("UNITY_EDITOR"), Conditional("SHOW_LOGS")]
        public static void Info(string message, Object context = null) => Debug.Log(message, context);

        [Conditional("UNITY_EDITOR"), Conditional("SHOW_LOGS")]
        public static void Warning(string message, Object context = null) => Debug.LogWarning(message, context);

        public static void Error(string message, Object context = null) => Debug.LogError(message, context);

        public static void Exception(Exception exception, Object context = null) => Debug.LogException(exception, context);

        // Unobserved UniTask failures (for example a faulted Forget()) are routed here instead of UniTask's own logging.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InstallHooks()
        {
            UniTaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
            UniTaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private static void OnUnobservedTaskException(Exception exception) => Exception(exception);
    }
}
