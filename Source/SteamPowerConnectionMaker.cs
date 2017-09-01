using System.Collections.Generic;
using Verse;

namespace SteamCorp
{
    public class SteamPowerConnectionMaker
    {
        private const int ConnectMaxDist = 0;

        public static void ConnectAllConnectorsToTransmitter(CompSteam newTransmitter)
        {
            //Log.Message("ConnectAllConnectorsToTransmitter");
            foreach (CompSteam current in PotentialConnectorsForTransmitter(newTransmitter))
            {
                if (current.connectParent == null)
                {
                    current.ConnectToTransmitter(newTransmitter, false);
                }
            }
        }
        
		private static IEnumerable<CompSteam> PotentialConnectorsForTransmitter(CompSteam b)
        {
            //Log.Message("PotentialConnectorsForTransmitter");
            if (!b.parent.Spawned)
            {
                Log.Warning("Can't check potential connectors for " + b + " because it's unspawned.");
            }
            else
            {
                CellRect rect = b.parent.OccupiedRect().ExpandedBy(ConnectMaxDist).ClipInsideMap(b.parent.Map);
                for (int z = rect.minZ; z <= rect.maxZ; z++)
                {
                    for (int x = rect.minX; x <= rect.maxX; x++)
                    {
                        IntVec3 c = new IntVec3(x, 0, z);
                        List<Thing> thingList = b.parent.Map.thingGrid.ThingsListAt(c);
                        foreach (Thing thing in thingList)
                        {
                            if (thing.def.CompDefFor<CompSteam>() != null)
                            {
                                yield return ((Building)thing).GetComp<CompSteam>();
                            }
                        }
                    }
                }
            }
        }

        public static void DisconnectAllFromTransmitterAndSetWantConnect(CompSteam deadPc, Map map, SteamNetManager steamNetManager)
        {
            //Log.Message("DisconnectAllFromTransmitterAndSetWantConnect");
            if (deadPc.connectChildren == null)
            {
                return;
            }
            for (int i = 0; i < deadPc.connectChildren.Count; i++)
            {
                CompSteam compSteam = deadPc.connectChildren[i];
                compSteam.connectParent = null;
                CompSteamTrader compSteamTrader = compSteam as CompSteamTrader;
                if (compSteamTrader != null)
                {
                    compSteamTrader.SteamOn = false;
                }
                steamNetManager.Notify_ConnectorWantsConnect(compSteam);
            }
        }

        public static void TryConnectToAnySteamNet(CompSteam pc, List<SteamPowerNet> disallowedNets = null)
        {
            //Log.Message("TryConnectToAnySteamNet");
            if (pc.connectParent != null || !pc.parent.Spawned)
            {
                return;
            }
            CompSteam compSteam = BestTransmitterForConnector(pc.parent.Position, pc.parent.Map, disallowedNets);
            if (compSteam != null)
            {
                pc.ConnectToTransmitter(compSteam, false);
            }
            else
            {
                pc.connectParent = null;
            }
        }

        public static void DisconnectFromSteamNet(CompSteam pc)
        {
            //Log.Message("DisconnectFromSteamNet");
            if (pc.connectParent == null)
            {
                return;
            }
            if (pc.SteamNet != null)
            {
                pc.SteamNet.DeregisterConnector(pc);
            }
            if (pc.connectParent.connectChildren != null)
            {
                pc.connectParent.connectChildren.Remove(pc);
                if (pc.connectParent.connectChildren.Count == 0)
                {
                    pc.connectParent.connectChildren = null;
                }
            }
            pc.connectParent = null;
        }

        public static CompSteam BestTransmitterForConnector(IntVec3 connectorPos, Map map, List<SteamPowerNet> disallowedNets = null)
        {
            //Log.Message("BestTransmitterForConnector");
            CellRect cellRect = CellRect.SingleCell(connectorPos).ExpandedBy(ConnectMaxDist).ClipInsideMap(map);
            cellRect.ClipInsideMap(map);
            float num = 999999f;
            CompSteam result = null;
            for (int i = cellRect.minZ; i <= cellRect.maxZ; i++)
            {
                for (int j = cellRect.minX; j <= cellRect.maxX; j++)
                {
                    IntVec3 c = new IntVec3(j, 0, i);
                    Building transmitter = c.GetTransmitter(map);
                    if (transmitter != null && !transmitter.Destroyed)
                    {
                        CompSteam steamComp = transmitter.GetComp<CompSteam>();
                        if (steamComp != null && steamComp.TransmitsSteamPower 
                            && (transmitter.GetComp<CompSteam>() != null 
                                || transmitter.GetComp<CompSteam>().Props.allowPipeConnection))
                        {
                            if (disallowedNets == null || !disallowedNets.Contains(steamComp.SteamNet))
                            {
                                float num2 = (float)(transmitter.Position - connectorPos).LengthHorizontalSquared;
                                if (num2 < num)
                                {
                                    num = num2;
                                    result = steamComp;
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }
    }
}
