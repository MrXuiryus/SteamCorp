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

            public CompSteam steamPower;

            public DelayedAction(DelayedActionType type, CompSteam steamPower)
            {
                this.type = type;
                this.steamPower = steamPower;
            }
        }

        private Map map;

        private SteamNetGrid grid;

        private List<SteamPowerNet> allNets = new List<SteamPowerNet>();

        private List<SteamNetManager.DelayedAction> delayedActions = new List<SteamNetManager.DelayedAction>();

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
            delayedActions.Add(new SteamNetManager.DelayedAction(DelayedActionType.RegisterTransmitter, newTransmitter));
            NotifyDrawersForWireUpdate(newTransmitter.parent.Position);
        }

        public void Notify_TransmitterDespawned(CompSteam oldTransmitter)
        {
            delayedActions.Add(new SteamNetManager.DelayedAction(DelayedActionType.DeregisterTransmitter, oldTransmitter));
            NotifyDrawersForWireUpdate(oldTransmitter.parent.Position);
        }

        public void Notfiy_TransmitterTransmitsPowerNowChanged(CompSteam transmitter)
        {
            if (!transmitter.parent.Spawned)
            {
                return;
            }
            delayedActions.Add(new SteamNetManager.DelayedAction(DelayedActionType.DeregisterTransmitter, transmitter));
            delayedActions.Add(new SteamNetManager.DelayedAction(DelayedActionType.RegisterTransmitter, transmitter));
            NotifyDrawersForWireUpdate(transmitter.parent.Position);
        }

        public void Notify_ConnectorWantsConnect(CompSteam wantingCon)
        {
            if (Scribe.mode == LoadSaveMode.Inactive && !HasRegisterConnectorDuplicate(wantingCon))
            {
                delayedActions.Add(new SteamNetManager.DelayedAction(DelayedActionType.RegisterConnector, wantingCon));
            }
            NotifyDrawersForWireUpdate(wantingCon.parent.Position);
        }

        public void Notify_ConnectorDespawned(CompSteam oldCon)
        {
            delayedActions.Add(new SteamNetManager.DelayedAction(DelayedActionType.DeregisterConnector, oldCon));
            NotifyDrawersForWireUpdate(oldCon.parent.Position);
        }

        public void NotifyDrawersForWireUpdate(IntVec3 root)
        {
            Map.mapDrawer.MapMeshDirty(root, MapMeshFlag.Things, true, false);
            Map.mapDrawer.MapMeshDirty(root, MapMeshFlag.PowerGrid, true, false);
        }

        public void RegisterPowerNet(SteamPowerNet newNet)
        {
            allNets.Add(newNet);
            newNet.SteamNetManager = this;
            Grid.Notify_PowerNetCreated(newNet);
            SteamNetMaker.UpdateVisualLinkagesFor(newNet);
        }

        public void DeletePowerNet(SteamPowerNet oldNet)
        {
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
                        TryDestroyNetAt(delayedAction.steamPower.parent.Position);
                        SteamPowerConnectionMaker.DisconnectAllFromTransmitterAndSetWantConnect(delayedAction.steamPower, Map, this);
                        delayedAction.steamPower.ResetPowerVars();
                    }
                }
                else
                {
                    ThingWithComps parent = delayedAction.steamPower.parent;
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
                    delayedAction.steamPower.SetUpSteamPowerVars();
                    foreach (IntVec3 current in GenAdj.CellsAdjacentCardinal(parent))
                    {
                        TryDestroyNetAt(current);
                    }
                }
            }
            for (int j = 0; j < count; j++)
            {
                SteamNetManager.DelayedAction delayedAction2 = delayedActions[j];
                if (delayedAction2.type == DelayedActionType.RegisterTransmitter || delayedAction2.type == DelayedActionType.DeregisterTransmitter)
                {
                    TryCreateNetAt(delayedAction2.steamPower.parent.Position);
                    foreach (IntVec3 current2 in GenAdj.CellsAdjacentCardinal(delayedAction2.steamPower.parent))
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
                        SteamPowerConnectionMaker.DisconnectFromSteamNet(delayedAction3.steamPower);
                        delayedAction3.steamPower.ResetPowerVars();
                    }
                }
                else
                {
                    delayedAction3.steamPower.SetUpSteamPowerVars();
                    SteamPowerConnectionMaker.TryConnectToAnySteamNet(delayedAction3.steamPower, null);
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
                if (delayedActions[i].steamPower == steamPower)
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
                if (transmitter != null && transmitter.TransmitsPowerNow)
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
            if (!cell.InBounds(Map))
            {
                return;
            }
            SteamPowerNet powerNet = Grid.TransmittedPowerNetAt(cell);
            if (powerNet != null)
            {
                DeletePowerNet(powerNet);
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
