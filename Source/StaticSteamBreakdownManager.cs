namespace SteamCorp
{
    public static class StaticSteamBreakdownManager
    {
        private static SteamBreakdownManager steamBreakdownManager;

        public static SteamBreakdownManager Manager
        {
            get => steamBreakdownManager;
            set => steamBreakdownManager = value;
        }
    }
}
