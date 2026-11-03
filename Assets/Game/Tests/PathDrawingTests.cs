using HoldIt.Board;
using HoldIt.Characters;
using HoldIt.Drawing;
using HoldIt.Input;
using HoldIt.Paths;
using MessagePipe;
using NUnit.Framework;
using UnityEngine;
using VContainer;

namespace HoldIt.Tests
{
    /// <summary>
    /// Drawing on Map 1 (<see cref="TestMaps.Lobby"/>) with real characters and brokers, seen by a fake camera that looks
    /// straight down but lifts heights up the screen, as the slanted camera does.
    /// </summary>
    public class PathDrawingTests
    {
        private static readonly Cell _start = new Cell(0, 10);

        private IObjectResolver _container;
        private ITileGrid _grid;
        private CharacterConfig _characterConfig;
        private DrawingConfig _drawingConfig;
        private CharacterSystem _characters;
        private PathDrawing _drawing;
        private FakeBoardCamera _camera;

        [SetUp]
        public void CreateDrawing()
        {
            var builder = new ContainerBuilder();
            var messagePipe = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<CharacterSpawned>(messagePipe);
            builder.RegisterMessageBroker<CharacterStateChanged>(messagePipe);
            builder.RegisterMessageBroker<StepRefused>(messagePipe);
            builder.RegisterMessageBroker<CharacterRemoved>(messagePipe);
            builder.RegisterMessageBroker<PointerDown>(messagePipe);
            builder.RegisterMessageBroker<PointerMoved>(messagePipe);
            builder.RegisterMessageBroker<PointerUp>(messagePipe);
            _container = builder.Build();

            _grid = MapParser.Parse(TestMaps.Lobby);
            _characterConfig = ScriptableObject.CreateInstance<CharacterConfig>();
            _drawingConfig = ScriptableObject.CreateInstance<DrawingConfig>();
            _characters = new CharacterSystem(_grid, _characterConfig, new FakeRestrooms(), new FakeClock(),
                _container.Resolve<IPublisher<CharacterSpawned>>(), _container.Resolve<IPublisher<CharacterStateChanged>>(),
                _container.Resolve<IPublisher<StepRefused>>(), _container.Resolve<IPublisher<CharacterRemoved>>());
            _camera = new FakeBoardCamera(_grid);
            _drawing = new PathDrawing(_characters, _grid, _camera, _camera, _drawingConfig,
                _container.Resolve<ISubscriber<PointerDown>>(), _container.Resolve<ISubscriber<PointerMoved>>(),
                _container.Resolve<ISubscriber<PointerUp>>());
            _drawing.Initialize();
        }

        [TearDown]
        public void DisposeDrawing()
        {
            _drawing.Dispose();
            _container.Dispose();
            UnityEngine.Object.DestroyImmediate(_characterConfig);
            UnityEngine.Object.DestroyImmediate(_drawingConfig);
        }

        [Test]
        public void Press_OnTheHead_StartsAPathFromTheCharacter()
        {
            var id = _characters.Spawn(Gender.Male, BodySize.Normal, _start);

            Down(_camera.ScreenOf(_start, _drawingConfig.PickHeight));

            Assert.IsTrue(_drawing.IsDrawing);
            Assert.AreEqual(id, _drawing.Drawer);
            CollectionAssert.AreEqual(new[] { _start }, _drawing.Cells);
        }

        [Test]
        public void Press_FarFromEveryone_DrawsNothing()
        {
            _characters.Spawn(Gender.Male, BodySize.Normal, _start);

            Down(_camera.ScreenOf(new Cell(2, 10), 0f));

            Assert.IsFalse(_drawing.IsDrawing);
            Assert.AreEqual(0, _drawing.Cells.Count);
        }

        [Test]
        public void Press_OnAWalkingCharacter_DrawsNothing()
        {
            var id = _characters.Spawn(Gender.Male, BodySize.Normal, _start);
            _characters.TryWalk(id, new CellPath(new[] { _start, new Cell(0, 9) }));

            Down(_camera.ScreenOf(_start, 0.5f));

            Assert.IsFalse(_drawing.IsDrawing);
        }

        [Test]
        public void Drag_ExtendsThePathAndRelease_SendsTheCharacter()
        {
            var id = _characters.Spawn(Gender.Male, BodySize.Normal, _start);

            Down(_camera.ScreenOf(_start, 0.5f));
            Move(_camera.ScreenOf(new Cell(0, 8), 0f));
            CollectionAssert.AreEqual(new[] { _start, new Cell(0, 9), new Cell(0, 8) }, _drawing.Cells);
            Assert.IsTrue(_drawing.EndIsFree);
            Up(_camera.ScreenOf(new Cell(0, 8), 0f));

            Assert.IsFalse(_drawing.IsDrawing);
            _characters.TryGet(id, out var character);
            Assert.AreEqual(CharacterState.Walking, character.State);
            Assert.AreEqual(new Cell(0, 8), character.Route.End);
        }

