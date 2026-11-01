using System;
using System.Collections.Generic;

namespace AF.Save
{
    /// <summary>
    /// Ties the savables registered in one scope to the save registry for the scope's lifetime. Created when the scope is
    /// built, so its savables are restored before the scope's entry points initialize; disposed with the scope, capturing
    /// their state into the loaded document first.
    /// </summary>
    internal sealed class SavableScopeLink : IDisposable
    {
        private readonly List<IDisposable> _registrations = new List<IDisposable>();

        public SavableScopeLink(IReadOnlyList<ISavable> savables, ISaveRegistry registry)
        {
            try
            {
                foreach (var savable in savables)
                {
                    // In a child scope the collection also returns the parent's savables; the parent's link owns those.
                    if (!registry.IsRegistered(savable))
                        _registrations.Add(registry.Register(savable));
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            for (var i = _registrations.Count - 1; i >= 0; i--)
                _registrations[i].Dispose();
            _registrations.Clear();
        }
    }
}
