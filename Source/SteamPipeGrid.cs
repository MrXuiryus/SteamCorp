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
        public List<Thing> cachedPipeUsers;
        public List<CompSteamPipe> cachedPipes;

        private int[] PipeGrid;

        private bool isDirty;
        public bool IsDirty { get => isDirty; set => isDirty = value; }

        private int gridCount;
        public int GridCount { get => gridCount; set => gridCount = value; }

        public SteamPipeGrid(Map map) : base(map)
        {
            PipeGrid = new int[map.AllCells.Count()];
            IsDirty = true;
        }

        public int IndexOfCellAt (IntVec3 pos)
        {
            Log.Message("Getting index of cell " + pos);
            return ((CellIndices)((Map)this.map).cellIndices).CellToIndex(pos);
        }

        public bool IsMatch(IntVec3 pos, int ID)
        {
            Log.Message("Trying to match " + PipeGrid[IndexOfCellAt(pos)] + " with " + ID);
            return PipeGrid[IndexOfCellAt(pos)] == ID;
        }

        public void SetIDAt(IntVec3 pos, int ID)
        {
            Log.Message("Setting ID of " + pos + " to " + ID);
            PipeGrid[IndexOfCellAt(pos)] = ID;
        }

        public int GetIDAt(IntVec3 pos)
        {
            Log.Message("Getting ID of " + pos + ": " + PipeGrid[IndexOfCellAt(pos)]);
            return PipeGrid[IndexOfCellAt(pos)];
        }

        public bool ZoneAt(IntVec3 pos)
        {
            Log.Message("Checking if " + pos + "has a zone already: " + (PipeGrid[IndexOfCellAt(pos)] >= 0));
            return PipeGrid[IndexOfCellAt(pos)] >= 0;
        }

        public void RegisterPipe(CompSteamPipe pipe)
        {
            Log.Message("Trying to register pipe " + pipe);
            if (!cachedPipes.Contains(pipe))
            {
                cachedPipes.Add(pipe);
                Log.Message("Registered pipe " + pipe);
            }
            IsDirty = true;
        }

        public void DeregisterPipe(CompSteamPipe pipe)
        {
            Log.Message("Trying to remove pipe " + pipe);
            if (cachedPipes.Contains(pipe))
            {
                cachedPipes.Remove(pipe);
                Log.Message("Removed pipe " + pipe);
            }
            IsDirty = true;
        }

        public void RegisterPipeUser(Thing user)
        {
            Log.Message("Trying to register pipe user " + user);
            if (!cachedPipeUsers.Contains(user))
            {
                cachedPipeUsers.Add(user);
                Log.Message("Registered pipe user " + user);
            }
        }

        public void DeregisterPipeUser(Thing user)
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
            GridCount = 0;

            // Erase all pipe grids
            for(int i = 0; i < PipeGrid.Length; i++)
            {
                PipeGrid[i] = -1;
                cachedPipes[i].GridID = -1;
            }
            
            foreach (CompSteamPipe pipe in cachedPipes)
            {
                if (pipe != null && pipe.GridID == -1)
                {
                    //Increment the master ID and assign a new grid
                    pipe.GridID = GridCount++;
                    
                    AddCurrentCellAndCheckAdjacent(pipe); 
                }
            }
            IsDirty = false;
        }

        private void AddCurrentCellAndCheckAdjacent(CompSteamPipe pipe)
        {
            Log.Message("Setting pipe on grid " + PipeGrid[IndexOfCellAt(GenAdj.OccupiedRect(pipe.parent).CenterVector3.ToIntVec3())] 
                + " to " + pipe.GridID);
            //Set the current cell to be in the power grid
            PipeGrid[IndexOfCellAt(GenAdj.OccupiedRect(pipe.parent).CenterVector3.ToIntVec3())] = pipe.GridID;

            // Check the edge cells to add into the grid
            foreach (IntVec3 rect in GenAdj.OccupiedRect(pipe.parent).EdgeCells)
            {
                ScanAdjacentCells(rect, pipe.GridID);
            }
        }

        public void ScanAdjacentCells(IntVec3 pos, int GridID)
        {
            Log.Message("Scanning cells next to " + pos + " and setting to " + GridID);
            // Get the adjacent tile in each of the Cardinal Directions
            foreach (IntVec3 index in GenAdj.CardinalDirections)
            {
                //Grab adjacent thing list and filter for a SteamBuilding, then grab Comp properties
                CompSteamPipe comp = GridsUtility.GetThingList(pos + index, map)
                    .Where(thing => thing is SteamBuilding).First().TryGetComp<CompSteamPipe>();
                
                //if item isn't assigned a grid assign one and expand it
                if (comp.GridID == -1)
                {
                    comp.GridID = GridID;
                    AddCurrentCellAndCheckAdjacent(comp);
                }
            }
        }
    }
}
