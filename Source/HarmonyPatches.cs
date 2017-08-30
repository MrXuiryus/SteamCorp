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
            Log.Message("Completed constructing patches");
        }
    }

    [HarmonyPatch(typeof(Map), "FinalizeInit")]
    class MapInitPatch
    {
        [HarmonyPostfix]
        public static void FinalizeInitPatch(Map __instance)
        {
            StaticSteamNetManager.Manager.UpdatePowerNetsAndConnections_First();
            Log.Message("Updated Power Nets");
        }
    }

    [HarmonyPatch(typeof(Map), "ConstructComponents")]
    class ConstructorPatch
    {
        [HarmonyPostfix]
        public static void ConstructComponentsPatch(Map __instance)
        {
            // set manager to new manager if null
            StaticSteamNetManager.Manager = new SteamNetManager(__instance, new SteamNetGrid(__instance));
            Log.Message("Manager now is " + StaticSteamNetManager.Manager);
        }
    }


    [HarmonyPatch(typeof(Map), "MapPostTick")]
    class MapTickPatch
    {
        [HarmonyPostfix]
        public static void MapPostTickPatch(Map __instance)
        {
            StaticSteamNetManager.Manager.PowerNetsTick();
        }
    }

    [HarmonyPatch(typeof(Map), "MapUpdate")]
    class MapUpdaterPatch
    {
        [HarmonyPostfix]
        public static void MapUpdatePatch(Map __instance)
        {
            StaticSteamNetManager.Manager.UpdatePowerNetsAndConnections_First();
            if (!WorldRendererUtility.WorldRenderedNow && Find.VisibleMap == __instance)
            {
                StaticSteamNetManager.Manager.Grid.DrawDebugPowerNetGrid();
            }
        }
    }

    /*[HarmonyPatch(typeof(BuildDesignatorUtility), "TryDrawPowerGridAndAnticipatedConnection")]
    class BuildDesignatorUtilityPatch
    {
        [HarmonyPostfix]
        public static void TryDrawPowerGridAndAnticipatedConnectionPatch(ref BuildableDef def)
        {
            Log.Message("nullcheck");
            if (def is ThingDef thingDef && thingDef.CompDefFor<CompSteam>() != null)
            {
                Log.Message("NOTNULL");
                OverlayDrawHandler.DrawPowerGridOverlayThisFrame();
                IntVec3 intVec = UI.MouseCell();
                CompSteam compPower = SteamPowerConnectionMaker.BestTransmitterForConnector(intVec, Find.VisibleMap, null);
                if (compPower != null)
                {
                    SteamNetGraphics.RenderAnticipatedWirePieceConnecting(intVec, compPower.parent);
                }
            }
        }
    }*/

    [HarmonyPatch(typeof(Graphic_LinkedTransmitterOverlay), "ShouldLinkWith")]
    class Graphic_LinkedTransmitterOverlayPatch
    {
        [HarmonyPostfix]
        public static void ShouldLinkWithPatch(ref bool __result, ref IntVec3 c, ref Thing parent)
        {
            bool parentIsSteamBuilding = parent is SteamBuilding;
            bool netAtCIsNull = StaticSteamNetManager.Manager.Grid.TransmittedPowerNetAt(c) == null;
            //fix steam items trying to link to electricity items
            if (__result && !netAtCIsNull && !parentIsSteamBuilding)
            {
                __result = false;
            }
            else if (__result && netAtCIsNull && parentIsSteamBuilding)
            {
                __result = false;
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
            if(__result == null)
            {
                List<Thing> list = map.thingGrid.ThingsListAt(c);
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].TryGetComp<CompSteam>() != null)
                    {
                        __result = (Building)list[i];
                        Log.Message("Found building " + __result);
                    }
                }
            }
        }
    }
}
