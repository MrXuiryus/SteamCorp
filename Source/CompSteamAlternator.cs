using RimWorld;

namespace SteamCorp
{
    class CompSteamAlternator : CompSteamPowerPlant
    {
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (Props.baseSteamConsumption > 0f && !parent.IsSteamBrokenDown())
            {
                SteamOn = parent.GetComp<CompPowerPlant>().PowerOn = true;
            }
        } 

        public override void CompTick()
        { 
            base.CompTick();
            UpdateDesiredPowerOutput();
        }

        public override void UpdateDesiredPowerOutput()
        {   
            base.UpdateDesiredPowerOutput();
            if ((breakdownableComp != null && breakdownableComp.BrokenDown)
                || (flickableComp != null && !flickableComp.SwitchIsOn)
                || !SteamOn)
            {
                SteamOn = parent.GetComp<CompPowerPlant>().PowerOn = false;
            }
            else
            {
                SteamOn = parent.GetComp<CompPowerPlant>().PowerOn = true;
            }
        }
    }
}