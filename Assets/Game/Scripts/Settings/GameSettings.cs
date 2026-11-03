using AF.Save;

namespace HoldIt.Settings
{
    /// <summary>
    /// Keeps <see cref="IGameSettings"/> in the global data, next to the language, so they follow the player to other
    /// devices. Nothing mirrors them while they change: whoever needs one reads it when it applies.
    /// </summary>
    internal sealed class GameSettings : Savable<GameSettingsData>, IGameSettings
    {
        private GameSettingsData _data = new GameSettingsData();

        public override string Key => "game.settings";
        public override SaveDocument Document => SaveDocument.Global;

        public bool Haptics => _data.haptics;

        public void SetHaptics(bool on) => _data.haptics = on;

        protected override GameSettingsData Capture() => _data;

        protected override void Restore(GameSettingsData data) => _data = data;
    }
}
