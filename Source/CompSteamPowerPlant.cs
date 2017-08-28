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
                PowerOn = true;
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
                || !PowerOn)
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