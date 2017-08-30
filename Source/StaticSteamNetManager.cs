namespace SteamCorp
{
    public static class StaticSteamNetManager
    {
        private static SteamNetManager steamNetManager;
        
        public static SteamNetManager Manager
        {
            get => steamNetManager;
            set => steamNetManager = value;
        }
    }
}
