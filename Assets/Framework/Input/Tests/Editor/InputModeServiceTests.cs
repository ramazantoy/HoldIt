using System;
using System.Collections.Generic;
using System.Linq;
using AF.Core;
using MessagePipe;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using VContainer;
using static AF.Input.Tests.TestActions;

namespace AF.Input.Tests
{
    public class InputModeServiceTests : InputTestFixture
    {
        private InputTestContext _context;

        [SetUp]
        public void SetUpContext() => _context = new InputTestContext();

        [TearDown]
        public void TearDownContext() => _context.Dispose();

        [Test]
        public void Boot_EnablesNoMap()
        {
            _context.Actions.Enable(); // as the UI input module does when it is enabled before the service starts
            _context.Modes.Start();

            Assert.AreSame(InputMode.None, _context.Modes.Current);
            CollectionAssert.IsEmpty(_context.EnabledActions());
        }

        [Test]
        public void GameState_SelectsTheMode()
        {
            _context.Modes.Start();
            _context.GoTo<MenuState>();
            Assert.AreSame(Menu, _context.Modes.Current);
            Assert.IsTrue(_context.Actions.FindActionMap("UI").enabled);
            Assert.IsFalse(_context.Actions.FindActionMap("Gameplay").enabled);

            _context.GoTo<PlayingState>();
            Assert.AreSame(Gameplay, _context.Modes.Current);
            Assert.IsTrue(_context.Actions.FindActionMap("Gameplay").enabled);
            Assert.IsFalse(_context.Actions.FindActionMap("UI").enabled);

            CollectionAssert.AreEqual(new[] { "none>menu", "menu>gameplay" },
                _context.ModeChanges.Select(change => $"{change.Previous}>{change.Current}"));
        }

        [Test]
        public void NewestRequestWins_AndHandlesAreReleasedInAnyOrder()
        {
            _context.Playing();

            var dialog = _context.Modes.Request(Menu);
            var inventory = _context.Modes.Request(Both);
            Assert.AreSame(Both, _context.Modes.Current);
            CollectionAssert.AreEqual(new[] { Menu, Both }, _context.Modes.Requests);

            dialog.Dispose();
            Assert.AreSame(Both, _context.Modes.Current, "Releasing an older request leaves the newest one active.");

            inventory.Dispose();
            inventory.Dispose();
            Assert.AreSame(Gameplay, _context.Modes.Current, "With no request left, the game state's mode applies.");
            CollectionAssert.IsEmpty(_context.Modes.Requests);
        }

        [Test]
        public void ModeChanged_IsPublishedOnEdgesOnly()
        {
            _context.Playing();
            _context.ModeChanges.Clear();

            using (_context.Modes.Request(Gameplay))
            {
            }

            CollectionAssert.IsEmpty(_context.ModeChanges);
        }

        [Test]
        public void ModeWithAnUnknownMap_Throws()
        {
            _context.Playing();

            var exception = Assert.Throws<ArgumentException>(() => _context.Modes.Request(new InputMode("broken", CursorState.Free, "ui")));

            StringAssert.Contains("'ui'", exception.Message);
            Assert.AreSame(Gameplay, _context.Modes.Current);
        }

        [Test]
        public void BootState_CannotHaveAMode()
        {
            Assert.Throws<ArgumentException>(() => new InputOptions().ForState<BootState>(Menu));
        }

        [Test]
        public void UseInput_NeedsUseSaveFirst()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            var actions = ScriptableObject.CreateInstance<InputActionAsset>();
            try
            {
                var exception = Assert.Throws<InvalidOperationException>(() => builder.UseInput(actions, messagePipe));
                StringAssert.Contains("UseSave", exception.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(actions);
            }
        }

        [Test]
        public void GameStateWithoutAMode_FailsWhenTheServiceIsBuilt()
        {
            var options = new InputOptions().ForState<MenuState>(Menu);

            var exception = Assert.Throws<InvalidOperationException>(() => _context.CreateService(options));

            StringAssert.Contains(nameof(PlayingState), exception.Message);
        }

