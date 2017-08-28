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
                (GetProps().storedEnergyMax - storedSteamEnergy) / GetProps().efficiency;
        }

        public float StoredSteamEnergy
        {
            get => storedSteamEnergy;
        }

        public float StoredEnergyPct
        {
            get => storedSteamEnergy / GetProps().storedEnergyMax;
        }

        public CompProperties_Battery GetProps()
        {
            return (CompProperties_SteamBattery)props;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<float>(ref storedSteamEnergy, "storedPower", 0f, false);
            CompProperties_Battery props = GetProps();
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
            amount *= GetProps().efficiency;
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
            storedSteamEnergy = GetProps().storedEnergyMax * pct;
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
            CompProperties_Battery props = GetProps();
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
            return text + "\n" + base.CompInspectStringExtra();
        }

        [DebuggerHidden]
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            CompPowerBattery.< CompGetGizmosExtra > c__IteratorB3 < CompGetGizmosExtra > c__IteratorB = new CompPowerBattery.< CompGetGizmosExtra > c__IteratorB3();

        < CompGetGizmosExtra > c__IteratorB.<> f__this = this;
            CompPowerBattery.< CompGetGizmosExtra > c__IteratorB3 expr_0E = < CompGetGizmosExtra > c__IteratorB;
            expr_0E.$PC = -2;
            return expr_0E;
        }
    }
}
