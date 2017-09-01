using RimWorld;

namespace SteamCorp
{
    class CompSteamAlternator : CompSteamTrader
    {
        protected CompBreakdownable breakdownableComp;
        protected CompRefuelable refuelableComp;

        protected virtual float DesiredPowerOutput
        {
            get => 0;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            refuelableComp = parent.GetComp<CompRefuelable>();
            breakdownableComp = parent.GetComp<CompBreakdownable>();
            if (Props.baseSteamConsumption < 0f && !parent.IsSteamBrokenDown())
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
                parent.GetComp<CompPowerPlant>().PowerOutput = ((CompProperties_Power)props).basePowerConsumption;
            }
        }
    }
}