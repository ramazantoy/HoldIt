using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using static AF.Input.Tests.TestActions;

namespace AF.Input.Tests
{
    /// <summary>
    /// The press that switches modes must not perform an action of the new mode. The Input System does it for analog controls:
    /// a Button action enabled while its trigger is held performs on the trigger's next change past the press point.
    /// </summary>
    public class CarryOverTests : InputTestFixture
    {
        private InputTestContext _context;
        private Gamepad _gamepad;
        private Keyboard _keyboard;

        [SetUp]
        public void SetUpContext()
        {
            _gamepad = InputSystem.AddDevice<Gamepad>();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _context = new InputTestContext();
        }

        [TearDown]
        public void TearDownContext() => _context.Dispose();

        [Test]
        public void InputSystem_PerformsAButtonEnabledWhileItsTriggerIsHeld()
        {
            // The trap itself, without the service. If an Input System update makes this fail, HeldButtonFilter may be obsolete.
            var submit = _context.Action("UI/Submit");
            var performed = 0;
            submit.performed += _ => performed++;

            Set(_gamepad.rightTrigger, 1f);
            submit.Enable();
            Set(_gamepad.rightTrigger, 0.7f);

            Assert.AreEqual(1, performed, "Releasing the held trigger through the press point performs the new action.");
        }

        [Test]
        public void HeldTrigger_DoesNotPerformTheButtonOfTheModeItSwitchedTo()
        {
            _context.Playing();
            IDisposable menu = null;
            _context.Action("Gameplay/Fire").performed += _ => menu ??= _context.Modes.Request(Menu);
            var submit = _context.Action("UI/Submit");
            var performed = 0;
            submit.performed += _ => performed++;

            Set(_gamepad.rightTrigger, 1f);
            Assert.AreSame(Menu, _context.Modes.Current);
            Assert.IsFalse(submit.enabled, "Held from the press that switched modes.");

            Set(_gamepad.rightTrigger, 0.7f);
            Set(_gamepad.rightTrigger, 0f);
            Assert.AreEqual(0, performed);
            Assert.IsTrue(submit.enabled, "Enabled once the trigger is released.");

            Set(_gamepad.rightTrigger, 0.9f);
            Assert.AreEqual(1, performed, "A new press performs it.");
            menu.Dispose();
        }

        [Test]
        public void HeldKey_IsHeldToo_AndTheNextPressPerforms()
        {
            _context.Playing();
            IDisposable menu = null;
            _context.Action("Gameplay/Pause").performed += _ => menu ??= _context.Modes.Request(Menu);
            var cancel = _context.Action("UI/Cancel");
            var performed = 0;
            cancel.performed += _ => performed++;

            Press(_keyboard.escapeKey);
            Assert.AreSame(Menu, _context.Modes.Current);
            Assert.IsFalse(cancel.enabled, "Held from the press that switched modes.");
            Release(_keyboard.escapeKey);
            Assert.IsTrue(cancel.enabled);
            Assert.AreEqual(0, performed);

            Press(_keyboard.escapeKey);
            Assert.AreEqual(1, performed);
            menu.Dispose();
        }

        [Test]
        public void HeldValue_CarriesOverAModeChange()
        {
            _context.Playing();
            var move = _context.Action("Gameplay/Move");
            Set(_gamepad.leftStick, new Vector2(0f, 1f));

            using (_context.Modes.Request(Menu))
                Assert.IsFalse(move.enabled);
            InputSystem.Update();

            Assert.IsTrue(move.enabled, "Value actions are never held back.");
            Assert.That(move.ReadValue<Vector2>().y, Is.EqualTo(1f).Within(0.01f), "Still moving after the mode came back.");
        }

        [Test]
        public void LeavingTheModeWhileHeld_LeavesTheActionDisabled()
        {
            _context.Playing();
            var submit = _context.Action("UI/Submit");
            Set(_gamepad.rightTrigger, 1f);

            var menu = _context.Modes.Request(Menu);
            Assert.IsFalse(submit.enabled);
            menu.Dispose();
            Set(_gamepad.rightTrigger, 0f);

            Assert.IsFalse(submit.enabled, "Its map is no longer in the mode.");
            CollectionAssert.DoesNotContain(_context.EnabledActions(), "UI/Submit");
        }
    }
}
