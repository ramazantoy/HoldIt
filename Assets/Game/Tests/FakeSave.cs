using System;
using System.Collections.Generic;
using System.Threading;
using AF.Save;
using Cysharp.Threading.Tasks;

namespace HoldIt.Tests
{
    /// <summary>Counts the global saves asked for. The game uses no slots; their operations throw.</summary>
    internal sealed class FakeSave : ISaveService
    {
        public int GlobalSaves { get; private set; }

        public LoadStatus GlobalStatus => LoadStatus.Loaded;
        public LoadStatus DeviceStatus => LoadStatus.Loaded;
        public bool HasSession => false;
        public string ActiveSlot => null;
        public bool IsDevSession => false;
        public TimeSpan PlayTime => TimeSpan.Zero;

        public UniTask<SaveResult> SaveGlobal(CancellationToken cancellation = default)
        {
            GlobalSaves++;
            return UniTask.FromResult(new SaveResult(SaveStatus.Written));
        }

        public UniTask<IReadOnlyList<SlotInfo>> ListSlots(CancellationToken cancellation = default) => throw new NotSupportedException();
        public UniTask<LoadResult> LoadSlot(string slotId, CancellationToken cancellation = default) => throw new NotSupportedException();
        public void NewSlot(string slotId) => throw new NotSupportedException();
        public UniTask<SaveResult> SaveSlot(CancellationToken cancellation = default) => throw new NotSupportedException();
        public UniTask<SaveResult> SaveSlot(string slotId, CancellationToken cancellation = default) => throw new NotSupportedException();
        public void CloseSlot() => throw new NotSupportedException();
        public UniTask<SaveResult> DeleteSlot(string slotId, CancellationToken cancellation = default) => throw new NotSupportedException();
    }
}
