using System;

namespace AF.Core
{
    /// <summary>The entry point exception handler of every scope.</summary>
    internal static class EntryPointErrors
    {
        /// <summary>
        /// Logs an entry point's exception. A cancellation whose token was really cancelled (Play stopped, a scene unloaded
        /// while its async work ran) is the normal end of that work and is not logged; one without a cancelled token is a bug.
        /// </summary>
        public static void Report(Exception exception)
        {
            if (!IsCancellation(exception))
                Log.Exception(exception);
        }

        internal static bool IsCancellation(Exception exception) =>
            exception is OperationCanceledException canceled && canceled.CancellationToken.IsCancellationRequested;
    }
}
