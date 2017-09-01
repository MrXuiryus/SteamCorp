using Verse;

namespace SteamCorp
{
    public class Building_Steam : Building
    {
        private SteamNetGrid pipeGrid;
        public SteamNetGrid PipeGrid { get => pipeGrid; set => pipeGrid = value; }

        private CompSteam pipeComp;
        public CompSteam PipeComp { get => pipeComp; set => pipeComp = value; }

        public virtual bool TransmitsSteamPower
        {
            get
            {
                CompSteam powerComp = this.TryGetComp<CompSteam>();
                return powerComp != null && powerComp.Props.transmitsSteam;
            }
        }

        public override void Tick()
        {
            base.Tick();
            SnowUtility.AddSnowRadial(this.OccupiedRect().RandomCell, Map, 4f, -0.06f);
        }
    }
} 