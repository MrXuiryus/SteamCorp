using System.Collections.Generic;
using Verse;

namespace SteamCorp
{
    public class SteamNetGrid
    {
        private Map map;

        private SteamPowerNet[] netGrid;

        private Dictionary<SteamPowerNet, List<IntVec3>> steamNetCells = new Dictionary<SteamPowerNet, List<IntVec3>>();

        public SteamNetGrid(Map map)
        {
            this.map = map;
            netGrid = new SteamPowerNet[map.cellIndices.NumGridCells];
        }

        public SteamPowerNet TransmittedPowerNetAt(IntVec3 c)
        {
            return netGrid[map.cellIndices.CellToIndex(c)];
        }

        public void Notify_PowerNetCreated(SteamPowerNet newNet)
        {
            if (steamNetCells.ContainsKey(newNet))
            {
                Log.Warning("Net " + newNet + " is already registered in PowerNetGrid.");
                steamNetCells.Remove(newNet);
            }
            List<IntVec3> list = new List<IntVec3>();
            steamNetCells.Add(newNet, list);
            for (int i = 0; i < newNet.transmitters.Count; i++)
            {
                CellRect cellRect = newNet.transmitters[i].parent.OccupiedRect();
                for (int j = cellRect.minZ; j <= cellRect.maxZ; j++)
                {
                    for (int k = cellRect.minX; k <= cellRect.maxX; k++)
                    {
                        int num = map.cellIndices.CellToIndex(k, j);
                        if (netGrid[num] != null)
                        {
                            Log.Warning(string.Concat(new object[]
                            {
                            "Two power nets on the same cell (",
                            k,
                            ", ",
                            j,
                            "). First transmitters: ",
                            newNet.transmitters[0].parent.LabelCap,
                            " and ",
                            (!netGrid[num].transmitters.NullOrEmpty<CompSteam>()) ? netGrid[num].transmitters[0].parent.LabelCap : "[none]",
                            "."
                            }));
                        }
                        netGrid[num] = newNet;
                        list.Add(new IntVec3(k, 0, j));
                    }
                }
            }
        }

        public void Notify_PowerNetDeleted(SteamPowerNet deadNet)
        {
            // deadNet.Delete(); may solve Multiple nets on same cell Warning
            if (!steamNetCells.TryGetValue(deadNet, out List<IntVec3> list))
            {
                Log.Warning("Net " + deadNet + " does not exist in PowerNetGrid's dictionary.");
                return;
            }
            foreach (IntVec3 pos in list)
            {
                int num = map.cellIndices.CellToIndex(pos);
                if (netGrid[num] == deadNet)
                {
                    netGrid[num] = null;
                }
                else
                {
                    Log.Warning("Multiple nets on the same cell " + pos + ". This is probably a result of an earlier error.");
                }
            }
            steamNetCells.Remove(deadNet);
        }

        public void DrawDebugPowerNetGrid()
        {
            if (!DebugViewSettings.drawPowerNetGrid 
                || Current.ProgramState != ProgramState.Playing 
                || map != Find.VisibleMap)
            {
                return;
            }
            Rand.PushState();
            foreach (IntVec3 current in Find.CameraDriver.CurrentViewRect.ClipInsideMap(map))
            {
                SteamPowerNet powerNet = netGrid[map.cellIndices.CellToIndex(current)];
                if (powerNet != null)
                {
                    Rand.Seed = powerNet.GetHashCode();
                    CellRenderer.RenderCell(current, Rand.Value);
                }
            }
            Rand.PopState();
        }
    }
}