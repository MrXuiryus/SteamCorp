using Harmony;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace SteamCorp
{
    [StaticConstructorOnStartup]
    public static class PatchConstructor
    {
        static PatchConstructor()
        {
            var harmony = HarmonyInstance.Create("SteamCorp");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(Map), "FinalizeInit")]
    class MapInitPatch
    {
        [HarmonyPostfix]
        public static void FinalizeInitPatch(Map __instance)
        {
            StaticManager.Net.UpdatePowerNetsAndConnections_First();
        }
    }

    [HarmonyPatch(typeof(PowerNetManager), "TryCreateNetAt")]
    class PowerNetManagerPatch
    {
        [HarmonyPostfix]
        public static void Postfix(PowerNetManager __instance, ref IntVec3 cell)
        {
            if (__instance.map.powerNetGrid.TransmittedPowerNetAt(cell) == null 
                && cell.GetFirstBuilding(__instance.map).TryGetComp<CompSteamAlternator>() != null)
            {
                Building_Steam alternator = null;
                foreach(Thing thing in cell.GetThingList(__instance.map))
                {
                    if(thing.TryGetComp<CompSteamAlternator>() != null)
                    {
                        alternator = (Building_Steam)thing;
                        break;
                    }
                }
                PowerNet powerNet = new PowerNet(new List<CompPower> { alternator.PowerComp });
                __instance.RegisterPowerNet(powerNet);
                PowerConnectionMaker.ConnectAllConnectorsToTransmitter(powerNet.transmitters[0]);
            }
        }
    }

    [HarmonyPatch(typeof(PowerNet), new Type[] {typeof(IEnumerable<CompPower>)})]
    static class PowerNetPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref IEnumerable<CompPower> newTransmitters, PowerNet __instance)
        {
            //doing a foreach to access the first and only comp in a Alternator network
            foreach (CompPower comp in newTransmitters)
            {
                if(comp.parent.GetComp<CompSteamAlternator>() != null)
                {
                    __instance.hasPowerSource = true;
                    foreach(CompPower p in __instance.powerComps)
                    {
                        p.transNet = __instance;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(PowerConnectionMaker), "BestTransmitterForConnector")]
    class PowerConnectionMakerPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref CompPower __result, ref IntVec3 connectorPos, ref Map map, ref List<PowerNet> disallowedNets)
        {
            CellRect cellRect = CellRect.SingleCell(connectorPos).ExpandedBy(6).ClipInsideMap(map);
            float num = 999999f;
            cellRect.ClipInsideMap(map);
            __result = null;
            for (int i = cellRect.minZ; i <= cellRect.maxZ; i++)
            {
                for (int j = cellRect.minX; j <= cellRect.maxX; j++)
                {
                    IntVec3 c = new IntVec3(j, 0, i);
                    Building transmitter = c.GetTransmitter(map);
                    if (transmitter != null && !transmitter.Destroyed)
                    {
                        CompPower powerComp = transmitter.PowerComp;
                        if (powerComp != null && powerComp.TransmitsPowerNow && (transmitter.def.building == null || transmitter.def.building.allowWireConnection))
                        {
                            if (disallowedNets == null || !disallowedNets.Contains(powerComp.transNet))
                            {
                                float num2 = (float)(transmitter.Position - connectorPos).LengthHorizontalSquared;
                                if (num2 < num)
                                {
                                    num = num2;
                                    __result = powerComp;
                                }
                            }
                        }
                    }
                    else {
                        foreach(Thing thing in c.GetThingList(map))
                        {
                            if (thing.GetType() == typeof(Building_Steam))
                            {
                                Building building = (Building)thing;
                                CompPower powerComp = building.PowerComp;
                                if (powerComp != null &&
                                    (disallowedNets == null || !disallowedNets.Contains(powerComp.transNet)))
                                {
                                    float num2 = (building.Position - connectorPos).LengthHorizontalSquared;
                                    if (num2 < num)
                                    {
                                        num = num2;
                                        __result = powerComp;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(Map), "ConstructComponents")]
    class ConstructorPatch
    {
        [HarmonyPostfix]
        public static void ConstructComponentsPatch(Map __instance)
        {
            // set manager to new manager if null
            StaticManager.Net = new SteamNetManager(__instance, new SteamNetGrid(__instance));
            StaticManager.Breakdowns = new SteamBreakdownManager(__instance);
        }
    }

    [HarmonyPatch(typeof(BuildDesignatorUtility), "TryDrawPowerGridAndAnticipatedConnection")]
    class BuildDesignatorUtilityPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref BuildableDef def)
        {
            ThingDef thingDef = def as ThingDef;
            if(thingDef != null && thingDef.GetCompProperties<CompProperties_SteamAlternator>() != null){
                IntVec3 intVec = UI.MouseCell();
                CompPower compPower = PowerConnectionMaker.BestTransmitterForConnector(intVec, Find.VisibleMap, null);
                if (compPower != null)
                {
                    PowerNetGraphics.RenderAnticipatedWirePieceConnecting(intVec, compPower.parent);
                }
            }
        }
    }


    [HarmonyPatch(typeof(PowerNet), "PowerNetTick")]
    class PowerNetTickPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PowerNet __instance)
        {
            if (__instance.transmitters.Any(connector => connector.parent.GetComp<CompSteamAlternator>() != null))
            {
                float num = __instance.CurrentEnergyGainRate();
                float num2 = __instance.CurrentStoredEnergy();
                if (num2 + num >= -1E-07f)
                {
                    float num3;
                    if (__instance.batteryComps.Count > 0 && num2 >= 0.1f)
                    {
                        num3 = num2 - 5f;
                    }
                    else
                    {
                        num3 = num2;
                    }
                    if (num3 + num >= 0f)
                    {
                        List<CompPowerTrader> partsWantingPowerOn = new List<CompPowerTrader>();
                        for (int i = 0; i < __instance.powerComps.Count; i++)
                        {
                            if (!__instance.powerComps[i].PowerOn && FlickUtility.WantsToBeOn(__instance.powerComps[i].parent) && !__instance.powerComps[i].parent.IsBrokenDown())
                            {
                                partsWantingPowerOn.Add(__instance.powerComps[i]);
                            }
                        }
                        if (partsWantingPowerOn.Count > 0)
                        {
                            int num4 = 200 / partsWantingPowerOn.Count;
                            if (num4 < 30) 
                            {
                                num4 = 30;
                            }
                            if (Find.TickManager.TicksGame % num4 == 0)
                            {
                                CompPowerTrader compPowerTrader = partsWantingPowerOn.RandomElement();
                                if (num + num2 >= -(compPowerTrader.EnergyOutputPerTick + 1E-07f))
                                {
                                    compPowerTrader.PowerOn = true;
                                    num += compPowerTrader.EnergyOutputPerTick;
                                }
                            }
                        }
                    }
                    Traverse.Create(__instance).Method("ChangeStoredEnergy", num).GetValue();
                }
                else if (Find.TickManager.TicksGame % 20 == 0)
                {
                    List<CompPowerTrader> potentialShutdownParts = new List<CompPowerTrader>();
                    for (int j = 0; j < __instance.powerComps.Count; j++)
                    {
                        if (__instance.powerComps[j].PowerOn && __instance.powerComps[j].EnergyOutputPerTick < 0f)
                        {
                            potentialShutdownParts.Add(__instance.powerComps[j]);
                        }
                    }
                    if (potentialShutdownParts.Count > 0)
                    {
                        potentialShutdownParts.RandomElement().PowerOn = false;
                    }
                }
                return false;
            }
            return true;
        }
    }


    [HarmonyPatch(typeof(Map), "MapPostTick")]
    class MapTickPatch
    {
        [HarmonyPostfix]
        public static void MapPostTickPatch(Map __instance)
        {
            StaticManager.Net.PowerNetsTick();
        }
    }

    [HarmonyPatch(typeof(Map), "MapUpdate")]
    class MapUpdaterPatch
    {
        [HarmonyPostfix]
        public static void MapUpdatePatch(Map __instance)
        {
            StaticManager.Net.UpdatePowerNetsAndConnections_First();
            if (!WorldRendererUtility.WorldRenderedNow && Find.VisibleMap == __instance)
            {
                StaticManager.Net.Grid.DrawDebugPowerNetGrid();
            }
        }
    }

    [HarmonyPatch(typeof(Graphic_LinkedTransmitterOverlay), "ShouldLinkWith")]
    class Graphic_LinkedTransmitterOverlayPatch
    {
        [HarmonyPostfix]
        public static void ShouldLinkWithPatch(ref bool __result, ref IntVec3 c, ref Thing parent)
        {
            bool parentIsSteamBuilding = parent.TryGetComp<CompSteam>() != null;
            bool parentIsAlternator = parent.TryGetComp<CompSteamAlternator>() != null;
            bool cHasSteamBuilding = parent.Map.thingGrid.ThingsListAt(c).Exists(t => t.TryGetComp<CompSteam>() != null);
            bool cHasAlternator = parent.Map.thingGrid.ThingsListAt(c).Exists(t => t.TryGetComp<CompSteamAlternator>() != null);
            bool powerNetExistsAtC = parent.Map.powerNetGrid.TransmittedPowerNetAt(c) != null;
            bool powerNetExistsAtParent = parent.Map.powerNetGrid.TransmittedPowerNetAt(parent.Position) != null;

            //check if both points are in bounds
            if (c.InBounds(parent.Map) && parent.Position.InBounds(parent.Map))
            {
                // if both items are electric grid return true
                if (powerNetExistsAtC && powerNetExistsAtParent)
                {
                    //but only if it's not an alternator
                    if (cHasAlternator || parentIsAlternator)
                    {
                        __result = false;
                    }
                    else
                    {
                        __result = true;
                    }
                    return;
                }
                else if (parentIsSteamBuilding && cHasSteamBuilding)
                {
                    __result = true;
                    return;
                }
            }
            __result = false;
        }
    }

    [HarmonyPatch(typeof(Graphic_LinkedTransmitter), "ShouldLinkWith")]
    class Graphic_LinkedTransmitterPatch
    { 
        [HarmonyPostfix]
        public static void ShouldLinkWithPatch(ref bool __result, ref IntVec3 c, ref Thing parent)
        {
            bool parentIsSteamBuilding = parent.TryGetComp<CompSteam>() != null;
            bool parentIsAlternator = parent.TryGetComp<CompSteamAlternator>() != null;
            bool cHasSteamBuilding = parent.Map.thingGrid.ThingsListAt(c).Exists(t => t.TryGetComp<CompSteam>() != null);
            bool cHasAlternator = parent.Map.thingGrid.ThingsListAt(c).Exists(t => t.TryGetComp<CompSteamAlternator>() != null);
            bool powerNetExistsAtC = parent.Map.powerNetGrid.TransmittedPowerNetAt(c) != null;
            bool powerNetExistsAtParent = parent.Map.powerNetGrid.TransmittedPowerNetAt(parent.Position) != null;

            //check if both points are in bounds
            if (c.InBounds(parent.Map) && parent.Position.InBounds(parent.Map))
            {
                // if both items are electric grid return true
                if (powerNetExistsAtC && powerNetExistsAtParent)
                {
                    //but only if it's not an alternator
                    if (cHasAlternator || parentIsAlternator)
                    {
                        __result = false;
                    }
                    else
                    {
                        __result = true;
                    }
                    return;
                }
                else if (parentIsSteamBuilding && cHasSteamBuilding)
                {
                    __result = true;
                    return;
                }
            }
            __result = false;
        }
    }

    [HarmonyPatch(typeof(GridsUtility), "GetTransmitter")]
    class GridsUtilityPatch
    {
        [HarmonyPostfix]
        public static void GetTransmitterPatch(ref Building __result, ref IntVec3 c, ref Map map)
        {
            if (__result == null)
            {
                List<Thing> list = map.thingGrid.ThingsListAt(c);
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].TryGetComp<CompSteam>() != null && list[i].TryGetComp<CompSteamAlternator>() == null)
                    {
                        __result = (Building)list[i];
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(WorkGiver_FixBrokenDownBuilding), "PotentialWorkThingsGlobal")]
    class WorkGiver_FixBrokenDownBuildingPatch
    {
        [HarmonyPostfix]
        public static void PotentialWorkThingsGlobalPatch(WorkGiver_FixBrokenDownBuilding __instance,
            ref IEnumerable<Thing> __result, ref Pawn pawn)
        {
            foreach (Thing t in StaticManager.Breakdowns.BrokenDownThings)
            {
                __result.Add<Thing>(t);
            }
        }
    }
}
