using HoldIt.Levels;
using NUnit.Framework;

namespace HoldIt.Tests
{
    public class ProgressRecorderTests
    {
        private FakeLevelCursor _cursor;
        private FakeLevelProgress _progress;
        private FakeSave _save;
        private Broker<LevelEnded> _ended;
        private ProgressRecorder _recorder;

        [SetUp]
        public void SetUpRecorder()
        {
            _cursor = new FakeLevelCursor { Index = 1 };
            _progress = new FakeLevelProgress();
            _save = new FakeSave();
            _ended = new Broker<LevelEnded>();
            _recorder = new ProgressRecorder(_cursor, _progress, _save, _ended.Subscriber);
            _recorder.Initialize();
        }

        [TearDown]
        public void TearDownRecorder()
        {
            _recorder.Dispose();
            _ended.Dispose();
        }

        [Test]
        public void Win_KeepsTheLevelsStars_AndWritesTheGlobalData()
        {
            _ended.Publish(new LevelEnded(LevelOutcome.Won, 2, 9));

            CollectionAssert.AreEqual(new[] { (1, 2) }, _progress.Wins);
            Assert.AreEqual(1, _save.GlobalSaves);
        }

        [Test]
        public void Loss_ChangesNothing()
        {
            _ended.Publish(new LevelEnded(LevelOutcome.Lost, 0, 4));

            Assert.AreEqual(0, _progress.Wins.Count);
            Assert.AreEqual(0, _save.GlobalSaves);
        }

        [Test]
        public void Dispose_StopsListening()
        {
            _recorder.Dispose();

            _ended.Publish(new LevelEnded(LevelOutcome.Won, 3, 9));

            Assert.AreEqual(0, _progress.Wins.Count);
        }
    }
}
