using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SteamCorp
{
    public class CompSteam : ThingComp
    {
        public static readonly float WattsToWattDaysPerTick = 1.66666669E-05f;

        private SteamPowerNet steamNet;
        public SteamPowerNet SteamNet { get => steamNet; set => steamNet = value; }

        private int intGridID;

        public int GridID
        {
            get => intGridID;
            set => intGridID = value;
        }

        public CompProperties_Steam Props
        {
            get => (CompProperties_Steam)props;
        }


        public virtual void SetUpPowerVars()
        { }
        public virtual void ResetPowerVars()
        { }
        public virtual void LostConnectParent()
        { }
    }
}
