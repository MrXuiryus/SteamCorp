﻿using System;
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
            get => steamNet ?? connectParent.SteamNet;
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

        /* not doing graphics for now
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
            if (this.TransmitsPowerNow)
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
        }*/

        public bool TransmitsSteamNow
        {
            get => ((Building)parent).TransmitsPowerNow;
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
    }
}
