using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SteamCorp
{
    public class SteamNetManager
    {
        private enum DelayedActionType
        {
            RegisterTransmitter,
            DeregisterTransmitter,
            RegisterConnector,
            DeregisterConnector
        }

        private struct DelayedAction
        {
            public DelayedActionType type;

            public CompSteam steamComp;

            public DelayedAction(DelayedActionType type, CompSteam steamPower)
            {
                this.type = type;
                this.steamComp = steamPower;
            }
        }

        private Map map;

        private SteamNetGrid grid;

        private List<SteamPowerNet> allNets = new List<SteamPowerNet>();

        private List<DelayedAction> delayedActions = new List<DelayedAction>();

        public List<SteamPowerNet> AllNetsListForReading
        {
            get
            {
                return allNets;
            }
        }

        public Map Map { get => map; set => map = value; }
        public SteamNetGrid Grid { get => grid; set => grid = value; }

        public SteamNetManager(Map map, SteamNetGrid grid)
        {
            Map = map;
            Grid = grid;
        }

        public void Notify_TransmitterSpawned(CompSteam newTransmitter)
        {
            //Log.Message("Notify_TransmitterSpawned \n" + grid.ToString() + "\n" + allNets.Count + "\n" + delayedActions.Count);
            delayedActions.Add(new DelayedAction(DelayedActionType.RegisterTransmitter, newTransmitter));
            NotifyDrawersForWireUpdate(newTransmitter.parent.Position);
        }

        public void Notify_TransmitterDespawned(CompSteam oldTransmitter)
        {
            //Log.Message("Notify_TransmitterDespawned");
            delayedActions.Add(new DelayedAction(DelayedActionType.DeregisterTransmitter, oldTransmitter));
            NotifyDrawersForWireUpdate(oldTransmitter.parent.Position);
        }

        public void Notfiy_TransmitterTransmitsPowerNowChanged(CompSteam transmitter)
        {
            if (!transmitter.parent.Spawned)
            {
                return;
            }
            delayedActions.Add(new DelayedAction(DelayedActionType.DeregisterTransmitter, transmitter));
            delayedActions.Add(new DelayedAction(DelayedActionType.RegisterTransmitter, transmitter));
            NotifyDrawersForWireUpdate(transmitter.parent.Position);
        }

        public void Notify_ConnectorWantsConnect(CompSteam wantingCon)
        {
            //Log.Message("Notify_ConnectorWantsConnect");
            if (Scribe.mode == LoadSaveMode.Inactive && !HasRegisterConnectorDuplicate(wantingCon))
            {
                delayedActions.Add(new DelayedAction(DelayedActionType.RegisterConnector, wantingCon));
            }
            NotifyDrawersForWireUpdate(wantingCon.parent.Position);
        } 

        public void Notify_ConnectorDespawned(CompSteam oldCon)
        {
            //Log.Message("Notify_ConnectorDespawned");
            delayedActions.Add(new DelayedAction(DelayedActionType.DeregisterConnector, oldCon));
            NotifyDrawersForWireUpdate(oldCon.parent.Position);
        }

        public void NotifyDrawersForWireUpdate(IntVec3 root)
        {
            //Log.Message("NotifyDrawersForWireUpdate");
            Map.mapDrawer.MapMeshDirty(root, MapMeshFlag.Things, true, false);
            Map.mapDrawer.MapMeshDirty(root, MapMeshFlag.PowerGrid, true, false);
        }

        public void RegisterPowerNet(SteamPowerNet newNet)
        {
            //Log.Message("RegisterPowerNet");
            allNets.Add(newNet);
            newNet.SteamNetManager = this;
            Grid.Notify_PowerNetCreated(newNet);
            SteamNetMaker.UpdateVisualLinkagesFor(newNet);
        }

        public void DeletePowerNet(SteamPowerNet oldNet)
        {
            //Log.Message("DeletePowerNet");
            allNets.Remove(oldNet);
            Grid.Notify_PowerNetDeleted(oldNet);
        }

        public void PowerNetsTick()
        {
            for (int i = 0; i < allNets.Count; i++)
            {
                allNets[i].SteamNetTick();
            }
        }

        public void UpdatePowerNetsAndConnections_First()
        {
            int count = delayedActions.Count;
            for (int i = 0; i < count; i++)
            {
                DelayedAction delayedAction = delayedActions[i];
                DelayedActionType type = delayedActions[i].type;
                if (type != DelayedActionType.RegisterTransmitter)
                {
                    if (type == DelayedActionType.DeregisterTransmitter)
                    {
                        TryDestroyNetAt(delayedAction.steamComp.parent.Position);
                        SteamPowerConnectionMaker.DisconnectAllFromTransmitterAndSetWantConnect(delayedAction.steamComp, Map, this);
                        delayedAction.steamComp.ResetPowerVars();
                    }
                }
                else
                {
                    ThingWithComps parent = delayedAction.steamComp.parent;
                    if (Grid.TransmittedPowerNetAt(parent.Position) != null)
                    {
                        Log.Warning(string.Concat(new object[]
                        {
                        "Tried to register trasmitter ",
                        parent,
                        " at ",
                        parent.Position,
                        ", but there is already a power net here. There can't be two transmitters on the same cell."
                        }));
                    }
                    delayedAction.steamComp.SetUpSteamPowerVars();
                    foreach (IntVec3 current in GenAdj.CellsAdjacentCardinal(parent))
                    {
                        TryDestroyNetAt(current);
                    }
                }
            }
            for (int j = 0; j < count; j++)
            {
                DelayedAction delayedAction2 = delayedActions[j];
                if (delayedAction2.type == DelayedActionType.RegisterTransmitter || delayedAction2.type == DelayedActionType.DeregisterTransmitter)
                {
                    TryCreateNetAt(delayedAction2.steamComp.parent.Position);
                    foreach (IntVec3 current2 in GenAdj.CellsAdjacentCardinal(delayedAction2.steamComp.parent))
                    {
                        TryCreateNetAt(current2);
                    }
                }
            }
            for (int k = 0; k < count; k++)
            {
                DelayedAction delayedAction3 = delayedActions[k];
                DelayedActionType type = delayedActions[k].type;
                if (type != DelayedActionType.RegisterConnector)
                {
                    if (type == DelayedActionType.DeregisterConnector)
                    {
                        SteamPowerConnectionMaker.DisconnectFromSteamNet(delayedAction3.steamComp);
                        delayedAction3.steamComp.ResetPowerVars();
                    }
                }
                else
                {
                    delayedAction3.steamComp.SetUpSteamPowerVars();
                    SteamPowerConnectionMaker.TryConnectToAnySteamNet(delayedAction3.steamComp, null);
                }
            }
            delayedActions.RemoveRange(0, count);
            if (DebugViewSettings.drawPower)
            {
                DrawDebugPowerNets();
            }
        }

        private bool HasRegisterConnectorDuplicate(CompSteam steamPower)
        {
            for (int i = delayedActions.Count - 1; i >= 0; i--)
            {
                if (delayedActions[i].steamComp == steamPower)
                {
                    if (delayedActions[i].type == DelayedActionType.DeregisterConnector)
                    {
                        return false;
                    }
                    if (delayedActions[i].type == DelayedActionType.RegisterConnector)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void TryCreateNetAt(IntVec3 cell)
        {
            if (!cell.InBounds(Map))
            {
                return;
            }
            if (Grid.TransmittedPowerNetAt(cell) == null)
            {
                Building transmitter = cell.GetTransmitter(Map);
                if (transmitter != null && transmitter is Building_Steam)
                {
                    SteamPowerNet powerNet = SteamNetMaker.NewPowerNetStartingFrom(transmitter);
                    RegisterPowerNet(powerNet);
                    for (int i = 0; i < powerNet.transmitters.Count; i++)
                    {
                        SteamPowerConnectionMaker.ConnectAllConnectorsToTransmitter(powerNet.transmitters[i]);
                    }
                }
            }
        }

        private void TryDestroyNetAt(IntVec3 cell)
        {
            if (cell.InBounds(Map))
            {
                SteamPowerNet powerNet = Grid.TransmittedPowerNetAt(cell);
                if (powerNet != null)
                {
                    DeletePowerNet(powerNet);
                }
            }
        }

        private void DrawDebugPowerNets()
        {
            if (Current.ProgramState != ProgramState.Playing)
            {
                return;
            }
            int num = 0;
            foreach (SteamPowerNet current in allNets)
            {
                foreach (CompSteam current2 in current.transmitters.Concat(current.connectors))
                {
                    foreach (IntVec3 current3 in GenAdj.CellsOccupiedBy(current2.parent))
                    {
                        CellRenderer.RenderCell(current3, (float)num * 0.44f);
                    }
                }
                num++;
            }
        }
    }
}
