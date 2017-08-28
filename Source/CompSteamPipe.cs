using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SteamCorp
{
    public class CompSteamPipe : ThingComp
    {
        public static readonly float WattsToWattDaysPerTick = 1.66666669E-05f;

        private int intGridID;

        public int GridID
        {
            get => intGridID;
            set => intGridID = value;
        }
    }
}
