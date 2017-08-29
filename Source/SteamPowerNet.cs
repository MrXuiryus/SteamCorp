using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace SteamCorp
{
    public class SteamPowerNet
    {
        private const int MaxRestartTryInterval = 200;

        private const int MinRestartTryInterval = 30;

        private const int ShutdownInterval = 20;

        private const float MinStoredEnergyToTurnOn = 5f;

        private SteamNetManager steamNetManager;

        public bool hasSteamSource;

        public List<CompSteam> connectors = new List<CompSteam>();

        public List<CompSteam> transmitters = new List<CompSteam>();

        public List<CompSteamTrader> steamComps = new List<CompSteamTrader>();

        public List<CompSteamBattery> batteryComps = new List<CompSteamBattery>();

        private float debugLastCreatedEnergy;

        private float debugLastRawStoredEnergy;

        private float debugLastApparentStoredEnergy;

        private static List<CompSteamTrader> partsWantingSteamOn = new List<CompSteamTrader>();

        private static List<CompSteamTrader> potentialShutdownParts = new List<CompSteamTrader>();

        private List<CompSteamBattery> givingBats = new List<CompSteamBattery>();

        private static List<CompSteamBattery> batteriesShuffled = new List<CompSteamBattery>();

        public SteamNetManager SteamNetManager { get => steamNetManager; set => steamNetManager = value; }

        public SteamPowerNet(IEnumerable<CompSteam> newTransmitters)
        {
            foreach (CompSteam comp in newTransmitters)
            {
            transmitters.Add(comp);
                comp.SteamNet = this;
            RegisterAllComponentsOf(comp.parent);
                if (comp.connectChildren != null)
                {
                    List<CompSteam> connectChildren = comp.connectChildren;
                    for (int i = 0; i < connectChildren.Count; i++)
                    {
                    RegisterConnector(connectChildren[i]);
                    }
                }
            }
            hasSteamSource = false;
            for (int j = 0; j < transmitters.Count; j++)
            {
                if (IsSteamPowerSource(transmitters[j]))
                {
                hasSteamSource = true;
                    break;
                }
            }
        }

        private bool IsSteamPowerSource(CompSteam comp)
        {
            return comp is CompSteamBattery || 
            (comp is CompSteamTrader && ((CompProperties_Steam)comp.props).baseSteamConsumption < 0f);
        }

        public void RegisterConnector(CompSteam b)
        {
            Log.Message("Trying to register connector " + b);
            if (!connectors.Contains(b))
            {
                connectors.Add(b);
                RegisterAllComponentsOf(b.parent);
            }
        }

        public void DeregisterConnector(CompSteam b)
        {
            connectors.Remove(b);
            DeregisterAllComponentsOf(b.parent);
        }

        private void RegisterAllComponentsOf(ThingWithComps parentThing)
        {
            CompSteamTrader traderComp = parentThing.GetComp<CompSteamTrader>();
            if (traderComp != null)
            {
                if (!steamComps.Contains(traderComp))
                {
                    steamComps.Add(traderComp);
                }
            }

            CompSteamBattery batteryComp = parentThing.GetComp<CompSteamBattery>();
            if (batteryComp != null)
            {
                if (!batteryComps.Contains(batteryComp))
                {
                    batteryComps.Add(batteryComp);
                }
            }
        }

        private void DeregisterAllComponentsOf(ThingWithComps parentThing)
        {
            CompSteamTrader traderComp = parentThing.GetComp<CompSteamTrader>();
            if (traderComp != null)
            {
                steamComps.Remove(traderComp);
            }

            CompSteamBattery batteryComp = parentThing.GetComp<CompSteamBattery>();
            if (batteryComp != null)
            {
                batteryComps.Remove(batteryComp);
            }
        }

        public float CurrentEnergyGainRate()
        {
            if (DebugSettings.unlimitedPower)
            {
                return 100000f;
            }
            float num = 0f;
            for (int i = 0; i < steamComps.Count; i++)
            {
                if (steamComps[i].SteamOn)
                {
                    num += steamComps[i].SteamEnergyOutputPerTick;
                }
            }
            return num;
        }

        public float CurrentStoredEnergy()
        {
            float num = 0f;
            for (int i = 0; i < batteryComps.Count; i++)
            {
                num += batteryComps[i].StoredSteamEnergy;
            }
            return num;
        }

        public void SteamNetTick()
        {
            float num = CurrentEnergyGainRate();
            float num2 = CurrentStoredEnergy();
            if (num2 + num >= -1E-07f)
            {
                float num3;
                if (batteryComps.Count > 0 && num2 >= 0.1f)
                {
                    num3 = num2 - 5f;
                }
                else
                {
                    num3 = num2;
                }
                if (UnityData.isDebugBuild)
                {
                debugLastApparentStoredEnergy = num3;
                debugLastCreatedEnergy = num;
                debugLastRawStoredEnergy = num2;
                }
                if (num3 + num >= 0f)
                {
                    partsWantingSteamOn.Clear();
                    for (int i = 0; i < steamComps.Count; i++)
                    {
                        if (!steamComps[i].SteamOn && FlickUtility.WantsToBeOn(steamComps[i].parent) && !steamComps[i].parent.IsBrokenDown())
                        {
                            partsWantingSteamOn.Add(steamComps[i]);
                        }
                    }
                    if (partsWantingSteamOn.Count > 0)
                    {
                        int num4 = 200 / partsWantingSteamOn.Count;
                        if (num4 < 30)
                        {
                            num4 = 30;
                        }
                        if (Find.TickManager.TicksGame % num4 == 0)
                        {
                            CompSteamTrader compSteamTrader = partsWantingSteamOn.RandomElement<CompSteamTrader>();
                            if (num + num2 >= -(compSteamTrader.SteamEnergyOutputPerTick + 1E-07f))
                            {
                                compSteamTrader.SteamOn = true;
                                num += compSteamTrader.SteamEnergyOutputPerTick;
                            }
                        }
                    }
                }
            ChangeStoredEnergy(num);
            }
            else if (Find.TickManager.TicksGame % 20 == 0)
            {
                potentialShutdownParts.Clear();
                for (int j = 0; j < steamComps.Count; j++)
                {
                    if (steamComps[j].SteamOn && steamComps[j].SteamEnergyOutputPerTick < 0f)
                    {
                        potentialShutdownParts.Add(steamComps[j]);
                    }
                }
                if (potentialShutdownParts.Count > 0)
                {
                    potentialShutdownParts.RandomElement<CompSteamTrader>().SteamOn = false;
                }
            }
        }

        private void ChangeStoredEnergy(float extra)
        {
            if (extra > 0f)
            {
            DistributeEnergyAmongBatteries(extra);
            }
            else
            {
                float num = -extra;
            givingBats.Clear();
                for (int i = 0; i < batteryComps.Count; i++)
                {
                    if (batteryComps[i].StoredSteamEnergy > 1E-07f)
                    {
                    givingBats.Add(batteryComps[i]);
                    }
                }
                float a = num / (float)givingBats.Count;
                int num2 = 0;
                while (num > 1E-07f)
                {
                    for (int j = 0; j < givingBats.Count; j++)
                    {
                        float num3 = Mathf.Min(a, givingBats[j].StoredSteamEnergy);
                        givingBats[j].DrawSteam(num3);
                        num -= num3;
                        if (num < 1E-07f)
                        {
                            return;
                        }
                    }
                    num2++;
                    if (num2 > 10)
                    {
                        break;
                    }
                }
                if (num > 1E-07f)
                {
                    Log.Warning("Drew energy from a SteamNet that didn't have it.");
                }
            }
        }

        private void DistributeEnergyAmongBatteries(float energy)
        {
            if (energy <= 0f || !batteryComps.Any<CompSteamBattery>())
            {
                return;
            }

            batteriesShuffled.Clear();
            batteriesShuffled.AddRange(batteryComps);
            batteriesShuffled.Shuffle<CompSteamBattery>();

            //magic numbers imported from rimworld's PowerNet.cs
            for(int num = 1; num < 10000; num++)
            {
                float num2 = 3.40282347E+38f;
                for (int i = 0; i < batteriesShuffled.Count; i++)
                {
                    num2 = Mathf.Min(num2, batteriesShuffled[i].AmountCanAccept);
                }

                //if energy is less than the least amount acceptable * total batteries:
                if (energy < num2 * (float)batteriesShuffled.Count)
                {
                    float amount = energy / (float)batteriesShuffled.Count;
                    for (int k = 0; k < batteriesShuffled.Count; k++)
                    {
                        batteriesShuffled[k].AddEnergy(amount);
                    }
                    energy = 0f;
                    return;
                }
                else
                {
                    foreach (CompSteamBattery battery in batteriesShuffled)
                    {
                        if (num2 > 0f)
                        {
                            battery.AddEnergy(num2);
                            energy -= num2;
                        }
                        if (battery.AmountCanAccept <= 0f || battery.AmountCanAccept == num2)
                        {
                            batteriesShuffled.Remove(battery);
                        }
                    }
                    if (energy < 0.0005f || !batteriesShuffled.Any<CompSteamBattery>())
                    {
                        batteriesShuffled.Clear();
                        return;
                    }
                }
            }
        }

        public string DebugString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("POWERNET:");
            stringBuilder.AppendLine("  Created energy: " + debugLastCreatedEnergy);
            stringBuilder.AppendLine("  Raw stored energy: " + debugLastRawStoredEnergy);
            stringBuilder.AppendLine("  Apparent stored energy: " + debugLastApparentStoredEnergy);
            stringBuilder.AppendLine("  hasSteamSource: " + hasSteamSource);
            stringBuilder.AppendLine("  Connectors: ");
            foreach (CompSteam current in connectors)
            {
                stringBuilder.AppendLine("      " + current.parent);
            }
            stringBuilder.AppendLine("  Transmitters: ");
            foreach (CompSteam current2 in transmitters)
            {
                stringBuilder.AppendLine("      " + current2.parent);
            }
            stringBuilder.AppendLine("  steamComps: ");
            foreach (CompSteamTrader current3 in steamComps)
            {
                stringBuilder.AppendLine("      " + current3.parent);
            }
            stringBuilder.AppendLine("  batteryComps: ");
            foreach (CompSteamBattery current4 in batteryComps)
            {
                stringBuilder.AppendLine("      " + current4.parent);
            }
            return stringBuilder.ToString();
        }
    }
}
