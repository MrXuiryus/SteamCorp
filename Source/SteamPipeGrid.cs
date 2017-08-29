using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace SteamCorp
{
    public class SteamPipeGrid : MapComponent
    {
        public List<CompSteam> cachedPipeUsers;
        public List<CompSteam> cachedPipes;

        private SteamPowerNet[] PipeGrid;

        private bool isDirty;
        public bool IsDirty { get => isDirty; set => isDirty = value; }

        private SteamPowerNet currentNetwork;
        public SteamPowerNet CurrentNetwork { get => currentNetwork; set => currentNetwork = value; }

        public SteamPipeGrid(Map map) : base(map)
        {
            PipeGrid = new SteamPowerNet[map.AllCells.Count()];
            IsDirty = true;
        }

        public int IndexOfCellAt (IntVec3 pos)
        {
            Log.Message("Getting index of cell " + pos);
            return ((CellIndices)((Map)this.map).cellIndices).CellToIndex(pos);
        }

        public bool IsMatch(IntVec3 pos, SteamPowerNet ID)
        {
            Log.Message("Trying to match " + PipeGrid[IndexOfCellAt(pos)] + " with " + ID);
            return PipeGrid[IndexOfCellAt(pos)] == ID;
        }

        public void SetIDAt(IntVec3 pos, SteamPowerNet ID)
        {
            Log.Message("Setting ID of " + pos + " to " + ID);
            PipeGrid[IndexOfCellAt(pos)] = ID;
        }

        public SteamPowerNet GetIDAt(IntVec3 pos)
        {
            Log.Message("Getting ID of " + pos + ": " + PipeGrid[IndexOfCellAt(pos)]);
            return PipeGrid[IndexOfCellAt(pos)];
        }

        public bool ZoneAt(IntVec3 pos)
        {
            Log.Message("Checking if " + pos + "has a zone already: " + (PipeGrid[IndexOfCellAt(pos)] != null));
            return PipeGrid[IndexOfCellAt(pos)] != null;
        }

        public void RegisterPipe(CompSteam pipe)
        {
            Log.Message("Trying to register pipe " + pipe);
            if (!cachedPipes.Contains(pipe))
            {
                cachedPipes.Add(pipe);
                Log.Message("Registered pipe " + pipe);
            }
            IsDirty = true;
        }

        public void DeregisterPipe(CompSteam pipe)
        {
            Log.Message("Trying to remove pipe " + pipe);
            if (cachedPipes.Contains(pipe))
            {
                cachedPipes.Remove(pipe);
                Log.Message("Removed pipe " + pipe);
            }
            IsDirty = true;
        }

        public void RegisterPipeUser(CompSteam user)
        {
            Log.Message("Trying to register pipe user " + user);
            if (!cachedPipeUsers.Contains(user))
            {
                cachedPipeUsers.Add(user);
                Log.Message("Registered pipe user " + user);
            }
        }

        public void DeregisterPipeUser(CompSteam user)
        {
            Log.Message("Trying to remove pipe user " + user);
            if (cachedPipeUsers.Contains(user))
            {
                cachedPipeUsers.Remove(user);
                Log.Message("Removed pipe user " + user);
            }
        }

        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();
            if (IsDirty)
            {
                RebuildPipeGrid();
            }
        }

        public void RebuildPipeGrid()
        {
            Log.Message("Rebuilding PipeGrid");
            currentNetwork = null;

            // Erase all pipe grids
            for(int i = 0; i < PipeGrid.Length; i++)
            {
                PipeGrid[i] = null;
                cachedPipes[i].SteamNet = null;
            }
            
            foreach (CompSteam pipe in cachedPipes)
            {
                if (pipe != null && pipe.SteamNet == null)
                {
                    //Increment the master ID and assign a new grid
                    pipe.SteamNet = new SteamPowerNet(new List<CompSteam>());
                    
                    AddCurrentCellAndCheckAdjacent(pipe); 
                }
            }
            IsDirty = false;
        }

        private void AddCurrentCellAndCheckAdjacent(CompSteam pipe)
        {
            Log.Message("Setting pipe on grid " + PipeGrid[IndexOfCellAt(GenAdj.OccupiedRect(pipe.parent).CenterVector3.ToIntVec3())] 
                + " to " + pipe.SteamNet);
            //Set the current cell to be in the power grid
            PipeGrid[IndexOfCellAt(GenAdj.OccupiedRect(pipe.parent).CenterVector3.ToIntVec3())] = pipe.SteamNet;

            // Check the edge cells to add into the grid
            foreach (IntVec3 rect in GenAdj.OccupiedRect(pipe.parent).EdgeCells)
            {
                ScanAdjacentCells(rect, pipe.SteamNet);
            }
        }

        public void ScanAdjacentCells(IntVec3 pos, SteamPowerNet network)
        {
            Log.Message("Scanning cells next to " + pos + " and setting to " + network);
            // Get the adjacent tile in each of the Cardinal Directions
            foreach (IntVec3 index in GenAdj.CardinalDirections)
            {
                //Grab adjacent thing list and filter for a SteamBuilding, then grab Comp properties
                CompSteam comp = GridsUtility.GetThingList(pos + index, map)
                    .Where(thing => thing is SteamBuilding).First().TryGetComp<CompSteam>();
                
                //if item isn't assigned a grid assign one and expand it
                if (comp.SteamNet == null)
                {
                    comp.SteamNet = network;
                    AddCurrentCellAndCheckAdjacent(comp);
                }
            }
        }
    }
}
