﻿using System.Collections.Generic;
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
                            for (int i = 0; i < thingList.Count; i++)
                            {
                                if(thingList[i] is SteamBuilding sbuilding)
                                {
//                                    Log.Message("Found steambuilding " + sbuilding);
                                    if (!openSet.Contains(sbuilding) && !currentSet.Contains(sbuilding)
                                        && !closedSet.Contains(sbuilding))
                                    {
                                        openSet.Add(sbuilding);
                                        break;
                                    }
                                }
                                else if (thingList[i] is Building building)
                                {
                                    if (building.TransmitsPowerNow)
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
            //Log.Message("Starting new power net from " + root + " at " + root.InteractionCell);
            return new SteamPowerNet(ContiguousSteamBuildings(root));
        }

        public static void UpdateVisualLinkagesFor(SteamPowerNet net)
        {
        }
    }
}