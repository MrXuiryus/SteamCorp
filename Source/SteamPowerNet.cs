using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace SteamCorp
{
    public class SteamPowerNet
        {
            private const int MaxRestartTryInterval = 200;

            private const int MinRestartTryInterval = 30;

            private const int ShutdownInterval = 20;

            private const float MinStoredEnergyToTurnOn = 5f;

            public SteamPipeGrid steamNetManager;

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

            public SteamPowerNet(IEnumerable<CompSteam> newTransmitters)
            {
                foreach (CompSteam comp in newTransmitters)
                {
                    this.transmitters.Add(comp);
                    comp.SteamNet = this;
                    this.RegisterAllComponentsOf(comp.parent);
                    if (comp.connectChildren != null)
                    {
                        List<CompSteam> connectChildren = comp.connectChildren;
                        for (int i = 0; i < connectChildren.Count; i++)
                        {
                            this.RegisterConnector(connectChildren[i]);
                        }
                    }
                }
                this.hasSteamSource = false;
                for (int j = 0; j < this.transmitters.Count; j++)
                {
                    if (this.IsSteamSource(this.transmitters[j]))
                    {
                        this.hasSteamSource = true;
                        break;
                    }
                }
            }

            private bool IsSteamSource(CompSteam cp)
            {
                return cp is CompSteamBattery || (cp is CompSteamTrader && cp.Props.baseSteamConsumption < 0f);
            }

            public void RegisterConnector(CompSteam b)
            {
                if (this.connectors.Contains(b))
                {
                    Log.Error("SteamNet registered connector it already had: " + b);
                    return;
                }
                this.connectors.Add(b);
                this.RegisterAllComponentsOf(b.parent);
            }

            public void DeregisterConnector(CompSteam b)
            {
                this.connectors.Remove(b);
                this.DeregisterAllComponentsOf(b.parent);
            }

            private void RegisterAllComponentsOf(ThingWithComps parentThing)
            {
                CompSteamTrader comp = parentThing.GetComp<CompSteamTrader>();
                if (comp != null)
                {
                    if (this.steamComps.Contains(comp))
                    {
                        Log.Error("SteamNet adding steamComp " + comp + " which it already has.");
                    }
                    else
                    {
                        this.steamComps.Add(comp);
                    }
                }
                CompSteamBattery comp2 = parentThing.GetComp<CompSteamBattery>();
                if (comp2 != null)
                {
                    if (this.batteryComps.Contains(comp2))
                    {
                        Log.Error("SteamNet adding batteryComp " + comp2 + " which it already has.");
                    }
                    else
                    {
                        this.batteryComps.Add(comp2);
                    }
                }
            }

            private void DeregisterAllComponentsOf(ThingWithComps parentThing)
            {
                CompSteamTrader comp = parentThing.GetComp<CompSteamTrader>();
                if (comp != null)
                {
                    this.steamComps.Remove(comp);
                }
                CompSteamBattery comp2 = parentThing.GetComp<CompSteamBattery>();
                if (comp2 != null)
                {
                    this.batteryComps.Remove(comp2);
                }
            }

            public float CurrentEnergyGainRate()
            {
                if (DebugSettings.unlimitedSteam)
                {
                    return 100000f;
                }
                float num = 0f;
                for (int i = 0; i < this.steamComps.Count; i++)
                {
                    if (this.steamComps[i].SteamOn)
                    {
                        num += this.steamComps[i].EnergyOutputPerTick;
                    }
                }
                return num;
            }

            public float CurrentStoredEnergy()
            {
                float num = 0f;
                for (int i = 0; i < this.batteryComps.Count; i++)
                {
                    num += this.batteryComps[i].StoredEnergy;
                }
                return num;
            }

            public void SteamNetTick()
            {
                float num = this.CurrentEnergyGainRate();
                float num2 = this.CurrentStoredEnergy();
                if (num2 + num >= -1E-07f && !this.steamNetManager.map.gameConditionManager.ConditionIsActive(GameConditionDefOf.SolarFlare))
                {
                    float num3;
                    if (this.batteryComps.Count > 0 && num2 >= 0.1f)
                    {
                        num3 = num2 - 5f;
                    }
                    else
                    {
                        num3 = num2;
                    }
                    if (UnityData.isDebugBuild)
                    {
                        this.debugLastApparentStoredEnergy = num3;
                        this.debugLastCreatedEnergy = num;
                        this.debugLastRawStoredEnergy = num2;
                    }
                    if (num3 + num >= 0f)
                    {
                        SteamNet.partsWantingSteamOn.Clear();
                        for (int i = 0; i < this.steamComps.Count; i++)
                        {
                            if (!this.steamComps[i].SteamOn && FlickUtility.WantsToBeOn(this.steamComps[i].parent) && !this.steamComps[i].parent.IsBrokenDown())
                            {
                                SteamNet.partsWantingSteamOn.Add(this.steamComps[i]);
                            }
                        }
                        if (SteamNet.partsWantingSteamOn.Count > 0)
                        {
                            int num4 = 200 / SteamNet.partsWantingSteamOn.Count;
                            if (num4 < 30)
                            {
                                num4 = 30;
                            }
                            if (Find.TickManager.TicksGame % num4 == 0)
                            {
                                CompSteamTrader compSteamTrader = SteamNet.partsWantingSteamOn.RandomElement<CompSteamTrader>();
                                if (num + num2 >= -(compSteamTrader.EnergyOutputPerTick + 1E-07f))
                                {
                                    compSteamTrader.SteamOn = true;
                                    num += compSteamTrader.EnergyOutputPerTick;
                                }
                            }
                        }
                    }
                    this.ChangeStoredEnergy(num);
                }
                else if (Find.TickManager.TicksGame % 20 == 0)
                {
                    SteamNet.potentialShutdownParts.Clear();
                    for (int j = 0; j < this.steamComps.Count; j++)
                    {
                        if (this.steamComps[j].SteamOn && this.steamComps[j].EnergyOutputPerTick < 0f)
                        {
                            SteamNet.potentialShutdownParts.Add(this.steamComps[j]);
                        }
                    }
                    if (SteamNet.potentialShutdownParts.Count > 0)
                    {
                        SteamNet.potentialShutdownParts.RandomElement<CompSteamTrader>().SteamOn = false;
                    }
                }
            }

            private void ChangeStoredEnergy(float extra)
            {
                if (extra > 0f)
                {
                    this.DistributeEnergyAmongBatteries(extra);
                }
                else
                {
                    float num = -extra;
                    this.givingBats.Clear();
                    for (int i = 0; i < this.batteryComps.Count; i++)
                    {
                        if (this.batteryComps[i].StoredEnergy > 1E-07f)
                        {
                            this.givingBats.Add(this.batteryComps[i]);
                        }
                    }
                    float a = num / (float)this.givingBats.Count;
                    int num2 = 0;
                    while (num > 1E-07f)
                    {
                        for (int j = 0; j < this.givingBats.Count; j++)
                        {
                            float num3 = Mathf.Min(a, this.givingBats[j].StoredEnergy);
                            this.givingBats[j].DrawSteam(num3);
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
                if (energy <= 0f || !this.batteryComps.Any<CompSteamBattery>())
                {
                    return;
                }
                SteamNet.batteriesShuffled.Clear();
                SteamNet.batteriesShuffled.AddRange(this.batteryComps);
                SteamNet.batteriesShuffled.Shuffle<CompSteamBattery>();
                int num = 0;
                while (true)
                {
                    num++;
                    if (num > 10000)
                    {
                        break;
                    }
                    float num2 = 3.40282347E+38f;
                    for (int i = 0; i < SteamNet.batteriesShuffled.Count; i++)
                    {
                        num2 = Mathf.Min(num2, SteamNet.batteriesShuffled[i].AmountCanAccept);
                    }
                    if (energy < num2 * (float)SteamNet.batteriesShuffled.Count)
                    {
                        goto IL_12F;
                    }
                    for (int j = SteamNet.batteriesShuffled.Count - 1; j >= 0; j--)
                    {
                        float amountCanAccept = SteamNet.batteriesShuffled[j].AmountCanAccept;
                        bool flag = amountCanAccept <= 0f || amountCanAccept == num2;
                        if (num2 > 0f)
                        {
                            SteamNet.batteriesShuffled[j].AddEnergy(num2);
                            energy -= num2;
                        }
                        if (flag)
                        {
                            SteamNet.batteriesShuffled.RemoveAt(j);
                        }
                    }
                    if (energy < 0.0005f || !SteamNet.batteriesShuffled.Any<CompSteamBattery>())
                    {
                        goto IL_196;
                    }
                }
                Log.Error("Too many iterations.");
                goto IL_1A0;
                IL_12F:
                float amount = energy / (float)SteamNet.batteriesShuffled.Count;
                for (int k = 0; k < SteamNet.batteriesShuffled.Count; k++)
                {
                    SteamNet.batteriesShuffled[k].AddEnergy(amount);
                }
                energy = 0f;
                IL_196:
                IL_1A0:
                SteamNet.batteriesShuffled.Clear();
            }

            public string DebugString()
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("POWERNET:");
                stringBuilder.AppendLine("  Created energy: " + this.debugLastCreatedEnergy);
                stringBuilder.AppendLine("  Raw stored energy: " + this.debugLastRawStoredEnergy);
                stringBuilder.AppendLine("  Apparent stored energy: " + this.debugLastApparentStoredEnergy);
                stringBuilder.AppendLine("  hasSteamSource: " + this.hasSteamSource);
                stringBuilder.AppendLine("  Connectors: ");
                foreach (CompSteam current in this.connectors)
                {
                    stringBuilder.AppendLine("      " + current.parent);
                }
                stringBuilder.AppendLine("  Transmitters: ");
                foreach (CompSteam current2 in this.transmitters)
                {
                    stringBuilder.AppendLine("      " + current2.parent);
                }
                stringBuilder.AppendLine("  steamComps: ");
                foreach (CompSteamTrader current3 in this.steamComps)
                {
                    stringBuilder.AppendLine("      " + current3.parent);
                }
                stringBuilder.AppendLine("  batteryComps: ");
                foreach (CompSteamBattery current4 in this.batteryComps)
                {
                    stringBuilder.AppendLine("      " + current4.parent);
                }
                return stringBuilder.ToString();
            }
        }
    }
    
}
