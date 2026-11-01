using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using AF.Save;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using static AF.Input.Tests.TestActions;

namespace AF.Input.Tests
{
    public class RebindTests : InputTestFixture
    {
        private InputTestContext _context;
        private Keyboard _keyboard;
        private Mouse _mouse;
        private RebindService _rebind;
        private InputAction _jump;

        [SetUp]
        public void SetUpContext()
        {
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.AddDevice<Gamepad>();
            _context = new InputTestContext().Playing();
            _rebind = _context.CreateRebind();
            _jump = _context.Action("Gameplay/Jump");
        }

        [TearDown]
        public void TearDownContext() => _context.Dispose();

        [Test]
        public void Rebind_BindsTheNextControl_WithEveryMapDisabledMeanwhile()
        {
            var task = _rebind.Rebind(_jump, 0);
            Assert.IsTrue(_rebind.IsRebinding);
            Assert.AreSame(RebindService.RebindingMode, _context.Modes.Current);
            CollectionAssert.IsEmpty(_context.EnabledActions());

            Press(_keyboard.kKey);
            Settle();

            Assert.AreEqual(RebindResult.Completed, Result(task));
            Assert.AreEqual("<Keyboard>/k", _jump.bindings[0].effectivePath);
            Assert.IsFalse(_rebind.IsRebinding);
            Assert.AreSame(Gameplay, _context.Modes.Current);
            Assert.IsFalse(_jump.enabled, "The new key is still held, so Jump waits for its release.");
            Assert.AreSame(_jump, _context.BindingChanges.Single().Action);

            Release(_keyboard.kKey);
            Assert.IsTrue(_jump.enabled);
        }

        [Test]
        public void Rebind_AcceptsOnlyDevicesOfTheBindingsScheme()
        {
            var gamepad = Gamepad.current;
            var task = _rebind.Rebind(_jump, 1); // the Gamepad binding

            Press(_keyboard.kKey);
            Settle();
            Assert.AreEqual(UniTaskStatus.Pending, task.Status, "A key cannot replace a gamepad binding.");

            Press(gamepad.buttonNorth);
            Settle();
            Assert.AreEqual(RebindResult.Completed, Result(task));
            Assert.AreEqual("<Gamepad>/buttonNorth", _jump.bindings[1].effectivePath);
        }

        [Test]
        public void CancelControl_EndsTheRebind_WithoutChangingTheBinding()
        {
            var task = _rebind.Rebind(_jump, 0);

            Press(_keyboard.escapeKey);
            Settle();

            Assert.AreEqual(RebindResult.Canceled, Result(task));
            Assert.AreEqual("<Keyboard>/space", _jump.bindings[0].effectivePath);
            Assert.AreSame(Gameplay, _context.Modes.Current);
            CollectionAssert.IsEmpty(_context.BindingChanges);
        }

        [Test]
        public void CancelPress_DoesNotPerformTheActionsBoundToIt()
        {
            // Esc cancels the rebind and is also Gameplay/Pause: the same press must not pause the game once the maps are back.
            var pause = _context.Action("Gameplay/Pause");
            var performed = 0;
            pause.performed += _ => performed++;
            var task = _rebind.Rebind(_jump, 0);

            Press(_keyboard.escapeKey);

            Assert.AreEqual(RebindResult.Canceled, Result(task));
            Assert.AreSame(Gameplay, _context.Modes.Current);
            Assert.IsFalse(pause.enabled, "Held from the press that cancelled the rebind.");
            Release(_keyboard.escapeKey);
            Assert.AreEqual(0, performed);
            Assert.IsTrue(pause.enabled);
        }

        [Test]
        public void Dispose_EndsARebindStillWaiting()
        {
            var task = _rebind.Rebind(_jump, 0);

            _rebind.Dispose();

            Assert.Throws<OperationCanceledException>(() => Result(task));
            Assert.IsFalse(_rebind.IsRebinding);
            Assert.AreSame(Gameplay, _context.Modes.Current);
            Assert.Throws<ObjectDisposedException>(() => _rebind.Rebind(_jump, 0));
        }

