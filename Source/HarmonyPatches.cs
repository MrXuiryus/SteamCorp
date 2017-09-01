using Harmony;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace SteamCorp
{
    [StaticConstructorOnStartup]
    public static class SelectScenario_BeginScenarioConfiguration_Patch
    {
        static SelectScenario_BeginScenarioConfiguration_Patch()
        {
            var harmony = HarmonyInstance.Create("SteamCorp");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
#if DEBUG
            Log.Message("Completed constructing patches");
#endif
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
            bool parentIsSteamBuilding = parent is Building_Steam;
            bool netAtCIsNull = StaticManager.Net.Grid.TransmittedPowerNetAt(c) == null;
            //fix steam items trying to link to electricity items
            if (__result)
            {
                if ((netAtCIsNull && !parentIsSteamBuilding)
                    || (netAtCIsNull && parentIsSteamBuilding))
                {
                    __result = false;
                }
            }
            //fix steam items not finding other steam items
            else if (!__result && !netAtCIsNull && parentIsSteamBuilding && c.InBounds(parent.Map))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(Graphic_LinkedTransmitter), "ShouldLinkWith")]
    class Graphic_LinkedTransmitterPatch
    {
        [HarmonyPostfix]
        public static void ShouldLinkWithPatch(ref bool __result, ref IntVec3 c, ref Thing parent)
        {
            bool parentIsSteamBuilding = parent is Building_Steam;
            bool netAtCIsNull = StaticManager.Net.Grid.TransmittedPowerNetAt(c) == null;
            //fix steam items trying to link to electricity items
            if (__result)
            {
                if ((netAtCIsNull && !parentIsSteamBuilding)
                    || (netAtCIsNull && parentIsSteamBuilding))
                {
                    __result = false;
                }
            }
            //fix steam items not finding other steam items
            else if (!__result && !netAtCIsNull && parentIsSteamBuilding && c.InBounds(parent.Map))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(GraphicUtility), "WrapLinked")]
    class GraphicUtilityPatch
    {
        [HarmonyPrefix]
        public static bool WrapLinkedPatch(ref Graphic subGraphic, ref LinkDrawerType linkDrawerType)
        {
            return true;
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
                    if (list[i].TryGetComp<CompSteam>() != null)
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
