using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using AF.Core.Editor;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using PrimeTween;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace AF.Core.Tests
{
    public class ValidatorTests
    {
        private const string LOCATION = "Test";
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var gameObject in _created)
                Object.DestroyImmediate(gameObject);
            _created.Clear();
        }

        [Test]
        public void UnregisteredInjectComponent_IsAnError()
        {
            var root = Create("Root");
            Create("Scope", root).AddComponent<SceneScope>();
            Create("Target", root).AddComponent<InjectTarget>();

            var report = new ValidationReport();
            InjectRegistrationValidator.Check(new[] { root }, LOCATION, report);

            Assert.AreEqual(1, report.ErrorCount, report.ToString());
            StringAssert.Contains(nameof(InjectTarget), report.Issues[0].Message);
        }

        [Test]
        public void InjectComponentUnderAutoInjectGameObject_IsRegistered()
        {
            var root = Create("Root");
            var scope = Create("Scope", root).AddComponent<SceneScope>();
            var target = Create("Target", root);
            Create("Child", target).AddComponent<InjectTarget>();

            using (var serialized = new SerializedObject(scope))
            {
                var list = serialized.FindProperty("autoInjectGameObjects");
                list.arraySize = 1;
                list.GetArrayElementAtIndex(0).objectReferenceValue = target;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            var report = new ValidationReport();
            InjectRegistrationValidator.Check(new[] { root }, LOCATION, report);

            Assert.AreEqual(0, report.ErrorCount, report.ToString());
        }

        [Test]
        public void InjectComponentInScopeField_IsRegistered()
        {
            var root = Create("Root");
            var scope = Create("Scope", root).AddComponent<FieldScope>();
            scope.target = Create("Target", root).AddComponent<InjectTarget>();

            var report = new ValidationReport();
            InjectRegistrationValidator.Check(new[] { root }, LOCATION, report);

            Assert.AreEqual(0, report.ErrorCount, report.ToString());
        }

        [Test]
        public void EmptyRequiredField_IsAnError_AssignedIsNot()
        {
            var root = Create("Root");
            var component = root.AddComponent<RequiredTarget>();

            var report = new ValidationReport();
            ReferenceValidator.Check(new[] { root }, LOCATION, report);
            Assert.AreEqual(1, report.ErrorCount, report.ToString());
            StringAssert.Contains(nameof(RequiredTarget.target), report.Issues[0].Message);

            component.target = root.transform;
            report = new ValidationReport();
            ReferenceValidator.Check(new[] { root }, LOCATION, report);
            Assert.AreEqual(0, report.ErrorCount, report.ToString());
        }

        [Test]
        public void ReferenceToADeletedObject_IsMissing_NotEmpty()
        {
            var root = Create("Root");
            var component = root.AddComponent<RequiredTarget>();
            var deleted = new GameObject("Deleted");
            component.target = deleted.transform;
            Object.DestroyImmediate(deleted);

            var report = new ValidationReport();
            ReferenceValidator.Check(new[] { root }, LOCATION, report);

            Assert.AreEqual(1, report.ErrorCount, report.ToString());
            StringAssert.Contains("points to a deleted object (Missing)", report.Issues[0].Message);
        }

        [Test]
        public void VContainerSettings_MustNameOneRootScope()
        {
            var report = new ValidationReport();
            ScopeSetupValidator.CheckSettings(Array.Empty<VContainerSettings>(), report);
            StringAssert.Contains("No VContainerSettings", report.ToString());

            var settings = ScriptableObject.CreateInstance<VContainerSettings>();
            try
            {
                report = new ValidationReport();
                ScopeSetupValidator.CheckSettings(new[] { settings, settings }, report);
                Assert.AreEqual(2, report.ErrorCount, report.ToString());
                StringAssert.Contains("more than one VContainerSettings", report.ToString());
                StringAssert.Contains("Root Lifetime Scope is empty", report.ToString());

                settings.RootLifetimeScope = Create("Root").AddComponent<FieldScope>();
                report = new ValidationReport();
                ScopeSetupValidator.CheckSettings(new[] { settings }, report);
                Assert.AreEqual(1, report.ErrorCount, report.ToString());
                StringAssert.Contains("does not derive from RootScope", report.ToString());
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Scene_HasExactlyOneSceneScope_AttachedToTheRoot()
        {
            var none = Array.Empty<RootScope>();
            var report = new ValidationReport();
            ScopeSetupValidator.CheckScene(Array.Empty<SceneScope>(), none, LOCATION, report);
            Assert.AreEqual(1, report.ErrorCount, report.ToString());
            StringAssert.Contains("no SceneScope", report.ToString());

            var first = Create("First").AddComponent<SceneScope>();
            report = new ValidationReport();
            ScopeSetupValidator.CheckScene(new[] { first }, none, LOCATION, report);
            Assert.AreEqual(0, report.ErrorCount, report.ToString());

            var second = Create("Second").AddComponent<SceneScope>();
            second.autoRun = false;
            using (var serialized = new SerializedObject(second))
            {
                serialized.FindProperty("parentReference.TypeName").stringValue = typeof(RootScope).FullName;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            report = new ValidationReport();
            ScopeSetupValidator.CheckScene(new[] { first, second }, none, LOCATION, report);
            Assert.AreEqual(3, report.ErrorCount, report.ToString());
            StringAssert.Contains("2 SceneScopes", report.ToString());
            StringAssert.Contains("Parent must be empty", report.ToString());
            StringAssert.Contains("Auto Run is off", report.ToString());
        }

        [Test]
        public void DirectSceneLoads_AreFoundInCompiledCode()
        {
            var report = new ValidationReport();
            DirectSceneLoadValidator.Check(WithNestedTypes(typeof(DirectSceneLoads)), report);

            var messages = string.Join("\n", report.Issues.Select(issue => issue.Message));
            Assert.AreEqual(4, report.ErrorCount, messages);
            StringAssert.Contains("DirectSceneLoads.LoadMenu calls SceneManager.LoadScene()", messages);
            StringAssert.Contains("DirectSceneLoads.UnloadNow calls SceneManager.UnloadScene()", messages);
            StringAssert.Contains("DirectSceneLoads.LoadLater calls SceneManager.LoadSceneAsync()", messages);
            StringAssert.Contains("DirectSceneLoads.UnloadLater calls SceneManager.UnloadSceneAsync()", messages);
        }

        [Test]
        public void ExemptAndOtherSceneCalls_AreNotReported()
        {
            var report = new ValidationReport();
            DirectSceneLoadValidator.Check(WithNestedTypes(typeof(ExemptSceneLoads), typeof(OtherSceneCalls), typeof(UnitySceneOperations)), report);

            Assert.AreEqual(0, report.ErrorCount, report.ToString());
        }

        [Test]
        public void AwaitedTweensWithoutAToken_AreFoundInCompiledCode()
        {
            var report = new ValidationReport();
            AwaitedTweenValidator.Check(WithNestedTypes(typeof(AwaitedTweens)), report);

            var messages = string.Join("\n", report.Issues.Select(issue => issue.Message));
            Assert.AreEqual(7, report.ErrorCount, messages);
            StringAssert.Contains("AwaitedTweens.AwaitTween calls Tween.GetAwaiter()", messages);
            StringAssert.Contains("AwaitedTweens.AwaitSequence calls Sequence.GetAwaiter()", messages);
            StringAssert.Contains("AwaitedTweens.Convert calls the conversion of a Tween to UniTask", messages);
            StringAssert.Contains("AwaitedTweens.NoToken calls Tween.ToUniTask() (no token)", messages);
            StringAssert.Contains("AwaitedTweens.YieldInstruction calls Tween.ToYieldInstruction()", messages);
            StringAssert.Contains("AwaitedTweens.Coroutine uses a Tween as an IEnumerator", messages);
            StringAssert.Contains("AwaitedTweens.EnumeratorCancellation uses a Tween as an IEnumerator", messages);
        }

        [Test]
        public void TokenAwaitedAndExemptTweens_AreNotReported()
        {
            var report = new ValidationReport();
            AwaitedTweenValidator.Check(WithNestedTypes(typeof(TokenAwaitedTweens), typeof(ExemptAwaitedTweens), typeof(TweenExtensions)), report);

            Assert.AreEqual(0, report.ErrorCount, report.ToString());
        }

        [Test]
        public void CopiesThatNothingInjects_AreFoundInCompiledCode()
        {
            var report = new ValidationReport();
            DirectInstantiateValidator.Check(WithNestedTypes(typeof(Instantiations)), report);

            var messages = string.Join("\n", report.Issues.Select(issue => issue.Message));
            Assert.AreEqual(7, report.ErrorCount, messages);
            StringAssert.Contains("Instantiations.CopyComponent calls Object.Instantiate<Transform>()", messages);
            StringAssert.Contains("Instantiations.AddByType calls GameObject.AddComponent() with a Type", messages);
            StringAssert.Contains("Instantiations.CopyGameObject calls Object.Instantiate<GameObject>()", messages);
            StringAssert.Contains("Instantiations.CopyObject calls Object.Instantiate() with a plain Object", messages);
            StringAssert.Contains("Instantiations.CopyAny calls Object.Instantiate<T>()", messages);
            StringAssert.Contains("Instantiations.CopyLater calls Object.InstantiateAsync<Transform>()", messages);
            StringAssert.Contains("Instantiations.AddInjected calls GameObject.AddComponent<InjectTarget>()", messages);
        }

        [Test]
        public void CopiesOfOtherObjectsAndExemptCopies_AreNotReported()
        {
            var report = new ValidationReport();
            DirectInstantiateValidator.Check(WithNestedTypes(typeof(AllowedInstantiations), typeof(ExemptInstantiations)), report);

            Assert.AreEqual(0, report.ErrorCount, report.ToString());
        }

        private static IEnumerable<Type> WithNestedTypes(params Type[] types) =>
            types.SelectMany(type => new[] { type }.Concat(WithNestedTypes(type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))));

        private GameObject Create(string name, GameObject parent = null)
        {
            var gameObject = new GameObject(name);
            if (parent != null)
                gameObject.transform.SetParent(parent.transform);
            else
                _created.Add(gameObject);
            return gameObject;
        }

        // Only read by DirectSceneLoadValidator, never run.
        private static class DirectSceneLoads
        {
            public static void LoadMenu() => SceneManager.LoadScene("Menu");

            public static AsyncOperation LoadLater(string scene) => SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);

            public static Func<AsyncOperation> UnloadLater(Scene scene) => () => SceneManager.UnloadSceneAsync(scene);

#pragma warning disable CS0618 // obsolete, still a call the validator must catch
            public static bool UnloadNow(string scene) => SceneManager.UnloadScene(scene);
#pragma warning restore CS0618
        }

        [SceneLoadExempt("test")]
        private static class ExemptSceneLoads
        {
            public static void LoadMenu() => SceneManager.LoadScene("Menu");
        }

        private static class OtherSceneCalls
        {
            public static Scene Active() => SceneManager.GetActiveScene();

            public static void Activate(Scene scene) => SceneManager.SetActiveScene(scene);
        }

        // Only read by AwaitedTweenValidator, never run.
        private static class AwaitedTweens
        {
            public static async UniTask AwaitTween(Transform target) => await Tween.PositionX(target, 1f, 1f);

            public static async UniTask AwaitSequence(Transform target) => await Sequence.Create(Tween.PositionX(target, 1f, 1f));

            public static UniTask Convert(Transform target) => Tween.PositionX(target, 1f, 1f);

            public static UniTask NoToken(Transform target) => Tween.PositionX(target, 1f, 1f).ToUniTask();

            public static IEnumerator YieldInstruction(Transform target) => Tween.PositionX(target, 1f, 1f).ToYieldInstruction();

            public static IEnumerator Coroutine(Transform target)
            {
                yield return Tween.PositionX(target, 1f, 1f);
            }

            public static UniTask EnumeratorCancellation(Transform target, CancellationToken cancellation) =>
                Tween.PositionX(target, 1f, 1f).WithCancellation(cancellation);
        }

        private static class TokenAwaitedTweens
        {
            public static async UniTask AwaitTween(Transform target, CancellationToken cancellation) =>
                await Tween.PositionX(target, 1f, 1f).ToUniTask(cancellation);

            public static Tween Fire(Transform target, CancellationToken cancellation) =>
                Tween.PositionX(target, 1f, 1f).SetCancellationToken(cancellation);
        }

        [TweenAwaitExempt("test")]
        private static class ExemptAwaitedTweens
        {
            public static async UniTask AwaitTween(Transform target) => await Tween.PositionX(target, 1f, 1f);
        }

        // Only read by DirectInstantiateValidator, never run.
        private static class Instantiations
        {
            public static Transform CopyComponent(Transform prefab) => Object.Instantiate(prefab);

            public static GameObject CopyGameObject(GameObject prefab) => Object.Instantiate(prefab);

            public static Object CopyObject(Object original) => Object.Instantiate(original);

            public static T CopyAny<T>(T original) where T : Object => Object.Instantiate(original);

            public static AsyncInstantiateOperation<Transform> CopyLater(Transform prefab) => Object.InstantiateAsync(prefab);

            public static InjectTarget AddInjected(GameObject target) => target.AddComponent<InjectTarget>();

            public static Component AddByType(GameObject target, Type type) => target.AddComponent(type);
        }

        private static class AllowedInstantiations
        {
            public static Material CopyMaterial(Material material) => Object.Instantiate(material);

            public static T CopyAsset<T>(T asset) where T : ScriptableObject => Object.Instantiate(asset);

            public static BoxCollider AddPlain(GameObject target) => target.AddComponent<BoxCollider>();
        }

        [InstantiateExempt("test")]
        private static class ExemptInstantiations
        {
            public static Transform CopyComponent(Transform prefab) => Object.Instantiate(prefab);
        }
    }
}
