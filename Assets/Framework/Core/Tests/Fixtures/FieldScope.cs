using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace AF.Core.Tests
{
    /// <summary>Test scope that registers a scene component through a serialized field.</summary>
    [AddComponentMenu("")]
    public sealed class FieldScope : LifetimeScope
    {
        public InjectTarget target;

        protected override void Configure(IContainerBuilder builder) => builder.RegisterComponent(target);
    }
}
