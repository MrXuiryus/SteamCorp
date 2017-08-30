using Verse;

namespace SteamCorp
{
    public class SteamBuilding : Building
    {
        private SteamNetGrid pipeGrid;
        public SteamNetGrid PipeGrid { get => pipeGrid; set => pipeGrid = value; }

        private CompSteam pipeComp;
        public CompSteam PipeComp { get => pipeComp; set => pipeComp = value; }
    }
}
