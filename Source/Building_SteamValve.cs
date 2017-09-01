using System.Text;
using RimWorld;
using Verse;

namespace SteamCorp
{
    class Building_SteamValve : Building_SteamSprayer
    {
        private bool wantsOnOld = true;

        private CompFlickable flickableComp;

        public override bool TransmitsPowerNow
        { 
            get
            {
#if DEBUG
                Log.Message("TransmitsPowerNow " + FlickUtility.WantsToBeOn(this));
#endif
                return FlickUtility.WantsToBeOn(this);
            }
        }

        public override Graphic Graphic
        {
            get { return flickableComp.CurrentGraphic; }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            flickableComp = GetComp<CompFlickable>();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (flickableComp == null)
                {
                    flickableComp = GetComp<CompFlickable>();
                }
                wantsOnOld = !FlickUtility.WantsToBeOn(this);
                UpdatePowerGrid();
            }
        } 

        protected override void ReceiveCompSignal(string signal)
        {
            if (signal == "FlickedOff" || signal == "FlickedOn" || signal == "ScheduledOn" || signal == "ScheduledOff")
            {
                UpdatePowerGrid();
            }
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());
            if (stringBuilder.Length != 0)
            {
                stringBuilder.AppendLine();
            }
            stringBuilder.Append("PowerSwitch_Power".Translate() + ": ");
            if (FlickUtility.WantsToBeOn(this))
            {
                stringBuilder.Append("On".Translate().ToLower());
            }
            else
            {
                stringBuilder.Append("Off".Translate().ToLower());
            }
            return stringBuilder.ToString();
        }

        private void UpdatePowerGrid()
        {
            if (FlickUtility.WantsToBeOn(this) != wantsOnOld)
            {
                if (Spawned)
                {
                    StaticSteamNetManager.Manager.Notfiy_TransmitterTransmitsPowerNowChanged(GetComp<CompSteam>());
                }
                wantsOnOld = FlickUtility.WantsToBeOn(this);
            }
        }
    }
}