        [Test]
        public void Pause_ShowsThePausedMode_AndResumeBringsBackWhatWasActive()
        {
            _context.Playing();
            var dialog = _context.Modes.Request(Both);

            var pause = _context.Pause.Request("pause-menu");
            Assert.AreSame(Menu, _context.Modes.Current, "The paused mode goes above the requests made before the pause.");
            Assert.IsFalse(_context.Action("Gameplay/Jump").enabled);

            var settings = _context.Modes.Request(Settings);
            Assert.AreSame(Settings, _context.Modes.Current, "A request made during the pause goes above it.");
            settings.Dispose();
            Assert.AreSame(Menu, _context.Modes.Current);

            pause.Dispose();
            Assert.AreSame(Both, _context.Modes.Current, "Resuming brings back the request that was active before the pause.");

            dialog.Dispose();
            Assert.AreSame(Gameplay, _context.Modes.Current);
        }

        [Test]
        public void PausedBeforeStart_StartsInThePausedMode()
        {
            using var pause = _context.Pause.Request("focus");
            _context.Modes.Start();

            Assert.AreSame(Menu, _context.Modes.Current);
        }

        [Test]
        public void SceneLoading_EnablesNoMap_UntilTheNewStateIsIn()
        {
            _context.Modes.Start();
            _context.GoTo<MenuState>();

            // As ISceneLoader does it: loading starts, the new state is entered, the scenes change, loading ends.
            _context.Scenes.SetLoading(true);
            Assert.AreSame(InputMode.None, _context.Modes.Current);
            CollectionAssert.IsEmpty(_context.EnabledActions());

            _context.GoTo<PlayingState>();
            Assert.AreSame(InputMode.None, _context.Modes.Current, "The new state's mode waits until the load ends.");

            _context.Scenes.SetLoading(false);
            Assert.AreSame(Gameplay, _context.Modes.Current);
            CollectionAssert.AreEqual(new[] { "none>menu", "menu>none", "none>gameplay" },
                _context.ModeChanges.Select(change => $"{change.Previous}>{change.Current}"),
                "The previous state's mode never comes back in between.");
        }

        [Test]
        public void RequestsDuringSceneLoading_WaitForItsEnd_InTheirOrder()
        {
            _context.Playing();
            _context.Scenes.SetLoading(true);

            var cutscene = _context.Modes.Request(Both); // as the new scene's Start may do under the loading screen
            var pause = _context.Pause.Request("platform-overlay");
            Assert.AreSame(InputMode.None, _context.Modes.Current, "Nothing takes input while the scenes change.");
            CollectionAssert.IsEmpty(_context.EnabledActions());

            _context.Scenes.SetLoading(false);
            Assert.AreSame(Menu, _context.Modes.Current, "The newest request, the pause, applies once the load ends.");

            pause.Dispose();
            Assert.AreSame(Both, _context.Modes.Current);
            cutscene.Dispose();
            Assert.AreSame(Gameplay, _context.Modes.Current);
        }

        [Test]
        public void LoadingBeforeStart_StartsWithNoMap()
        {
            _context.Scenes.SetLoading(true);
            _context.GoTo<MenuState>();
            _context.Modes.Start();

            Assert.AreSame(InputMode.None, _context.Modes.Current);
            _context.Scenes.SetLoading(false);
            Assert.AreSame(Menu, _context.Modes.Current);
        }

        [Test]
        public void Cursor_FollowsTheMode()
        {
            _context.Playing();
            Assert.AreEqual((CursorLockMode.Locked, false), _context.Cursor);

            using (_context.Modes.Request(Menu))
                Assert.AreEqual((CursorLockMode.None, true), _context.Cursor, "Free shows the cursor while no mouse-less scheme is in use.");

            using (_context.Modes.Request(new InputMode("cutscene", CursorState.Hidden)))
                Assert.AreEqual((CursorLockMode.None, false), _context.Cursor);

            _context.Modes.Dispose();
            Assert.AreEqual((CursorLockMode.None, true), _context.Cursor, "Disposing frees the cursor.");
            CollectionAssert.IsEmpty(_context.EnabledActions());
        }

