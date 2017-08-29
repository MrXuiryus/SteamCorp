using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Verse;
using RimWorld;

namespace SteamCorp
{
    public class CompSteamBattery : CompSteam
    {
        private float storedSteamEnergy;

        public float AmountCanAccept
        {
            get => parent.IsBrokenDown() ? 0f : 
                (Props.storedEnergyMax - storedSteamEnergy) / Props.efficiency;
        }

        public float StoredSteamEnergy
        {
            get => storedSteamEnergy;
        }

        public float StoredEnergyPct
        {
            get => storedSteamEnergy / Props.storedEnergyMax;
        }

        public new CompProperties_SteamBattery Props
        {
            get => (CompProperties_SteamBattery)props;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<float>(ref storedSteamEnergy, "storedPower", 0f, false);
            CompProperties_SteamBattery props = Props;
            if (storedSteamEnergy > props.storedEnergyMax)
            {
                storedSteamEnergy = props.storedEnergyMax;
            }
        }

        public void AddEnergy(float amount)
        {
            if (amount < 0f)
            {
                Log.Error("Cannot add negative energy " + amount);
                return;
            }
            if (amount > AmountCanAccept)
            {
                amount = AmountCanAccept;
            }
            amount *= Props.efficiency;
            storedSteamEnergy += amount;
        }

        public void DrawPower(float amount)
        {
            storedSteamEnergy -= amount;
            if (storedSteamEnergy < 0f)
            {
                Log.Error("Drawing power we don't have from " + parent);
                storedSteamEnergy = 0f;
            }
        }

        public void SetStoredEnergyPct(float pct)
        {
            pct = Mathf.Clamp01(pct);
            storedSteamEnergy = Props.storedEnergyMax * pct;
        }

        public override void ReceiveCompSignal(string signal)
        {
            if (signal == "Breakdown")
            {
                DrawPower(StoredSteamEnergy);
            }
        }

        public override string CompInspectStringExtra()
        {
            CompProperties_SteamBattery props = Props;
            string text = string.Concat(new string[]
            {
            "PowerBatteryStored".Translate(),
            ": ",
            storedSteamEnergy.ToString("F0"),
            " / ",
            props.storedEnergyMax.ToString("F0"),
            " Wd"
            });
            string text2 = text;
            text = string.Concat(new string[]
            {
            text2,
            "\n",
            "PowerBatteryEfficiency".Translate(),
            ": ",
            (props.efficiency * 100f).ToString("F0"),
            "%"
            });
            return text + base.CompInspectStringExtra();
        }

        internal void DrawSteam(float num3)
        {
            throw new NotImplementedException();
        }
    }
}
