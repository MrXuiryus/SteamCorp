using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SteamCorp
{
    public class SteamBuilding : Building
    {
        private CompSteamPipe pipeComp;
        private SteamPipeGrid pipeGrid;

        public SteamPipeGrid PipeGrid { get => pipeGrid; set => pipeGrid = value; }
        public CompSteamPipe PipeComp { get => pipeComp; set => pipeComp = value; }
        
        public int GridID
        {
            get => pipeComp.GridID;
        }

        public bool AttachedToGrid()
        {
            return pipeGrid.cachedPipeUsers.OfType<SteamBuilding>().Any(pipe => pipe.GridID == GridID);
        }
    }
}
