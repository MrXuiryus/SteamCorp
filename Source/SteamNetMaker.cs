﻿using RimWorld;
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
                                if (thing.TryGetComp<CompSteam>() != null)
                                {
                                    Building building = (Building)thing;
                                    if (FlickUtility.WantsToBeOn(building))
                                    {
                                        if (!openSet.Contains(building) && !currentSet.Contains(building)
                                            && !closedSet.Contains(building))
                                        {
                                            openSet.Add(building);
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
            // return new net from contiguous buildings if flicked on (or not flickable) and steam building
            return (root.TryGetComp<CompSteam>() != null && FlickUtility.WantsToBeOn(root)) 
                ? new SteamPowerNet(ContiguousSteamBuildings(root))
                // if the building is an off valve, return the net at root's position, or if null build a new net at root
                : new SteamPowerNet(root.GetComps<CompSteam>());
        }

        public static void UpdateVisualLinkagesFor(SteamPowerNet net)
        { }
    }
}