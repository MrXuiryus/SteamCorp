using RimWorld;
using Verse;

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
            parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.PowerGrid, false, false);
            parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.PowerGrid, false, false);
            parent.Map.powerNetManager.Notify_TransmitterSpawned(parent.GetComp<CompPower>());
            parent.GetComp<CompPower>().SetUpPowerVars();
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            if (parent.GetComp<CompPower>() == null)
            {
                return;
            }

            foreach (CompPower child in parent.GetComp<CompPower>().connectChildren)
            {
                child.LostConnectParent();
            }
            map.powerNetManager.Notify_TransmitterDespawned(parent.GetComp<CompPower>());
            map.mapDrawer.MapMeshDirty(this.parent.Position, MapMeshFlag.PowerGrid, true, false);
        }

        public override void CompTick()
        { 
            base.CompTick();
            UpdateDesiredPowerOutput();
            //parent.GetComp<CompPower>()?.PowerNet?.DeregisterConnector(parent.GetComp<CompPower>());
        }
         
        public override void UpdateDesiredPowerOutput()
        {   
            if ((breakdownableComp != null && breakdownableComp.BrokenDown)
                || (flickableComp != null && !flickableComp.SwitchIsOn))
            {
                SteamOn = parent.GetComp<CompPowerPlant>().PowerOn = false;
            }
            else
            {
                parent.GetComp<CompPowerPlant>().PowerOn = SteamOn;
            }
        }
    }
}