        [Test]
        public void BindingChange_KeepsWhichActionsAreEnabled_AndCancelsWhatIsInProgress()
        {
            // Any binding change re-resolves every map of the asset. Unity keeps each action's enabled state, so a disabled
            // map does not come back; what it resets is the actions in progress, which get their canceled callback.
            var canceled = 0;
            _jump.canceled += _ => canceled++;
            Press(_keyboard.spaceKey);
            var enabledBefore = _context.EnabledActions().ToList();

            _context.Action("UI/Submit").ApplyBindingOverride(1, "<Keyboard>/numpadEnter"); // a map that is off
            _rebind.ResetAll();

            CollectionAssert.AreEqual(enabledBefore, _context.EnabledActions());
            Assert.IsFalse(_context.Actions.FindActionMap("UI").enabled);
            CollectionAssert.IsEmpty(InputModeAudit.FindDrift(_context.Actions, _context.Modes.Current, _ => false));
            Assert.AreEqual(1, canceled, "The held Jump is cancelled by the first re-resolve; a Button then waits for a new press.");
        }

        [Test]
        public void PointerMovement_DoesNotComplete_AndCancellingTheTokenThrows()
        {
            using var cancellation = new CancellationTokenSource();
            var task = _rebind.Rebind(_jump, 0, cancellation.Token);

            Move(_mouse.position, new Vector2(100f, 100f));
            Settle();
            Assert.AreEqual(UniTaskStatus.Pending, task.Status);

            cancellation.Cancel();

            Assert.Throws<OperationCanceledException>(() => Result(task));
            Assert.IsFalse(_rebind.IsRebinding);
            Assert.AreSame(Gameplay, _context.Modes.Current);
        }

        [Test]
        public void NoInput_TimesOut()
        {
            var options = Options();
            options.Rebind.TimeoutSeconds = 1f;
            var task = _context.CreateRebind(options).Rebind(_jump, 0);

            currentTime += 2;
            InputSystem.Update();

            Assert.AreEqual(RebindResult.TimedOut, Result(task));
        }

        [Test]
        public void Composite_IsReboundByParts_AndResetClearsEveryPart()
        {
            var move = _context.Action("Gameplay/Move");
            var composite = move.bindings.IndexOf(binding => binding.isComposite);
            Assert.Throws<ArgumentException>(() => _rebind.Rebind(move, composite));
            Assert.IsFalse(_rebind.IsRebinding);

            move.ApplyBindingOverride(composite + 1, "<Keyboard>/upArrow");
            move.ApplyBindingOverride(composite + 4, "<Keyboard>/rightArrow");
            _rebind.Reset(move, composite);

            Assert.IsTrue(move.bindings.All(binding => binding.overridePath == null));
            Assert.AreSame(move, _context.BindingChanges.Single().Action);
        }

        [Test]
        public void ResetAll_RemovesEveryOverride()
        {
            _jump.ApplyBindingOverride(0, "<Keyboard>/k");
            _context.Action("UI/Submit").ApplyBindingOverride(1, "<Keyboard>/numpadEnter");

            _rebind.ResetAll();

            Assert.IsTrue(_context.Actions.actionMaps.SelectMany(map => map.bindings).All(binding => binding.overridePath == null));
            Assert.IsNull(_context.BindingChanges.Single().Action);
        }

