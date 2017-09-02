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
                parent.GetComp<CompPowerPlant>().PowerOn = false;
                SteamOn = true;
            } 
        } 

        public override void CompTick()
        { 
            base.CompTick();
            UpdateDesiredPowerOutput();
            parent.GetComp<CompPower>()?.PowerNet?.DeregisterConnector(parent.GetComp<CompPower>());
        }
         
        public override void UpdateDesiredPowerOutput()
        {   
            base.UpdateDesiredPowerOutput();
            if ((breakdownableComp != null && breakdownableComp.BrokenDown)
                || (flickableComp != null && !flickableComp.SwitchIsOn))
            {
                parent.GetComp<CompPowerPlant>().PowerOn = false;
            }
            else if (SteamNet != null && SteamNet.CurrentStoredEnergy() <=   0)
            {
                parent.GetComp<CompPowerPlant>().PowerOn = false;
            }
            else
            {
                parent.GetComp<CompPowerPlant>().PowerOn = true;
            }
        }
    }
}