﻿using RimWorld;
using System.Collections.Generic;
using Verse;

namespace SteamCorp
{
    public class CompSteam : ThingComp
    {
        public static readonly float WattsToWattDaysPerTick = 1.66666669E-05f;

        public CompSteam connectParent;
        public List<CompSteam> connectChildren;
        private SteamPowerNet steamNet;

        public SteamPowerNet SteamNet {
            get => steamNet ?? (connectParent?.SteamNet);
            set => steamNet = value;
        }
        
        public CompProperties_Steam Props
        {
            get => (CompProperties_Steam)props;
        }

        
        public virtual void SetUpSteamPowerVars()
        { }

        public virtual void ResetPowerVars()
        {
            steamNet = null;
            connectParent = null;
            connectChildren = null;
        }

        public override void PostExposeData()
        {
            Thing thing = null;
            if (Scribe.mode == LoadSaveMode.Saving && connectParent != null)
            {
                thing = connectParent.parent;
            }
            Scribe_References.Look<Thing>(ref thing, "parentThing", false);
            if (thing != null)
            {
                connectParent = ((ThingWithComps)thing).GetComp<CompSteam>();
            }
            if (Scribe.mode == LoadSaveMode.PostLoadInit && connectParent != null)
            {
                ConnectToTransmitter(connectParent, true);
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            //delete pipes already installed underneath building
            foreach (IntVec3 cell in parent.OccupiedRect().Cells)
            {
                foreach (Thing t in parent.Map.thingGrid.ThingsAt(cell))
                {
                    Building building = t as Building;
                    if (building != null && building.Label == "Steam Pipe" && building.Label != parent.Label)
                    {
                        building.DeSpawn();
                    }
                }
            }
            base.PostSpawnSetup(respawningAfterLoad);
            parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.PowerGrid, true, false);
            StaticManager.Net.Notify_TransmitterSpawned(this);
            SetUpSteamPowerVars();
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            if (Props.transmitsSteam || parent.def.ConnectToPower)
            {
                if (Props.transmitsSteam && connectChildren != null)
                {
                    foreach (CompSteam child in connectChildren)
                    {
                        child.LostConnectParent();
                    }
                }
            }
            if(parent.GetComp<CompGlower>() != null)
            {               
                Find.VisibleMap.glowGrid.DeRegisterGlower(parent.GetComp<CompGlower>());
            }
            if (Props.transmitsSteam)
            {
                StaticManager.Net.Notify_TransmitterDespawned(this);
            }
            StaticManager.Net.Notify_ConnectorDespawned(this);
            map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.PowerGrid, true, false);
        }

        public virtual void LostConnectParent()
        {
            connectParent = null;
        }
        
        public override void PostPrintOnto(SectionLayer layer)
        {
            base.PostPrintOnto(layer);
            if (connectParent != null)
            {
                SteamNetGraphics.PrintWirePieceConnecting(layer, parent, connectParent.parent, true);
            }
        }

        public override void CompPrintForPowerGrid(SectionLayer layer)
        {
            if (TransmitsSteamPower)
            {
                PowerOverlayMats.LinkedOverlayGraphic.Print(layer, parent);
            }
            if (parent.def.ConnectToPower)
            {
                SteamNetGraphics.PrintOverlayConnectorBaseFor(layer, parent);
            }
            if (connectParent != null)
            {
                SteamNetGraphics.PrintWirePieceConnecting(layer, parent, connectParent.parent, true);
            }
        }

        public bool TransmitsSteamPower
        {
            get => Props.transmitsSteam;
        }

        public void ConnectToTransmitter(CompSteam transmitter, bool reconnectingAfterLoading = false)
        { 
            connectParent = transmitter;
            if (connectParent.connectChildren == null)
            {
                connectParent.connectChildren = new List<CompSteam>();
            }
            transmitter.connectChildren.Add(this);
            if (steamNet != null)
            {
                steamNet.RegisterConnector(this);
            }
        }

        public override string CompInspectStringExtra()
        {
            if (SteamNet == null)
            {
                return "PowerNotConnected".Translate();
            }
            string text = (SteamNet.CurrentEnergyGainRate() / WattsToWattDaysPerTick).ToString("F0");
            string text2 = SteamNet.CurrentStoredEnergy().ToString("F0");
            return "Steam " + "PowerConnectedRateStored".Translate(new object[]
            {
                text,
                text2
            });
        }
    } 
}
