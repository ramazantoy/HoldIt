using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AF.Core.Editor;
using AF.Input.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using Object = UnityEngine.Object;

namespace AF.Input.Tests
{
    public class InputValidatorTests
    {
        private const string LOCATION = "Test";
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var target in _created)
                Object.DestroyImmediate(target);
            _created.Clear();
        }

        [Test]
        public void ProjectWideActions_AreAnError()
        {
            var report = new ValidationReport();
            InputSetupValidator.CheckProjectWideActions(null, report);
            Assert.AreEqual(0, report.ErrorCount, report.ToString());

            InputSetupValidator.CheckProjectWideActions(Track(TestActions.Create()), report);
            Assert.AreEqual(1, report.ErrorCount, report.ToString());
            StringAssert.Contains("Project-wide Actions", report.Issues[0].Message);
        }

        [Test]
        public void FixedUpdateMode_IsAnError()
        {
            var settings = Track(ScriptableObject.CreateInstance<InputSettings>());
            var report = new ValidationReport();
            InputSetupValidator.CheckUpdateMode(settings, report);
            Assert.AreEqual(0, report.ErrorCount, report.ToString());

            settings.updateMode = InputSettings.UpdateMode.ProcessEventsInFixedUpdate;
            InputSetupValidator.CheckUpdateMode(settings, report);
            Assert.AreEqual(1, report.ErrorCount, report.ToString());
        }

        [Test]
        public void EventSystemInAScene_IsAnError()
        {
            var root = Create("Root");
            var eventSystem = Create("EventSystem", root);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            var report = new ValidationReport();
            InputSetupValidator.CheckNoEventSystem(new[] { root }, LOCATION, report);

            Assert.AreEqual(2, report.ErrorCount, report.ToString());
            StringAssert.Contains("lives on the root prefab", report.Issues[0].Message);
        }

        [Test]
        public void PlayerInput_IsAnError()
        {
            var root = Create("Root");
            Create("Player", root).AddComponent<PlayerInput>();
            Create("Players", root).AddComponent<PlayerInputManager>();

            var report = new ValidationReport();
            InputSetupValidator.CheckNoPlayerInput(new[] { root }, LOCATION, report);

            Assert.AreEqual(2, report.ErrorCount, report.ToString());
            StringAssert.Contains(nameof(PlayerInput), report.Issues[0].Message);
            StringAssert.Contains(nameof(PlayerInputManager), report.Issues[1].Message);
        }

        [Test]
        public void PrefabWithAnEventSystemOrPlayerInput_IsAnError()
        {
            var prefab = Create("Menu");
            var eventSystem = Create("EventSystem", prefab);
            eventSystem.AddComponent<EventSystem>();
            Create("Player", prefab).AddComponent<PlayerInput>();

            var report = new ValidationReport();
            InputSetupValidator.CheckPrefab(prefab, LOCATION, report);

            Assert.AreEqual(2, report.ErrorCount, report.ToString());
            StringAssert.Contains("lives on the root prefab", report.Issues[0].Message);
            StringAssert.Contains(nameof(PlayerInput), report.Issues[1].Message);
        }

        [Test]
        public void RootWithTwoEventSystems_IsAnError()
        {
            var root = Create("Root");
            Create("EventSystem", root).AddComponent<EventSystem>();
            Create("Second", root).AddComponent<EventSystem>();

            var report = new ValidationReport();
            InputSetupValidator.CheckRoot(root, LOCATION, report);

            StringAssert.Contains("2 EventSystems; the root prefab has exactly one", report.ToString());
        }

        [Test]
        public void RootWithoutEventSystem_OrWithAnotherModule_IsAnError()
        {
            var root = Create("Root");
            var report = new ValidationReport();
            InputSetupValidator.CheckRoot(root, LOCATION, report);
            Assert.AreEqual(1, report.ErrorCount, report.ToString());
            StringAssert.Contains("no EventSystem", report.Issues[0].Message);

            var eventSystem = Create("EventSystem", root);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
            report = new ValidationReport();
            InputSetupValidator.CheckRoot(root, LOCATION, report);

            Assert.AreEqual(2, report.ErrorCount, report.ToString());
            StringAssert.Contains("only InputSystemUIInputModule", report.Issues[0].Message);
            StringAssert.Contains("has no InputSystemUIInputModule", report.Issues[1].Message);
        }

        [Test]
        public void UiModule_MustUseTheRootActions_WithEveryRequiredAction()
        {
            var actions = Track(TestActions.Create());
            var other = Track(TestActions.Create());
            // Inactive: assigning actions to an active module enables them, even in Edit Mode.
            var host = Create("EventSystem");
            host.SetActive(false);
            var module = host.AddComponent<InputSystemUIInputModule>();
            var report = new ValidationReport();
            InputSetupValidator.CheckModule(module, actions, LOCATION, report);
            StringAssert.Contains("uses DefaultInputActions", report.Issues[0].Message, "Added in the Editor, the module takes Unity's default actions.");

            module.UnassignActions();
            report = new ValidationReport();
            InputSetupValidator.CheckModule(module, actions, LOCATION, report);
            Assert.AreEqual(1, report.ErrorCount, report.ToString());
            StringAssert.Contains("Actions Asset is empty", report.Issues[0].Message);

            module.actionsAsset = other;
            report = new ValidationReport();
            InputSetupValidator.CheckModule(module, actions, LOCATION, report);
            StringAssert.Contains("but the root scope uses", report.Issues[0].Message);
            Assert.AreEqual(7, report.ErrorCount, "The asset mismatch and six unassigned actions.\n" + report);

            module.actionsAsset = actions;
            module.point = Reference(actions, "UI/Point");
            module.leftClick = Reference(actions, "UI/Click");
            module.move = Reference(actions, "UI/Navigate");
            module.submit = Reference(actions, "UI/Submit");
            module.cancel = Reference(actions, "UI/Cancel");
            module.scrollWheel = Reference(other, "UI/ScrollWheel");
            report = new ValidationReport();
            InputSetupValidator.CheckModule(module, actions, LOCATION, report);
            Assert.AreEqual(1, report.ErrorCount, report.ToString());
            StringAssert.Contains("Scroll Wheel action comes from", report.Issues[0].Message);

            module.scrollWheel = Reference(actions, "UI/ScrollWheel");
            report = new ValidationReport();
            InputSetupValidator.CheckModule(module, actions, LOCATION, report);
            Assert.AreEqual(0, report.ErrorCount, report.ToString());
        }

        [Test]
        public void DirectToggles_AreFoundInCompiledCode()
        {
            var report = new ValidationReport();
            DirectInputToggleValidator.Check(WithNestedTypes(typeof(DirectToggles)), report);

            var messages = string.Join("\n", report.Issues.Select(issue => issue.Message));
            Assert.AreEqual(7, report.ErrorCount, messages);
            StringAssert.Contains("DirectToggles.EnableMap calls InputActionMap.Enable()", messages);
            StringAssert.Contains("DirectToggles.EnableAsset calls InputActionAsset.Enable()", messages);
            StringAssert.Contains("DirectToggles.LockCursor calls Cursor.lockState", messages);
            StringAssert.Contains("DirectToggles.SwitchMap calls PlayerInput.SwitchCurrentActionMap()", messages);
            StringAssert.Contains("DirectToggles.ShowCursor calls Cursor.visible", messages);
            StringAssert.Contains("DirectToggles.DisableLater calls InputAction.Disable()", messages);
            StringAssert.Contains("DirectToggles.EnableAll calls IInputActionCollection.Enable()", messages);
        }

        [Test]
        public void ExemptAndCleanCode_IsNotReported()
        {
            var report = new ValidationReport();
            DirectInputToggleValidator.Check(WithNestedTypes(typeof(ExemptToggles), typeof(ExemptMethods), typeof(CleanInput)), report);

            Assert.AreEqual(0, report.ErrorCount, report.ToString());
        }

        private static IEnumerable<Type> WithNestedTypes(params Type[] types) =>
            types.SelectMany(type => new[] { type }.Concat(WithNestedTypes(type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))));

        private InputActionReference Reference(InputActionAsset asset, string action) => Track(InputActionReference.Create(asset.FindAction(action, true)));

        private T Track<T>(T target) where T : Object
        {
            _created.Add(target);
            return target;
        }

        private GameObject Create(string name, GameObject parent = null)
        {
            var gameObject = new GameObject(name);
            if (parent != null)
                gameObject.transform.SetParent(parent.transform);
            else
                _created.Add(gameObject);
            return gameObject;
        }

        // Only read by DirectInputToggleValidator, never run.
        private static class DirectToggles
        {
            public static void EnableMap(InputActionMap map) => map.Enable();

            public static void ShowCursor() => Cursor.visible = true;

            public static Action DisableLater(InputAction action) => () => action.Disable();

            public static void EnableAll(IInputActionCollection actions) => actions.Enable();

            public static void EnableAsset(InputActionAsset asset) => asset.Enable();

            public static void LockCursor() => Cursor.lockState = CursorLockMode.Locked;

            public static void SwitchMap(PlayerInput player) => player.SwitchCurrentActionMap("UI");
        }

        [InputModeExempt("test: a standalone debug action")]
        private static class ExemptToggles
        {
            public static void EnableMap(InputActionMap map) => map.Enable();

            public static Action DisableLater(InputAction action) => () => action.Disable();
        }

        private static class ExemptMethods
        {
            [InputModeExempt("test")]
            public static void EnableMap(InputActionMap map) => map.Enable();

            [InputModeExempt("test")]
            public static Action DisableLater(InputAction action) => () => action.Disable();

            [InputModeExempt("test")]
            public static Func<Action> DisableNested(InputAction action) => () => () => action.Disable();
        }

        private static class CleanInput
        {
            public static float Read(InputAction action) => action.ReadValue<float>();

            public static bool IsVisible() => Cursor.visible;
        }
    }
}
