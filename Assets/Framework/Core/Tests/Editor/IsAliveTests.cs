using NUnit.Framework;
using UnityEngine;

namespace AF.Core.Tests
{
    public class IsAliveTests
    {
        [Test]
        public void DestroyedComponent_IsNotAlive_ThoughNotNull()
        {
            var gameObject = new GameObject(nameof(IsAliveTests));
            object component = gameObject.transform;
            Assert.IsTrue(component.IsAlive());

            Object.DestroyImmediate(gameObject);

            Assert.IsFalse(component.IsAlive());
            Assert.IsTrue(component != null, "The trap IsAlive exists for: a plain C# null check still passes.");
        }

        [Test]
        public void PlainObjects()
        {
            Assert.IsTrue(new object().IsAlive());
            Assert.IsFalse(((object)null).IsAlive());
        }
    }
}
