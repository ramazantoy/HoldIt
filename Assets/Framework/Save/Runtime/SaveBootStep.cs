using System.Threading;
using AF.Core;
using Cysharp.Threading.Tasks;

namespace AF.Save
{
    /// <summary>Loads the global and device documents during boot, before any scene leaves the Boot state.</summary>
    internal sealed class SaveBootStep : IBootStep
    {
        private readonly SaveService _save;

        public SaveBootStep(SaveService save) => _save = save;

        public UniTask Run(CancellationToken cancellation) => _save.LoadShared(cancellation);
    }
}
