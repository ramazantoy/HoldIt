using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace AF.Core.Tests
{
    public class ScopeFieldAuditTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var gameObject in _created)
                Object.DestroyImmediate(gameObject);
            _created.Clear();
        }

        [Test]
        public void UnregisteredInjectField_IsReported()
        {
            var scope = CreateScope();

            var problems = ScopeFieldAudit.FindUnregistered(new ContainerBuilder(), scope, null);

            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains(nameof(FieldScope.target), problems[0]);
        }

        [Test]
        public void RegisteredInjectField_IsNotReported()
        {
            var scope = CreateScope();
            var builder = new ContainerBuilder();
            builder.RegisterComponent(scope.target);

            CollectionAssert.IsEmpty(ScopeFieldAudit.FindUnregistered(builder, scope, null));
        }

        [Test]
        public void InjectFieldUnderAutoInjectGameObject_IsNotReported()
        {
            var scope = CreateScope();

            CollectionAssert.IsEmpty(ScopeFieldAudit.FindUnregistered(new ContainerBuilder(), scope, new[] { scope.target.gameObject }));
        }

        [Test]
        public void PrefabAssetInField_IsNotReported()
        {
            const string PATH = "Assets/ScopeFieldAuditTests.prefab";
            var source = new GameObject("Prefab");
            _created.Add(source);
            source.AddComponent<InjectTarget>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(source, PATH);
            try
            {
                var scope = CreateScope();
                scope.target = prefab.GetComponent<InjectTarget>();

                CollectionAssert.IsEmpty(ScopeFieldAudit.FindUnregistered(new ContainerBuilder(), scope, null),
                    "A prefab is copied and injected by whoever instantiates it, a pool for example.");
            }
            finally
            {
                AssetDatabase.DeleteAsset(PATH);
            }
        }

        private FieldScope CreateScope()
        {
            var root = new GameObject("Scope");
            _created.Add(root);
            var scope = root.AddComponent<FieldScope>();

            var target = new GameObject("Target");
            target.transform.SetParent(root.transform);
            scope.target = target.AddComponent<InjectTarget>();
            return scope;
        }
    }
}
