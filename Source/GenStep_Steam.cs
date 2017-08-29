using System;
using System.Collections.Generic;
using Verse;
using UnityEngine;
using RimWorld;
using Verse.AI;

namespace SteamCorp
{
    public class GenStep_Steam : GenStep
    {
        private const int MaxDistToExistingNetForTurrets = 1;

        public bool canSpawnBatteries = true;

        public bool canSpawnPowerGenerators = true;

        public FloatRange newBatteriesInitialStoredEnergyPctRange = new FloatRange(0.2f, 0.5f);

        private List<Thing> tmpThings = new List<Thing>();

        private List<IntVec3> tmpCells = new List<IntVec3>();

        private static readonly IntRange MaxDistanceBetweenBatteryAndTransmitter = new IntRange(20, 50);

        private Dictionary<SteamPowerNet, bool> tmpPowerNetPredicateResults = new Dictionary<SteamPowerNet, bool>();

        private static List<IntVec3> tmpTransmitterCells = new List<IntVec3>();

        public override void Generate(Map map)
        {
            Generate(map, StaticSteamNetManager.Manager);
        }

        public void Generate(Map map, SteamNetManager manager)
        {
            Log.Message("Generate2");
            manager.UpdatePowerNetsAndConnections_First();
            UpdateDesiredPowerOutputForAllGenerators(map);
            EnsureBatteriesConnectedAndMakeSense(map);
            EnsurePowerUsersConnected(map);
            EnsureGeneratorsConnectedAndMakeSense(map);
            tmpThings.Clear();
        }

