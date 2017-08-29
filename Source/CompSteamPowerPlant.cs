using System;
using RimWorld;

namespace SteamCorp
{
    class CompSteamPowerPlant : CompSteamTrader
    {
        protected CompRefuelable refuelableComp;

        protected CompBreakdownable breakdownableComp;

        protected virtual float DesiredPowerOutput
        {
            get => -Props.baseSteamConsumption;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            refuelableComp = parent.GetComp<CompRefuelable>();
            breakdownableComp = parent.GetComp<CompBreakdownable>();
            if (Props.baseSteamConsumption < 0f && !parent.IsBrokenDown())
            {
                SteamOn = true;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            UpdateDesiredPowerOutput();
        }

        public void UpdateDesiredPowerOutput()
        {
            if ((breakdownableComp != null && breakdownableComp.BrokenDown) 
                || (refuelableComp != null && !refuelableComp.HasFuel) 
                || (flickableComp != null && !flickableComp.SwitchIsOn) 
                || !SteamOn)
            {
                SteamPowerOutput = 0f;
            }
            else
            {
                SteamPowerOutput = DesiredPowerOutput;
            }
        }
    }
}