        [Test]
        public void Drag_OffTheBoard_StopsAtItsEdge()
        {
            _characters.Spawn(Gender.Male, BodySize.Normal, _start);

            Down(_camera.ScreenOf(_start, 0.5f));
            Move(_camera.ScreenOf(new Cell(-3, 10), 0f));

            Assert.AreEqual(_start, _drawing.Cells[_drawing.Cells.Count - 1]);
            Move(_camera.ScreenOf(new Cell(-3, 9), 0f));
            Assert.AreEqual(new Cell(0, 9), _drawing.Cells[_drawing.Cells.Count - 1]);
        }

        [Test]
        public void Release_WhereSomeoneStands_CutsBackToTheLastFreeCell()
        {
            var id = _characters.Spawn(Gender.Male, BodySize.Normal, _start);
            _characters.Spawn(Gender.Female, BodySize.Normal, new Cell(0, 8));

            Down(_camera.ScreenOf(_start, 0.5f));
            Move(_camera.ScreenOf(new Cell(0, 8), 0f));
            Assert.IsFalse(_drawing.EndIsFree);
            Up(_camera.ScreenOf(new Cell(0, 8), 0f));

            _characters.TryGet(id, out var character);
            Assert.AreEqual(new Cell(0, 9), character.Route.End);
        }

        [Test]
        public void Release_WhereSomeoneIsGoing_CutsBackToTheLastFreeCell()
        {
            var goer = _characters.Spawn(Gender.Female, BodySize.Normal, new Cell(1, 9));
            _characters.TryWalk(goer, new CellPath(new[] { new Cell(1, 9), new Cell(0, 9), new Cell(0, 8) }));
            var id = _characters.Spawn(Gender.Male, BodySize.Normal, _start);

            Down(_camera.ScreenOf(_start, 0.5f));
            Move(_camera.ScreenOf(new Cell(0, 8), 0f));
            Up(_camera.ScreenOf(new Cell(0, 8), 0f));

            _characters.TryGet(id, out var character);
            Assert.AreEqual(new Cell(0, 9), character.Route.End, "It may pass the walker, not end where it goes.");
        }

        [Test]
        public void Release_OnItsOwnCell_SendsNothing()
        {
            var id = _characters.Spawn(Gender.Male, BodySize.Normal, _start);

            Down(_camera.ScreenOf(_start, 0.5f));
            Up(_camera.ScreenOf(_start, 0f));

            Assert.IsFalse(_drawing.IsDrawing);
            _characters.TryGet(id, out var character);
            Assert.AreEqual(CharacterState.Idle, character.State);
        }

        [Test]
        public void Release_WithoutAPress_DoesNothing()
        {
            var id = _characters.Spawn(Gender.Male, BodySize.Normal, _start);

            Up(_camera.ScreenOf(new Cell(0, 8), 0f));

            _characters.TryGet(id, out var character);
            Assert.AreEqual(CharacterState.Idle, character.State);
        }

        private void Down(Vector2 position) =>
            _container.Resolve<IPublisher<PointerDown>>().Publish(new PointerDown(position));

        private void Move(Vector2 position) =>
            _container.Resolve<IPublisher<PointerMoved>>().Publish(new PointerMoved(position));

        private void Up(Vector2 position) => _container.Resolve<IPublisher<PointerUp>>().Publish(new PointerUp(position));

        /// <summary>100 pixels a tile, straight down, with every unit of height 40 pixels up the screen.</summary>
        private sealed class FakeBoardCamera : IBoardPicker, IBoardProjection
        {
            private const float PixelsPerTile = 100f;
            private const float PixelsPerHeight = 40f;

            private readonly ITileGrid _grid;

            public FakeBoardCamera(ITileGrid grid) => _grid = grid;

            public Quaternion Rotation => Quaternion.Euler(90f, 0f, 0f);

            public Vector2 ScreenOf(Cell cell, float height)
            {
                TryWorldToScreen(_grid.Space.CellCenter(cell) + Vector3.up * height, out var screen);
                return screen;
            }

            public bool TryWorldToScreen(Vector3 world, out Vector2 screen)
            {
                screen = new Vector2(world.x * PixelsPerTile, world.z * PixelsPerTile + world.y * PixelsPerHeight);
                return true;
            }

            public bool TryPick(Vector2 screenPosition, out Cell cell) =>
                _grid.Space.TryGetCell(Floor(screenPosition), out cell);

            public bool TryPickClamped(Vector2 screenPosition, out Cell cell)
            {
                _grid.Space.TryGetCell(Floor(screenPosition), out var unclamped);
                cell = new Cell(Mathf.Clamp(unclamped.Col, 0, _grid.Cols - 1), Mathf.Clamp(unclamped.Row, 0, _grid.Rows - 1));
                return true;
            }

            private static Vector3 Floor(Vector2 screen) =>
                new Vector3(screen.x / PixelsPerTile, 0f, screen.y / PixelsPerTile);
        }
    }
}
