using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SteamCorp
{
    public class SteamBuilding : Building
    {
        private SteamPipeGrid pipeGrid;
        public SteamPipeGrid PipeGrid { get => pipeGrid; set => pipeGrid = value; }

        private CompSteamPipe pipeComp;
        public CompSteamPipe PipeComp { get => pipeComp; set => pipeComp = value; }
        
        public int GridID { get => pipeComp.GridID; set => pipeComp.GridID = value; }

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
