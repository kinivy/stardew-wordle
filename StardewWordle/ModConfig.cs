namespace StardewWordle
{
    public class ModConfig
    {
        public bool DarkTheme = false;
        public bool EnableNotifications = false;
        public bool EnableUIInfoSuite2Integration = true;
        public MultiplayerMode MultiplayerMode = MultiplayerMode.Synchronous;
    }
    public enum MultiplayerMode
    {
        Individual,
        Synchronous
    }
}
