using RimWorld;
using System;
using System.Text;
using Verse;
using Verse.Sound;

namespace SteamCorp
{
    public class CompSteamTrader : CompSteam
    {
        public const string PowerTurnedOnSignal = "PowerTurnedOn";

        public const string PowerTurnedOffSignal = "PowerTurnedOff";

        public Action powerStartedAction;

        public Action powerStoppedAction;

        private bool steamOn;

        private float steamPowerOutput;

        private bool powerLastOutputted;

        private Sustainer sustainerPowered;

        protected CompFlickable flickableComp;

        public float SteamPowerOutput
        {
            get => steamPowerOutput;
            set
            {
                if (value > 0f)
                {
                    powerLastOutputted = true;
                }
                if (value < 0f)
                {
                    powerLastOutputted = false;
                }
                steamPowerOutput = value;
            }
        }

        public float SteamEnergyOutputPerTick
        {
            get => SteamPowerOutput * CompPower.WattsToWattDaysPerTick;
        }

        public bool SteamOn
        {
            get => steamOn;
            set
            {
                // only trip if steamOn has changed
                if (steamOn != value)
                {
                    steamOn = value;
                    if (steamOn)
                    {
                        if (!FlickUtility.WantsToBeOn(parent))
                        {
                            Log.Warning("Tried to power on " + parent + " which did not desire it.");
                        }
                        else if (parent.IsBrokenDown())
                        {
                            Log.Warning("Tried to power on " + parent + " which is broken down.");
                            return;
                        }
                        else
                        {
                            powerStartedAction?.Invoke();
                            parent.BroadcastCompSignal("PowerTurnedOn");
                            SoundDef soundDef = ((CompProperties_Power)parent.def.CompDefForAssignableFrom<CompPowerTrader>()).soundPowerOn;
                            if (soundDef.NullOrUndefined())
                            {
                                soundDef = SoundDefOf.PowerOnSmall;
                            }
                            soundDef.PlayOneShot(new TargetInfo(parent.Position, parent.Map, false));
                            StartSustainerPoweredIfInactive();
                        }
                    }
                    else
                    {
                        powerStoppedAction?.Invoke();
                        parent.BroadcastCompSignal("PowerTurnedOff");
                        SoundDef soundDef2 = ((CompProperties_Power)parent.def.CompDefForAssignableFrom<CompPowerTrader>()).soundPowerOff;
                        if (soundDef2.NullOrUndefined())
                        {
                            soundDef2 = SoundDefOf.PowerOffSmall;
                        }
                        if (parent.Spawned)
                        {
                            soundDef2.PlayOneShot(new TargetInfo(parent.Position, parent.Map, false));
                        }
                        EndSustainerPoweredIfActive();
                        }
                    }
            }
        }

        public string DebugString
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(parent.LabelCap + " CompPower:");
                stringBuilder.AppendLine("   PowerOn: " + SteamOn);
                stringBuilder.AppendLine("   energyProduction: " + SteamPowerOutput);
                return stringBuilder.ToString();
            }
        }

        public override void ReceiveCompSignal(string signal)
        {
            if (signal == "FlickedOff" || signal == "ScheduledOff" || signal == "Breakdown")
            {
                SteamOn = false;
            }
            if (signal == "RanOutOfFuel" && powerLastOutputted)
            {
                SteamOn = false;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            flickableComp = parent.GetComp<CompFlickable>();
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            EndSustainerPoweredIfActive();
            steamPowerOutput = 0f;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref steamOn, "powerOn", true, false);
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if (!parent.IsBrokenDown())
            {
                if (flickableComp != null && !flickableComp.SwitchIsOn)
                {
                    parent.Map.overlayDrawer.DrawOverlay(parent, OverlayTypes.PowerOff);
                }
                else if (FlickUtility.WantsToBeOn(parent) && !SteamOn)
                {
                    parent.Map.overlayDrawer.DrawOverlay(parent, OverlayTypes.NeedsPower);
                }
            }
        }

        public override void SetUpSteamPowerVars()
        {
            base.SetUpSteamPowerVars();
            CompProperties_Steam props = base.Props;
            SteamPowerOutput = -1f * props.baseSteamConsumption;
            powerLastOutputted = (props.baseSteamConsumption <= 0f);
        }

        public override void ResetPowerVars()
        {
            base.ResetPowerVars();
            steamOn = false;
            steamPowerOutput = 0f;
            powerLastOutputted = false;
            sustainerPowered = null;
            if (flickableComp != null)
            {
                flickableComp.ResetToOn();
            }
        }

        public override void LostConnectParent()
        {
            base.LostConnectParent();
            SteamOn = false;
        }

        public override string CompInspectStringExtra()
        {
            string str;
            if (powerLastOutputted)
            {
                str = "PowerOutput".Translate() + ": " + SteamPowerOutput.ToString("#####0") + " W";
            }
            else
            {
                str = "PowerNeeded".Translate() + ": " + (-SteamPowerOutput).ToString("#####0") + " W";
            }
            return str + base.CompInspectStringExtra();
        }

        private void StartSustainerPoweredIfInactive()
        {
            CompProperties_Steam props = base.Props;
            if (!props.soundAmbientPowered.NullOrUndefined() && sustainerPowered == null)
            {
                SoundInfo info = SoundInfo.InMap(parent, MaintenanceType.None);
                sustainerPowered = props.soundAmbientPowered.TrySpawnSustainer(info);
            }
        }

        private void EndSustainerPoweredIfActive()
        {
            if (sustainerPowered != null)
            {
                sustainerPowered.End();
                sustainerPowered = null;
            }
        }
    }
}