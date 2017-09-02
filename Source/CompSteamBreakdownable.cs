using Verse;
using RimWorld;

namespace SteamCorp
{
    public class CompSteamBreakdownable : ThingComp
    {
        private const int BreakdownMTBTicks = 13680000;

        public const string BreakdownSignal = "Breakdown";

        private bool brokenDownInt;

        private CompSteamTrader traderComp;

        public bool BrokenDown
        {
            get => brokenDownInt;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref brokenDownInt, "brokenDown", false, false);
        }

        public override void PostDraw()
        {
            if (brokenDownInt)
            {
                parent.Map.overlayDrawer.DrawOverlay(parent, OverlayTypes.BrokenDown);
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            traderComp = parent.GetComp<CompSteamTrader>();
            parent.Map.GetComponent<SteamBreakdownManager>().Register(this);
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            map.GetComponent<SteamBreakdownManager>().Deregister(this);
        }

        public void CheckForBreakdown()
        {
            if (CanBreakdownNow() && Rand.MTBEventOccurs(1.368E+07f, 1f, 1041f))
            {
                DoBreakdown();
            }
        }

        protected bool CanBreakdownNow()
        {
            return !BrokenDown && (traderComp == null || traderComp.SteamOn);
        }

        public void Notify_Repaired()
        {
            brokenDownInt = false;
            parent.Map.GetComponent<SteamBreakdownManager>().Notify_Repaired(parent);
            if (parent is Building_PowerSwitch)
            {
                parent.Map.powerNetManager.Notfiy_TransmitterTransmitsPowerNowChanged(parent.GetComp<CompPower>());
            }
        }

        public void DoBreakdown()
        {
            brokenDownInt = true;
            parent.BroadcastCompSignal("Breakdown");
            StaticManager.Breakdowns.Notify_BrokenDown(parent);
            if (parent.Faction == Faction.OfPlayer)
            {
                Find.LetterStack.ReceiveLetter("LetterLabelBuildingBrokenDown".Translate(new object[]
                {
                    parent.LabelShort
                }), "LetterBuildingBrokenDown".Translate(new object[]
                {
                    parent.LabelShort
                }), LetterDefOf.BadNonUrgent, parent, null);
            }
        }

        public override string CompInspectStringExtra()
        {
            if (BrokenDown)
            {
                return "BrokenDown".Translate();
            }
            return null;
        }
    }
}