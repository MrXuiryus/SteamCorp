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
        private bool IsDirty;
        public int masterID;

        public SteamPipeGrid(Map map) : base(map)
        {
            PipeGrid = new int[map.AllCells.Count<IntVec3>()];
            IsDirty = true;
            masterID = 0;
        }

        public int IndexOfCellAt (IntVec3 pos)
        {
            return ((CellIndices)((Map)this.map).cellIndices).CellToIndex(pos);
        }

        public bool IsMatch(IntVec3 pos, int ID)
        {
            return PipeGrid[IndexOfCellAt(pos)] == ID;
        }

        public void SetIDAt(IntVec3 pos, int ID)
        {
            PipeGrid[IndexOfCellAt(pos)] = ID;
        }

        public int GetIDAt(IntVec3 pos, int ID)
        {
            return PipeGrid[IndexOfCellAt(pos)];
        }

        public bool ZoneAt(IntVec3 pos, int ID)
        {
            return PipeGrid[IndexOfCellAt(pos)] >= -1;
        }

        public void RegisterPipe(CompSteamPipe pipe)
        {
            if (!cachedPipes.Contains(pipe))
            {
                cachedPipes.Add(pipe);
            }
            IsDirty = true;
        }

        public void DeregisterPipe(CompSteamPipe pipe)
        {
            if (cachedPipes.Contains(pipe))
            {
                cachedPipes.Remove(pipe);
            }
            IsDirty = true;
        }

        public void RegisterPipeUser(Thing user)
        {
            if(!cachedPipeUsers.Contains(user))
            {
                cachedPipeUsers.Add(user);
            }
        }

        public void DeregisterPipeUser(Thing user)
        {
            if (cachedPipeUsers.Contains(user))
            {
                cachedPipeUsers.Remove(user);
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
            masterID = 0;

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
                    pipe.GridID = masterID++;
                    
                    AddCurrentCellAndCheckAdjacent(pipe); 
                }
            }
            IsDirty = false;
        }

        private void AddCurrentCellAndCheckAdjacent(CompSteamPipe pipe)
        {
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
            // Get the adjacent tile in each of the Cardinal Directions
            for (int index = 0; index < GenAdj.CardinalDirections.Length; index++)
            {
                //Grab adjacent building list and filter for PipeBuildings
                CompSteamPipe comp = 
                    GridsUtility.GetThingList(pos + (IntVec3)GenAdj.CardinalDirections[index], map)
                    .Where(thing => thing is SteamBuilding).First().TryGetComp<CompSteamPipe>();
                
                if (comp.GridID == -1)
                {
                    comp.GridID = GridID;
                    AddCurrentCellAndCheckAdjacent(comp);
                }
            }
        }
    }
}
