using System.Threading;
using Cysharp.Threading.Tasks;

namespace AF.Core
{
    /// <summary>
    /// Gate for asynchronous startup work (global save, settings, platform). The root scope is built synchronously;
    /// anything that needs that work done awaits <see cref="WaitReady"/>.
    /// </summary>
    public interface IAppBoot
    {
        bool IsReady { get; }

        /// <summary>Completes when every boot step has finished; throws the step's exception if one failed.</summary>
        UniTask WaitReady(CancellationToken cancellation);
    }

    /// <summary>
    /// One unit of startup work. Register in the root scope with <c>builder.Register&lt;T&gt;(Lifetime.Singleton).As&lt;IBootStep&gt;()</c>;
    /// steps run one after another in registration order.
    /// </summary>
    public interface IBootStep
    {
        UniTask Run(CancellationToken cancellation);
    }
}
