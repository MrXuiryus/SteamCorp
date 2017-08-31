using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SteamCorp
{
    [StaticConstructorOnStartup]
    class Building_SteamBattery : Building_SteamSprayer
    {
        private const float MinEnergyToExplode = 500f;

        private const float EnergyToLoseWhenExplode = 400f;

        private const float ExplodeChancePerDamage = 0.05f;

        private int ticksToExplode;

        private Sustainer wickSustainer;

        private static readonly Vector2 BarSize = new Vector2(1.3f, 0.4f);

        private static readonly Material BatteryBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.9f, 0.85f, 0.2f), false);

        private static readonly Material BatteryBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f), false);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref ticksToExplode, "ticksToExplode", 0, false);
        }

        public override void Draw()
        {
            base.Draw();
            CompSteamBattery comp = GetComp<CompSteamBattery>();
            GenDraw.FillableBarRequest r = new GenDraw.FillableBarRequest
            {
                center = DrawPos + Vector3.up * 0.1f,
                size = BarSize,
                fillPercent = comp.StoredSteamEnergy / comp.Props.storedEnergyMax,
                filledMat = SolidColorMaterials.SimpleSolidColorMaterial(Color.cyan, false),
                unfilledMat = BatteryBarUnfilledMat,
                margin = 0.15f
            };
            Rot4 rotation = Rotation;
            rotation.Rotate(RotationDirection.Clockwise);
            r.rotation = rotation;
            GenDraw.DrawFillableBar(r);
            if (ticksToExplode > 0 && Spawned)
            {
                base.Map.overlayDrawer.DrawOverlay(this, OverlayTypes.BurningWick);
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (ticksToExplode > 0)
            {
                if (wickSustainer == null)
                {
                    StartWickSustainer();
                }
                else
                {
                    wickSustainer.Maintain();
                }
                ticksToExplode--;
                if (ticksToExplode == 0)
                {
                    IntVec3 randomCell = this.OccupiedRect().RandomCell;
                    float radius = Rand.Range(0.5f, 1f) * 3f;
                    GenExplosion.DoExplosion(randomCell, Map, radius, DamageDefOf.Flame, null, null, null, null, null, 0f, 1, false, null, 0f, 1);
                    GetComp<CompPowerBattery>().DrawPower(400f);
                }
            }
        }

        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            if (!Destroyed && ticksToExplode == 0 && dinfo.Def == DamageDefOf.Flame && Rand.Value < 0.05f && GetComp<CompPowerBattery>().StoredEnergy > 500f)
            {
                ticksToExplode = Rand.Range(70, 150);
                StartWickSustainer();
            }
        }

        private void StartWickSustainer()
        {
            SoundInfo info = SoundInfo.InMap(this, MaintenanceType.PerTick);
            wickSustainer = SoundDefOf.HissSmall.TrySpawnSustainer(info);
        }
    }
}
    