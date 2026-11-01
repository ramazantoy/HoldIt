using System;
using System.Collections.Generic;
using AF.Core;
using MessagePipe;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using static AF.Input.Tests.TestActions;

namespace AF.Input.Tests
{
    public class ControlSchemeTrackerTests : InputTestFixture
    {
        private readonly List<string> _changes = new List<string>();
        private InputTestContext _context;
        private IDisposable _subscription;
        private Keyboard _keyboard;
        private Gamepad _gamepad;

        [SetUp]
        public void SetUpDevices()
        {
            _keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            _changes.Clear();
        }

        [TearDown]
        public void TearDownContext()
        {
            _subscription?.Dispose();
            _context?.Dispose();
        }

        [Test]
        public void StartsWithTheFirstSchemeWhoseDevicesAreConnected()
        {
            Start(countsEveryDevice: true);

            Assert.AreEqual(KeyboardMouseScheme, _context.Schemes.Current);
            Assert.IsTrue(_context.Schemes.UsesMouse);
        }

        [Test]
        public void SchemeChanges_ArePublishedWhenTheSchemeChanges_NotOnEveryDevice()
        {
            Start(countsEveryDevice: true);

            PressAndRelease(_gamepad.buttonSouth);
            Assert.AreEqual(GamepadScheme, _context.Schemes.Current);
            Assert.AreSame(_gamepad, _context.Schemes.Device);
            Assert.IsFalse(_context.Schemes.UsesMouse);

            var secondPad = InputSystem.AddDevice<Gamepad>();
            PressAndRelease(secondPad.buttonSouth);
            Assert.AreSame(secondPad, _context.Schemes.Device);

            PressAndRelease(_keyboard.spaceKey);
            PressAndRelease(_keyboard.spaceKey);

            CollectionAssert.AreEqual(new[] { $"{KeyboardMouseScheme}>{GamepadScheme}", $"{GamepadScheme}>{KeyboardMouseScheme}" }, _changes);
        }

        [Test]
        public void DevicesCreatedByScript_DoNotChangeTheScheme()
        {
            // The tracker's own rule. Devices added from code are what on-screen controls and a virtual mouse create.
            Start(countsEveryDevice: false);
            Assert.IsNull(_context.Schemes.Current, "No device the runtime reported, so no scheme yet.");

            PressAndRelease(_gamepad.buttonSouth);

            Assert.IsNull(_context.Schemes.Current);
            Assert.IsNull(_context.Schemes.Device);
            CollectionAssert.IsEmpty(_changes);
        }

        [Test]
        public void FreeCursor_IsHiddenWhileTheSchemeHasNoMouse()
        {
            Start(countsEveryDevice: true);
            _context.GoTo<MenuState>();
            Assert.AreEqual((CursorLockMode.None, true), _context.Cursor);

            Set(_gamepad.rightTrigger, 1f); // UI/Submit
            Set(_gamepad.rightTrigger, 0f);
            Assert.AreEqual((CursorLockMode.None, false), _context.Cursor);

            PressAndRelease(_keyboard.enterKey);
            Assert.AreEqual((CursorLockMode.None, true), _context.Cursor);
        }

        private void Start(bool countsEveryDevice)
        {
            _context = new InputTestContext(countsDevice: countsEveryDevice ? _ => true : null).Playing();
            _subscription = _context.Container.Resolve<ISubscriber<ControlSchemeChanged>>()
                .Subscribe(change => _changes.Add($"{change.Previous}>{change.Current}"));
        }
    }
}