        private void UpdateDesiredPowerOutputForAllGenerators(Map map)
        {
            tmpThings.Clear();
            tmpThings.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial));
            for (int i = 0; i < tmpThings.Count; i++)
            {
                if (IsPowerGenerator(tmpThings[i]))
                {
                    CompSteamPowerPlant compPowerPlant = tmpThings[i].TryGetComp<CompSteamPowerPlant>();
                    if (compPowerPlant != null)
                    {
                        compPowerPlant.UpdateDesiredPowerOutput();
                    }
                }
            }
        }

        private void EnsureBatteriesConnectedAndMakeSense(Map map)
        {
            tmpThings.Clear();
            tmpThings.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial));
            for (int i = 0; i < tmpThings.Count; i++)
            {
                CompSteamBattery compPowerBattery = tmpThings[i].TryGetComp<CompSteamBattery>();
                if (compPowerBattery != null)
                {
                    SteamPowerNet powerNet = compPowerBattery.SteamNet;
                    if (powerNet == null || !HasAnyPowerGenerator(powerNet))
                    {
                        map.powerNetManager.UpdatePowerNetsAndConnections_First();
                        SteamPowerNet powerNet2;
                        IntVec3 dest;
                        Building building2;
                        if (TryFindClosestReachableNet(compPowerBattery.parent.Position, (SteamPowerNet x) => HasAnyPowerGenerator(x), map, out powerNet2, out dest))
                        {
                            map.floodFiller.ReconstructLastFloodFillPath(dest, tmpCells);
                            if (canSpawnPowerGenerators)
                            {
                                int count = tmpCells.Count;
                                float chance = Mathf.InverseLerp((float)MaxDistanceBetweenBatteryAndTransmitter.min, (float)MaxDistanceBetweenBatteryAndTransmitter.max, (float)count);
                                Building building;
                                if (Rand.Chance(chance) && TrySpawnPowerGeneratorNear(compPowerBattery.parent.Position, map, compPowerBattery.parent.Faction, out building))
                                {
                                SpawnTransmitters(compPowerBattery.parent.Position, building.Position, map, compPowerBattery.parent.Faction);
                                    powerNet2 = null;
                                }
                            }
                            if (powerNet2 != null)
                            {
                            SpawnTransmitters(tmpCells, map, compPowerBattery.parent.Faction);
                            }
                        }
                        else if (canSpawnPowerGenerators && TrySpawnPowerGeneratorNear(compPowerBattery.parent.Position, map, compPowerBattery.parent.Faction, out building2))
                        {
                            SpawnTransmitters(compPowerBattery.parent.Position, building2.Position, map, compPowerBattery.parent.Faction);
                        }
                    }
                }
            }
        }

        private void EnsurePowerUsersConnected(Map map)
        {
            tmpThings.Clear();
            tmpThings.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial));
            for (int i = 0; i < tmpThings.Count; i++)
            {
                if (IsPowerUser(tmpThings[i]))
                {
                    CompSteamTrader compSteamTrader = tmpThings[i].TryGetComp<CompSteamTrader>();
                    SteamPowerNet powerNet = compSteamTrader.SteamNet;
                    if (powerNet != null && powerNet.hasSteamSource)
                    {
                        TryTurnOnImmediately(compSteamTrader, map);
                    }
                    else
                    {
                        map.powerNetManager.UpdatePowerNetsAndConnections_First();
                        SteamPowerNet powerNet2;
                        IntVec3 dest;
                        Building building;
                        if (TryFindClosestReachableNet(compSteamTrader.parent.Position, (SteamPowerNet x) => x.CurrentEnergyGainRate() - compSteamTrader.Props.baseSteamConsumption * CompSteam.WattsToWattDaysPerTick > 1E-07f, map, out powerNet2, out dest))
                        {
                            map.floodFiller.ReconstructLastFloodFillPath(dest, tmpCells);
                            bool flag = false;
                            if (canSpawnPowerGenerators && tmpThings[i] is Building_Turret && tmpCells.Count > 13)
                            {
                                flag = TrySpawnPowerGeneratorAndBatteryIfCanAndConnect(tmpThings[i], map);
                            }
                            if (!flag)
                            {
                            SpawnTransmitters(tmpCells, map, tmpThings[i].Faction);
                            }
                        TryTurnOnImmediately(compSteamTrader, map);
                        }
                        else if (canSpawnPowerGenerators && TrySpawnPowerGeneratorAndBatteryIfCanAndConnect(tmpThings[i], map))
                        {
                        TryTurnOnImmediately(compSteamTrader, map);
                        }
                        else if (TryFindClosestReachableNet(compSteamTrader.parent.Position, (SteamPowerNet x) => x.CurrentStoredEnergy() > 1E-07f, map, out powerNet2, out dest))
                        {
                            map.floodFiller.ReconstructLastFloodFillPath(dest, tmpCells);
                        SpawnTransmitters(tmpCells, map, tmpThings[i].Faction);
                        }
                        else if (canSpawnBatteries && TrySpawnBatteryNear(tmpThings[i].Position, map, tmpThings[i].Faction, out building))
                        {
                        SpawnTransmitters(tmpThings[i].Position, building.Position, map, tmpThings[i].Faction);
                        if (building.GetComp<CompPowerBattery>().StoredEnergy > 0f)
                        {
                            TryTurnOnImmediately(compSteamTrader, map);
                        }
                        }
                    }
                }
            }
        }

        private void EnsureGeneratorsConnectedAndMakeSense(Map map)
        {
            tmpThings.Clear();
            tmpThings.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial));
            for (int i = 0; i < tmpThings.Count; i++)
            {
                if (IsPowerGenerator(tmpThings[i]))
                {
                    SteamPowerNet powerNet = tmpThings[i].TryGetComp<CompSteam>().SteamNet;
                    if (powerNet == null || !HasAnyPowerUser(powerNet))
                    {
                        map.powerNetManager.UpdatePowerNetsAndConnections_First();
                        SteamPowerNet powerNet2;
                        IntVec3 dest;
                        if (TryFindClosestReachableNet(tmpThings[i].Position, (SteamPowerNet x) => HasAnyPowerUser(x), map, out powerNet2, out dest))
                        {
                            map.floodFiller.ReconstructLastFloodFillPath(dest, tmpCells);
                        SpawnTransmitters(tmpCells, map, tmpThings[i].Faction);
                        }
                    }
                }
            }
        }

        private bool IsPowerUser(Thing thing)
        {
            CompSteamTrader compPowerTrader = thing.TryGetComp<CompSteamTrader>();
            return compPowerTrader != null && (compPowerTrader.SteamPowerOutput < 0f || (!compPowerTrader.SteamOn && compPowerTrader.Props.baseSteamConsumption > 0f));
        }

        private bool IsPowerGenerator(Thing thing)
        {
            if (thing.TryGetComp<CompSteamPowerPlant>() != null)
            {
                return true;
            }
            CompSteamTrader compPowerTrader = thing.TryGetComp<CompSteamTrader>();
            return compPowerTrader != null && (compPowerTrader.SteamPowerOutput > 0f || (!compPowerTrader.SteamOn && compPowerTrader.Props.baseSteamConsumption < 0f));
        }

        private bool HasAnyPowerGenerator(SteamPowerNet net)
        {
            List<CompSteamTrader> powerComps = net.steamComps;
            for (int i = 0; i < powerComps.Count; i++)
            {
                if (IsPowerGenerator(powerComps[i].parent))
                {
                    return true;
                }
            }
            return false;
        }

        private bool HasAnyPowerUser(SteamPowerNet net)
        {
            List<CompSteamTrader> powerComps = net.steamComps;
            for (int i = 0; i < powerComps.Count; i++)
            {
                if (IsPowerUser(powerComps[i].parent))
                {
                    return true;
                }
            }
            return false;
        }

        private bool TryFindClosestReachableNet(IntVec3 root, Predicate<SteamPowerNet> predicate, Map map, out SteamPowerNet foundNet, out IntVec3 closestTransmitter)
        {
            tmpPowerNetPredicateResults.Clear();
            SteamPowerNet foundNetLocal = null;
            IntVec3 closestTransmitterLocal = IntVec3.Invalid;
            map.floodFiller.FloodFill(root, (IntVec3 x) => EverPossibleToTransmitPowerAt(x, map), delegate (IntVec3 x)
            {
                Building transmitter = x.GetTransmitter(map);
                SteamPowerNet powerNet = transmitter?.GetComp<CompSteam>().SteamNet;
                if (powerNet == null)
                {
                    return false;
                }
                bool flag;
                if (!tmpPowerNetPredicateResults.TryGetValue(powerNet, out flag))
                {
                    flag = predicate(powerNet);
                    tmpPowerNetPredicateResults.Add(powerNet, flag);
                }
                if (flag)
                {
                    foundNetLocal = powerNet;
                    closestTransmitterLocal = x;
                    return true;
                }
                return false;
            }, true);
            tmpPowerNetPredicateResults.Clear();
            if (foundNetLocal != null)
            {
                foundNet = foundNetLocal;
                closestTransmitter = closestTransmitterLocal;
                return true;
            }
            foundNet = null;
            closestTransmitter = IntVec3.Invalid;
            return false;
        }

        private void SpawnTransmitters(List<IntVec3> cells, Map map, Faction faction)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].GetTransmitter(map) == null)
                {
                    Thing thing = GenSpawn.Spawn(ThingDefOf.PowerConduit, cells[i], map);
                    thing.SetFaction(faction, null);
                }
            }
        }

        private void SpawnTransmitters(IntVec3 start, IntVec3 end, Map map, Faction faction)
        {
            bool foundPath = false;
            map.floodFiller.FloodFill(start, (IntVec3 x) => EverPossibleToTransmitPowerAt(x, map), delegate (IntVec3 x)
            {
                if (x == end)
                {
                    foundPath = true;
                    return true;
                }
                return false;
            }, true);
            if (foundPath)
            {
                map.floodFiller.ReconstructLastFloodFillPath(end, tmpTransmitterCells);
                SpawnTransmitters(tmpTransmitterCells, map, faction);
            }
        }

        private bool TrySpawnPowerTransmittingBuildingNear(IntVec3 position, Map map, Faction faction, ThingDef def, out Building newBuilding)
        {
            TraverseParms traverseParams = TraverseParms.For(TraverseMode.PassAllDestroyableThings, Danger.Deadly, false);
            IntVec3 loc;
            if (RCellFinder.TryFindRandomCellNearWith(position, delegate (IntVec3 x)
            {
                if (!x.Standable(map) || x.Roofed(map) || !EverPossibleToTransmitPowerAt(x, map))
                {
                    return false;
                }
                if (!map.reachability.CanReach(position, x, PathEndMode.OnCell, traverseParams))
                {
                    return false;
                }
                CellRect.CellRectIterator iterator = GenAdj.OccupiedRect(x, Rot4.North, def.size).GetIterator();
                while (!iterator.Done())
                {
                    IntVec3 current = iterator.Current;
                    if (!current.InBounds(map) || current.Roofed(map) || current.GetEdifice(map) != null || current.GetFirstItem(map) != null || current.GetTransmitter(map) != null)
                    {
                        return false;
                    }
                    iterator.MoveNext();
                }
                return true;
            }, map, out loc, 8))
            {
                newBuilding = (Building)GenSpawn.Spawn(ThingMaker.MakeThing(def, null), loc, map, Rot4.North, false);
                newBuilding.SetFaction(faction, null);
                return true;
            }
            newBuilding = null;
            return false;
        }

        private bool TrySpawnPowerGeneratorNear(IntVec3 position, Map map, Faction faction, out Building newPowerGenerator)
        {
            if (TrySpawnPowerTransmittingBuildingNear(position, map, faction, ThingDefOf.SolarGenerator, out newPowerGenerator))
            {
                map.powerNetManager.UpdatePowerNetsAndConnections_First();
                newPowerGenerator.GetComp<CompPowerPlant>().UpdateDesiredPowerOutput();
                return true;
            }
            return false;
        }

        private bool TrySpawnBatteryNear(IntVec3 position, Map map, Faction faction, out Building newBattery)
        {
            if (TrySpawnPowerTransmittingBuildingNear(position, map, faction, ThingDefOf.Battery, out newBattery))
            {
                float randomInRange = newBatteriesInitialStoredEnergyPctRange.RandomInRange;
                newBattery.GetComp<CompPowerBattery>().SetStoredEnergyPct(randomInRange);
                return true;
            }
            return false;
        }

        private bool TrySpawnPowerGeneratorAndBatteryIfCanAndConnect(Thing forThing, Map map)
        {
            if (!canSpawnPowerGenerators)
            {
                return false;
            }
            IntVec3 position = forThing.Position;
            if (canSpawnBatteries)
            {
                float chance = (!(forThing is Building_Turret)) ? 0.1f : 1f;
                Building building;
                if (Rand.Chance(chance) && TrySpawnBatteryNear(forThing.Position, map, forThing.Faction, out building))
                {
                SpawnTransmitters(forThing.Position, building.Position, map, forThing.Faction);
                    position = building.Position;
                }
            }
            Building building2;
            if (TrySpawnPowerGeneratorNear(position, map, forThing.Faction, out building2))
            {
            SpawnTransmitters(position, building2.Position, map, forThing.Faction);
                return true;
            }
            return false;
        }

        private bool EverPossibleToTransmitPowerAt(IntVec3 c, Map map)
        {
            return c.GetTransmitter(map) != null || GenConstruct.CanBuildOnTerrain(ThingDefOf.PowerConduit, c, map, Rot4.North, null);
        }

        private void TryTurnOnImmediately(CompSteamTrader powerComp, Map map)
        {
            if (powerComp.SteamOn)
            {
                return;
            }
            map.powerNetManager.UpdatePowerNetsAndConnections_First();
            if (powerComp.SteamNet != null && powerComp.SteamNet.CurrentEnergyGainRate() > 1E-07f)
            {
                powerComp.SteamOn = true;
            }
        }
    }
}