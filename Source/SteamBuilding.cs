using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SteamCorp
{
    public class SteamBuilding : Building
    {
        private SteamNetGrid pipeGrid;
        public SteamNetGrid PipeGrid { get => pipeGrid; set => pipeGrid = value; }

        private CompSteam pipeComp;
        public CompSteam PipeComp { get => pipeComp; set => pipeComp = value; }

        /* Possibly not useful for now, uncomment to use.
        // Returns true if this SteamBuilding is connected to a pipe
        public bool AttachedToGrid()
        {
            if (GridID == -1)
            {
                Log.Message(this + " is not attached to a grid.");
                return false;
            }
            else
            {
                bool ret = pipeGrid.cachedPipeUsers.OfType<SteamBuilding>().Any(pipe => pipe.GridID == GridID);
                Log.Message(ret ? (this + " is attached to grid " + GridID) : (this + " is not attached to a grid."));
                return ret;
            }
        }*/
    }
}
