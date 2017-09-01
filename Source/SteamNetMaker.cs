using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SteamCorp
{
    public static class SteamNetMaker
    {
        private static HashSet<Building> closedSet = new HashSet<Building>();

        private static HashSet<Building> openSet = new HashSet<Building>();

        private static HashSet<Building> currentSet = new HashSet<Building>();

        private static IEnumerable<CompSteam> ContiguousSteamBuildings(Building root)
        {
            closedSet.Clear();
            currentSet.Clear();
            openSet.Add(root);
            do
            {
                foreach (Building building in openSet)
                {
                    closedSet.Add(building);
                }
                HashSet<Building> hashSet = currentSet;
                currentSet = openSet;
                openSet = hashSet;
                openSet.Clear();
                foreach (Building currentBuilding in currentSet)
                {
                    foreach (IntVec3 adjacentCell in GenAdj.CellsAdjacentCardinal(currentBuilding))
                    {
                        if (adjacentCell.InBounds(currentBuilding.Map))
                        {
                            List<Thing> thingList = adjacentCell.GetThingList(currentBuilding.Map);
                            foreach (Thing thing in thingList)
                            {
                                if(thing is Building_Steam sbuilding)
                                {
                                    if (sbuilding.TransmitsSteamPower && FlickUtility.WantsToBeOn(sbuilding))
                                    {
                                        if (!openSet.Contains(sbuilding) && !currentSet.Contains(sbuilding)
                                            && !closedSet.Contains(sbuilding))
                                        {
                                            openSet.Add(sbuilding);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            while (openSet.Count > 0);
            return from b in closedSet
                    select b.GetComp<CompSteam>();
        }

        public static SteamPowerNet NewPowerNetStartingFrom(Building root)
        {
            return (root is Building_Steam && FlickUtility.WantsToBeOn(root)) 
                ? new SteamPowerNet(ContiguousSteamBuildings(root)) 
                : new SteamPowerNet(root.GetComps<CompSteam>());
        }

        public static void UpdateVisualLinkagesFor(SteamPowerNet net)
        { }
    }
}