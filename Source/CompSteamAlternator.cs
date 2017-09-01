using RimWorld;
using Verse;

namespace SteamCorp
{
    class CompSteamAlternator : CompSteamTrader
    {
        protected CompBreakdownable breakdownableComp;

        protected virtual float DesiredPowerOutput
        {
            get => -((CompProperties_SteamAlternator)props).wattsOfSteamIn;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            breakdownableComp = parent.GetComp<CompBreakdownable>();
            if (Props.baseSteamConsumption > 0f && !parent.IsSteamBrokenDown())
            {
                SteamOn = true;
                SteamPowerOutput = -((CompProperties_SteamAlternator)props).baseSteamConsumption;
                parent.GetComp<CompPowerPlant>().PowerOutput = ((CompProperties_SteamAlternator)props).wattsOfPowerOut;
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
                || (flickableComp != null && !flickableComp.SwitchIsOn)
                || !SteamOn)
            {
                SteamPowerOutput = parent.GetComp<CompPowerPlant>().PowerOutput = 0f;
            }
            else
            {
                SteamPowerOutput = -((CompProperties_SteamAlternator)props).baseSteamConsumption;
                parent.GetComp<CompPowerPlant>().PowerOutput = ((CompProperties_SteamAlternator)props).wattsOfPowerOut;
            }
        }
    }
}