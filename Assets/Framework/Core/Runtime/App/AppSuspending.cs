namespace AF.Core
{
    public enum SuspendReason
    {
        /// <summary>
        /// <c>OnApplicationPause(true)</c>: the app went to the background on mobile, where it may be killed without notice.
        /// With Run In Background off, Unity's documentation says it also comes when Play mode in the Editor loses focus,
        /// and when a windowed desktop player does.
        /// </summary>
        Background,
        Quit,
    }

    /// <summary>
    /// Published by the root scope when the app goes to the background or quits. The process may be frozen or killed right
    /// after the publish returns, so handlers finish their work synchronously: nothing awaited here is sure to complete.
    /// </summary>
    public readonly struct AppSuspending
    {
        public readonly SuspendReason Reason;

        public AppSuspending(SuspendReason reason) => Reason = reason;
    }
}
