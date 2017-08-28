using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SteamCorp
{
    class CompSteamPipe : ThingComp
    {
        private int intGridID;

        public int GridID
        {
            get => intGridID;
            set => intGridID = value;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
        }
    }
}
