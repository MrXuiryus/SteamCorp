using RimWorld;
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

        public CompSteam connectParent;
        public List<CompSteam> connectChildren;
        private SteamPowerNet steamNet;

        public SteamPowerNet SteamNet {
            get => steamNet ?? ((connectParent != null) ? connectParent.SteamNet : null);
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
            base.PostSpawnSetup(respawningAfterLoad);
            if (Props.transmitsSteam || parent.def.ConnectToPower)
            {
                if (Props.transmitsSteam)
                {
                    steamNet.SteamNetManager.Notify_TransmitterSpawned(this);
                }
                if (Props.connectToSteam)
                {
                    steamNet.SteamNetManager.Notify_ConnectorWantsConnect(this);
                }
                SetUpSteamPowerVars();
            }
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            if (Props.transmitsSteam || parent.def.ConnectToPower)
            {
                if (Props.transmitsSteam)
                {
                    if (connectChildren != null)
                    {
                        foreach (CompSteam child in connectChildren)
                        {
                            child.LostConnectParent();
                        }
                    }
                }
            }
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
                PowerNetGraphics.PrintWirePieceConnecting(layer, parent, connectParent.parent, false);
            }
        }

        public override void CompPrintForPowerGrid(SectionLayer layer)
        {
            if (TransmitsSteamPowerNow)
            {
                PowerOverlayMats.LinkedOverlayGraphic.Print(layer, parent);
            }
            if (parent.def.ConnectToPower)
            {
                PowerNetGraphics.PrintOverlayConnectorBaseFor(layer, parent);
            }
            if (connectParent != null)
            {
                PowerNetGraphics.PrintWirePieceConnecting(layer, parent, connectParent.parent, true);
            }
        }

        public bool TransmitsSteamPowerNow
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
            string text = (SteamNet.CurrentEnergyGainRate() / CompSteam.WattsToWattDaysPerTick).ToString("F0");
            string text2 = SteamNet.CurrentStoredEnergy().ToString("F0");
            return "PowerConnectedRateStored".Translate(new object[]
            {
                text,
                text2
            });
        }
    }
}
