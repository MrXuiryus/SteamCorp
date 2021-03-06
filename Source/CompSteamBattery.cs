﻿using System;
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
            get => parent.IsSteamBrokenDown() ? 0f : 
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
            string text = string.Concat(new string[]
            {
             "Steam " + "PowerBatteryStored".Translate(),
            ": ",
            storedSteamEnergy.ToString("F0"),
            " / ",
            Props.storedEnergyMax.ToString("F0"),
            " Wd"
            });
            string text2 = text;
            text = string.Concat(new string[]
            {
            text2,
            "\n",
             "Steam " + "PowerBatteryEfficiency".Translate(),
            ": ",
            (Props.efficiency * 100f).ToString("F0"),
            "%"
            });
            return text + "\n" + base.CompInspectStringExtra();
        } 
    }
} 