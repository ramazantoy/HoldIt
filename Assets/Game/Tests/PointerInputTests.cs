using System;
using System.Collections.Generic;
using HoldIt.Input;
using MessagePipe;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace HoldIt.Tests
{
    public class PointerInputTests : InputTestFixture
    {
        private static readonly Rect _uiArea = new Rect(0f, 0f, 100f, 100f);

        private readonly List<string> _log = new List<string>();
        private Mouse _mouse;
        private InputActionAsset _actions;
        private PointerInput _pointer;

        [SetUp]
        public void SetUpPointer()
        {
            _mouse = InputSystem.AddDevice<Mouse>();
            _actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var gameplay = _actions.AddActionMap("Gameplay");
            gameplay.AddAction("Point", InputActionType.PassThrough, "<Pointer>/position", expectedControlLayout: "Vector2");
            gameplay.AddAction("Press", InputActionType.Button, "<Pointer>/press");

            _pointer = new PointerInput(_actions, new AreaHitTest(_uiArea),
                new LogPublisher<PointerDown>(_log, m => $"down {m.Position}"),
                new LogPublisher<PointerMoved>(_log, m => $"moved {m.Position}"),
                new LogPublisher<PointerUp>(_log, m => $"up {m.Position}"));
            _pointer.Initialize();
            gameplay.Enable();
        }

        [TearDown]
        public void TearDownPointer()
        {
            _pointer.Dispose();
            Object.DestroyImmediate(_actions);
            _log.Clear();
        }

        [Test]
        public void PressOutsideUi_PublishesDownMovesAndUp()
        {
            Set(_mouse.position, new Vector2(200f, 300f));
            Press(_mouse.leftButton);
            _pointer.Tick();
            Assert.IsTrue(_pointer.IsPressed);

            Set(_mouse.position, new Vector2(250f, 300f));
            _pointer.Tick();
            _pointer.Tick(); // No change, no message.

            Release(_mouse.leftButton);
            _pointer.Tick();

            Assert.IsFalse(_pointer.IsPressed);
            CollectionAssert.AreEqual(new[] { "down (200.00, 300.00)", "moved (250.00, 300.00)", "up (250.00, 300.00)" }, _log);
        }

        [Test]
        public void PressStartedOverUi_IsIgnoredUntilReleased_EvenWhenDraggedOff()
        {
            Set(_mouse.position, new Vector2(50f, 50f));
            Press(_mouse.leftButton);
            _pointer.Tick();
            Set(_mouse.position, new Vector2(400f, 400f));
            _pointer.Tick();
            Release(_mouse.leftButton);
            _pointer.Tick();

            Assert.IsFalse(_pointer.IsPressed);
            CollectionAssert.IsEmpty(_log);

            Press(_mouse.leftButton);
            _pointer.Tick();
            CollectionAssert.AreEqual(new[] { "down (400.00, 400.00)" }, _log, "The next press, outside the UI, counts.");
        }

        [Test]
        public void TapWithinOneFrame_PublishesDownThenUp()
        {
            Set(_mouse.position, new Vector2(200f, 300f));
            Press(_mouse.leftButton);
            Release(_mouse.leftButton);
            _pointer.Tick();

            Assert.IsFalse(_pointer.IsPressed);
            CollectionAssert.AreEqual(new[] { "down (200.00, 300.00)", "up (200.00, 300.00)" }, _log);
        }

        [Test]
        public void ReleaseAndNextPressInOneFrame_KeepTheirOwnPositions()
        {
            Set(_mouse.position, new Vector2(200f, 300f));
            Press(_mouse.leftButton);
            _pointer.Tick();

            // A quick re-tap, or a frame hitch: the finger lifts at A and lands at B before the next Tick.
            Release(_mouse.leftButton);
            Set(_mouse.position, new Vector2(600f, 700f));
            Press(_mouse.leftButton);
            _pointer.Tick();

            CollectionAssert.AreEqual(new[] { "down (200.00, 300.00)", "up (200.00, 300.00)", "down (600.00, 700.00)" }, _log,
                "The old press is not dragged to the new one's position.");
        }

        [Test]
        public void ModeDisablingTheMap_EndsThePressWithUp()
        {
            Set(_mouse.position, new Vector2(200f, 300f));
            Press(_mouse.leftButton);
            _pointer.Tick();

            // What the input mode service does when the game pauses or a scene starts loading.
            _actions.FindActionMap("Gameplay").Disable();
            _pointer.Tick();

            Assert.IsFalse(_pointer.IsPressed);
            CollectionAssert.AreEqual(new[] { "down (200.00, 300.00)", "up (200.00, 300.00)" }, _log);
        }

        [Test]
        public void Hover_UpdatesPositionWithoutMessages()
        {
            Set(_mouse.position, new Vector2(200f, 300f));
            _pointer.Tick();

            Assert.AreEqual(new Vector2(200f, 300f), _pointer.Position);
            CollectionAssert.IsEmpty(_log);
        }

        private sealed class AreaHitTest : IUiHitTest
        {
            private readonly Rect _area;

            public AreaHitTest(Rect area) => _area = area;

            public bool IsOverUi(Vector2 screenPosition) => _area.Contains(screenPosition);
        }

        private sealed class LogPublisher<T> : IPublisher<T>
        {
            private readonly List<string> _log;
            private readonly Func<T, string> _format;

            public LogPublisher(List<string> log, Func<T, string> format)
            {
                _log = log;
                _format = format;
            }

            public void Publish(T message) => _log.Add(_format(message));
        }
    }
}
