using System.Collections.Generic;

namespace SteamCorp
{
    public static class SteamNetManagers
    {
        private static List<SteamNetManager> steamNetManagerList;
        
        public static List<SteamNetManager> List
        {
            get => steamNetManagerList ?? (steamNetManagerList = new List<SteamNetManager>());
            set => steamNetManagerList = value;
        }
    }
}