        [Test]
        public void ActionOfAnotherAsset_IsRejected()
        {
            var other = Create();
            try
            {
                Assert.Throws<ArgumentException>(() => _rebind.Reset(other.FindAction("Gameplay/Jump"), 0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(other);
            }
        }

        /// <summary>Lets the rebind's short wait for a better match run out.</summary>
        private void Settle()
        {
            currentTime += 0.1;
            InputSystem.Update();
        }

        private static RebindResult Result(UniTask<RebindResult> task) => task.GetAwaiter().GetResult();
    }

    /// <summary>Binding overrides through the save system, the way they survive between sessions.</summary>
    public class BindingsSaveTests
    {
        private const string GLOBAL_ENTRY = "saves/global";
        private MemoryStorage _storage;
        private InputActionAsset _first;
        private InputActionAsset _second;

        [SetUp]
        public void SetUp()
        {
            _storage = new MemoryStorage();
            _first = Create();
            _second = InputActionAsset.FromJson(_first.ToJson()); // the next session: the same asset, same binding ids
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_first);
            UnityEngine.Object.DestroyImmediate(_second);
        }

        [Test]
        public void Overrides_RoundTripThroughTheGlobalSave()
        {
            var (save, _) = Session(_first);
            _first.FindAction("Gameplay/Jump").ApplyBindingOverride(0, "<Keyboard>/k");
            var move = _first.FindAction("Gameplay/Move");
            var up = move.bindings.IndexOf(binding => binding.isPartOfComposite);
            move.ApplyBindingOverride(up, "<Keyboard>/upArrow");
            Assert.IsTrue(save.SaveGlobal().GetAwaiter().GetResult().IsWritten);

            var (_, changes) = Session(_second);

            Assert.AreEqual("<Keyboard>/k", _second.FindAction("Gameplay/Jump").bindings[0].effectivePath);
            Assert.AreEqual("<Keyboard>/upArrow", _second.FindAction("Gameplay/Move").bindings[up].effectivePath);
            Assert.AreEqual(1, changes.Messages.Count, "Restoring publishes BindingsChanged once.");
        }

        [Test]
        public void NoSavedBindings_RemovesOverrides()
        {
            _second.FindAction("Gameplay/Jump").ApplyBindingOverride(0, "<Keyboard>/k");

            Session(_second);

            Assert.AreEqual("<Keyboard>/space", _second.FindAction("Gameplay/Jump").bindings[0].effectivePath);
        }

        [Test]
        public void OverrideOfARecreatedBinding_IsDroppedWithAWarning_OthersStillApply()
        {
            var jumpId = _first.FindAction("Gameplay/Jump").bindings[0].id;
            _storage.Primary[GLOBAL_ENTRY] =
                "{\"schemaVersion\":1,\"meta\":{},\"sections\":{\"input.bindings\":{\"overrides\":[" +
                "{\"id\":\"00000000-0000-0000-0000-000000000001\",\"action\":\"Gameplay/Fire\",\"path\":\"<Gamepad>/leftTrigger\"}," +
                $"{{\"id\":\"{jumpId}\",\"action\":\"Gameplay/Jump\",\"path\":\"<Keyboard>/k\"}}]}}}}}}";

            LogAssert.Expect(LogType.Warning, new Regex("matches no binding"));
            Session(_first);

            Assert.AreEqual("<Keyboard>/k", _first.FindAction("Gameplay/Jump").bindings[0].effectivePath);
        }

        [Test]
        public void NullOverrideList_RestoresTheDefaults()
        {
            _first.FindAction("Gameplay/Jump").ApplyBindingOverride(0, "<Keyboard>/k");
            _storage.Primary[GLOBAL_ENTRY] =
                "{\"schemaVersion\":1,\"meta\":{},\"sections\":{\"input.bindings\":{\"overrides\":null}}}";

            Session(_first);

            Assert.AreEqual("<Keyboard>/space", _first.FindAction("Gameplay/Jump").bindings[0].effectivePath);
        }

        private (SaveService save, Recorder<BindingsChanged> changes) Session(InputActionAsset actions)
        {
            var registry = new SaveRegistry();
            var save = new SaveService(registry, _storage, new SaveOptions { Storage = _storage });
            var changes = new Recorder<BindingsChanged>();
            registry.Register(new InputBindings(actions, changes));
            save.LoadShared(CancellationToken.None).GetAwaiter().GetResult();
            return (save, changes);
        }
    }
}