        [Test]
        public void ConfineCursor_ConfinesFreeAndHidden_NotLocked()
        {
            _context.Playing();
            _context.Modes.ConfineCursor = true;
            Assert.AreEqual((CursorLockMode.Locked, false), _context.Cursor);

            using (_context.Modes.Request(Menu))
            {
                Assert.AreEqual((CursorLockMode.Confined, true), _context.Cursor);
                _context.Modes.ConfineCursor = false;
                Assert.AreEqual((CursorLockMode.None, true), _context.Cursor, "Applies at once.");
                _context.Modes.ConfineCursor = true;
            }

            using (_context.Modes.Request(new InputMode("cutscene", CursorState.Hidden)))
                Assert.AreEqual((CursorLockMode.Confined, false), _context.Cursor);

            _context.Modes.Dispose();
            Assert.AreEqual((CursorLockMode.None, true), _context.Cursor, "Disposing frees the cursor.");
        }

        [Test]
        public void RequestFromACanceledCallback_EndsInTheNewestMode()
        {
            _context.Playing();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            IDisposable aiming = null;
            _context.Action("Gameplay/Jump").canceled += _ => aiming ??= _context.Modes.Request(Both);
            Press(keyboard.spaceKey);

            using var menu = _context.Modes.Request(Menu); // disables Jump, whose canceled callback requests Both

            Assert.AreSame(Both, _context.Modes.Current);
            Assert.IsTrue(_context.Actions.FindActionMap("UI").enabled);
            Release(keyboard.spaceKey); // Jump came back held; once released, every action is as the mode says
            CollectionAssert.IsEmpty(InputModeAudit.FindDrift(_context.Actions, _context.Modes.Current, _ => false));
            CollectionAssert.AreEqual(new[] { "none>gameplay", "gameplay>menu", "menu>both" },
                _context.ModeChanges.Select(change => $"{change.Previous}>{change.Current}"));
            aiming.Dispose();
        }

        [Test]
        public void RequestFromAModeChangedHandler_IsPublishedAfterTheChangeThatCausedIt()
        {
            _context.Playing();
            IDisposable followUp = null;
            var seen = new List<string>();
            using var subscription = _context.Container.Resolve<ISubscriber<InputModeChanged>>().Subscribe(change =>
            {
                seen.Add($"{change.Previous}>{change.Current}:{_context.Modes.Current}");
                if (change.Current == Menu)
                    followUp ??= _context.Modes.Request(Settings);
            });

            using var menu = _context.Modes.Request(Menu);

            CollectionAssert.AreEqual(new[] { "gameplay>menu:menu", "menu>settings:settings" }, seen);
            followUp.Dispose();
        }

        [Test]
        public void Drift_ReportsActionsChangedBehindTheService()
        {
            _context.Playing();
            _context.Action("UI/Submit").Enable();
            _context.Action("Gameplay/Jump").Disable();

            var problems = InputModeAudit.FindDrift(_context.Actions, _context.Modes.Current, _ => false);

            Assert.AreEqual(2, problems.Count, string.Join("\n", problems));
            StringAssert.Contains("Gameplay/{Jump} disabled", problems[0]);
            StringAssert.Contains("UI/{Submit} enabled", problems[1]);
        }

        [Test]
        public void Apply_RestoresActionsChangedBehindTheService()
        {
            _context.Playing();
            _context.Action("Gameplay/Jump").Disable(); // the old "disabled action as a guard" habit

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"Gameplay/\{Jump\} disabled outside the input mode service"));
            using (_context.Modes.Request(Menu))
            {
            }

            Assert.IsTrue(_context.Action("Gameplay/Jump").enabled, "Every apply enables every action of the mode's maps.");
        }
    }
}
