using EliteJournalReader;
using EliteJournalReader.Events;
using OD_Trade_Mission_Tracker.Commodities;
using OD_Trade_Mission_Tracker.SystemInfo;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace OD_Trade_Mission_Tracker.Missions
{
    public class MissionHistoryBuilder
    {
        private readonly JournalWatcher watcher;
        private readonly string commanderFid;
        private TradeMissionsContainer container;

        private bool odyssey;
        private Dictionary<long, TradeMissionData> horizonMissionsData = new(), odysseyMissionsData = new();
        private bool handleLogs = true;
        //private ShipLoadout currentShip = new();
        private SystemData currentSystem = new();
        private Station currentStation = new();
        private List<CommodityData> commodities = new();

        public MissionHistoryBuilder(JournalWatcher watcher, string fid)
        {
            commanderFid = fid;

            this.watcher = watcher;
        }

        private void UpdateDistances()
        {
            if (currentSystem is null || commodities is null || commodities.Count == 0)
            {
                return;
            }

            foreach (CommodityData commodity in commodities)
            {
                commodity.Distance = SystemPosition.Distance(currentSystem.SystemPos, commodity.SystemPos);
            }
        }

        private void OnFileHeaderEvent(object sender, FileheaderEvent.FileheaderEventArgs e)
        {
            odyssey = e.Odyssey;
        }

        private void OnCommanderEvent(object sender, CommanderEvent.CommanderEventArgs e)
        {
            handleLogs = e.FID.Equals(commanderFid);
        }

        private void OnLocationEvent(object sender, LocationEvent.LocationEventArgs e)
        {
            if (handleLogs == false)
            {
                return;
            }

            currentSystem = new()
            {
                SystemName = e.StarSystem,
                SystemAddress = e.SystemAddress,
                SystemPos = e.StarPos
            };


            if (e.Docked == false)
            {
                return;
            }

            currentStation = new()
            {
                StarSystem = e.StarSystem,
                SystemAddress = e.SystemAddress,
                StationName = e.StationName
            };
        }

        private void OnDockedAtStation(object sender, DockedEvent.DockedEventArgs e)
        {
            if (handleLogs == false)
            {
                return;
            }

            currentStation = new()
            {
                StarSystem = e.StarSystem,
                SystemAddress = e.SystemAddress,
                StationName = e.StationName
            };
        }

        private void OnUndockedFromStation(object sender, UndockedEvent.UndockedEventArgs e)
        {
            if (handleLogs == false)
            {
                return;
            }

            currentStation = null;
        }

        private void OnMissionAccepted(object sender, MissionAcceptedEvent.MissionAcceptedEventArgs e)
        {

            if (handleLogs == false)
            {
                return;
            }

            if (ValidMission(e.Count, e.Name) == false)
            {
                return;
            }


            TradeMissionData missionData = new(e, currentStation, container);
            missionData.CurrentState = MissionState.Active;
            missionData.OdysseyMission = odyssey;
            AddMissionToDictionary(ref odyssey ? ref odysseyMissionsData : ref horizonMissionsData, missionData);
        }

        private readonly string[] validMissionNames = new string[] { "Mission_Collect", "Mission_Delivery", "Mission_Mining" };
        private bool ValidMission(int? count, string missionName)
        {
            if (count is null || count == 0)
            {
                return false;
            }

            for (int i = 0; i < validMissionNames.Length; i++)
            {
                if (missionName.StartsWith(validMissionNames[i]))
                {
                    return true;
                }
            }

            return false;
        }
        private void OnMissionRedirected(object sender, MissionRedirectedEvent.MissionRedirectedEventArgs e)
        {
            if (handleLogs == false)
            {
                return;
            }

            Dictionary<long, TradeMissionData> dict = odyssey ? odysseyMissionsData : horizonMissionsData;

            if (dict is null || !dict.ContainsKey(e.MissionID))
            {
                return;
            }

            TradeMissionData missionData = dict[e.MissionID];
            missionData.CurrentState = MissionState.Redirected;
        }

        private void OnMissionCompleted(object sender, MissionCompletedEvent.MissionCompletedEventArgs e)
        {
            if (handleLogs == false)
            {
                return;
            }

            Dictionary<long, TradeMissionData> dict = odyssey ? odysseyMissionsData : horizonMissionsData;

            if (dict is null || !dict.ContainsKey(e.MissionID))
            {
                return;
            }

            TradeMissionData missionData = dict[e.MissionID];
            missionData.CurrentState = MissionState.Completed;
            missionData.Reward = e.Reward;
            missionData.ItemsDelivered = missionData.Count;
            if (missionData.ItemsCollected < 0)
            {
                missionData.ItemsCollected = missionData.Count;
            }
        }

        private void OnMissionFailed(object sender, MissionFailedEvent.MissionFailedEventArgs e)
        {
            if (handleLogs == false)
            {
                return;
            }

            if (odyssey ? (odysseyMissionsData is null || !odysseyMissionsData.ContainsKey(e.MissionID)) : (horizonMissionsData is null || !horizonMissionsData.ContainsKey(e.MissionID)))
            {
                return;
            }

            TradeMissionData missionData = odyssey ? odysseyMissionsData[e.MissionID] : horizonMissionsData[e.MissionID];
            missionData.CurrentState = MissionState.Failed;
            missionData.Reward = 0;
        }

        private void OnMissionAbandonded(object sender, MissionAbandonedEvent.MissionAbandonedEventArgs e)
        {
            if (handleLogs == false)
            {
                return;
            }

            if (odyssey ? (odysseyMissionsData is null || !odysseyMissionsData.ContainsKey(e.MissionID)) : (horizonMissionsData is null || !horizonMissionsData.ContainsKey(e.MissionID)))
            {
                return;
            }

            TradeMissionData missionData = odyssey ? odysseyMissionsData[e.MissionID] : horizonMissionsData[e.MissionID];
            missionData.CurrentState = MissionState.Abandonded;
            missionData.Reward = 0;
        }

        private void OnCargoDepot(object sender, CargoDepotEvent.CargoDepotEventArgs e)
        {
            if (handleLogs == false)
            {
                return;
            }

            Dictionary<long, TradeMissionData> dict = odyssey ? odysseyMissionsData : horizonMissionsData;

            if (dict.ContainsKey(e.MissionID) == false)
            {
                return;
            }

            dict[e.MissionID].ItemsDelivered = e.ItemsDelivered;
            dict[e.MissionID].ItemsCollected = e.ItemsCollected;
        }

        private void OnMarketBuy(object sender, MarketBuyEvent.MarketBuyEventArgs e)
        {
            if (handleLogs == false)
            {
                return;
            }

            TextInfo textInfo = new CultureInfo("en-GB", false).TextInfo;

            CommodityData commodity = new()
            {
                CommodityName = textInfo.ToTitleCase(e.Type.ToLowerInvariant()),
                System = currentSystem.SystemName,
                SystemAddress = currentSystem.SystemAddress,
                SystemPos = currentSystem.SystemPos.Copy(),
                Station = currentStation.StationName,
                Cost = e.BuyPrice
            };

            commodities.Add(commodity);
        }

        //private void OnCargo(object sender, CargoEvent.CargoEventArgs e)
        //{
        //    if (handleLogs == false)
        //    {
        //        return;
        //    }


        //}

        //private void OnLoadoutEvent(object sender, LoadoutEvent.LoadoutEventArgs e)
        //{
        //    if (handleLogs == false)
        //    {
        //        return;
        //    }

        //    currentShip?.UpdateFromLoadoutEvent(e);
        //}

        private void OnFSDJump(object sender, FSDJumpEvent.FSDJumpEventArgs e)
        {
            if (handleLogs == false)
            {
                return;
            }

            currentSystem = new()
            {
                SystemName = e.StarSystem,
                SystemAddress = e.SystemAddress,
                SystemPos = e.StarPos
            };


        }

        private void OnCarrierJump(object sender, CarrierJumpEvent.CarrierJumpEventArgs e)
        {
            if (handleLogs == false)
            {
                return;
            }

            currentSystem = new()
            {
                SystemName = e.StarSystem,
                SystemAddress = e.SystemAddress,
                SystemPos = e.StarPos
            };
        }
        private static void AddMissionToDictionary(ref Dictionary<long, TradeMissionData> missionDictionary, TradeMissionData missionData)
        {
            if (missionDictionary == null)
            {
                missionDictionary = new();
            }

            if (missionDictionary.ContainsKey(missionData.MissionID))
            {
                return;
            }

            missionDictionary.Add(missionData.MissionID, missionData);
        }

        public Tuple<Dictionary<long, TradeMissionData>, Dictionary<long, TradeMissionData>, List<CommodityData>> GetHistory(IProgress<string> progress, TradeMissionsContainer container)
        {
            this.container = container;

            SubscribeToEvents();

            watcher.ParseHistory(progress);

            UnsubscribeFromEvents();

            UpdateDistances();

            commodities.Reverse();

            if (commodities.Count > 20)
            {
                commodities.RemoveRange(20, commodities.Count - 20);
            }

            return Tuple.Create(horizonMissionsData, odysseyMissionsData, commodities);
        }

        private void SubscribeToEvents()
        {
            watcher.GetEvent<FileheaderEvent>()?.AddHandler(OnFileHeaderEvent);

            watcher.GetEvent<CommanderEvent>()?.AddHandler(OnCommanderEvent);

            watcher.GetEvent<LocationEvent>()?.AddHandler(OnLocationEvent);

            watcher.GetEvent<DockedEvent>()?.AddHandler(OnDockedAtStation);

            watcher.GetEvent<UndockedEvent>()?.AddHandler(OnUndockedFromStation);

            watcher.GetEvent<MissionAcceptedEvent>()?.AddHandler(OnMissionAccepted);

            watcher.GetEvent<MissionRedirectedEvent>()?.AddHandler(OnMissionRedirected);

            watcher.GetEvent<MissionCompletedEvent>()?.AddHandler(OnMissionCompleted);

            watcher.GetEvent<MissionFailedEvent>()?.AddHandler(OnMissionFailed);

            watcher.GetEvent<MissionAbandonedEvent>()?.AddHandler(OnMissionAbandonded);

            watcher.GetEvent<MarketBuyEvent>()?.AddHandler(OnMarketBuy);

            watcher.GetEvent<CargoDepotEvent>()?.AddHandler(OnCargoDepot);

            //watcher.GetEvent<CargoEvent>()?.AddHandler(OnCargo);

            //watcher.GetEvent<LoadoutEvent>()?.AddHandler(OnLoadoutEvent);

            watcher.GetEvent<CarrierJumpEvent>()?.AddHandler(OnCarrierJump);

            watcher.GetEvent<FSDJumpEvent>()?.AddHandler(OnFSDJump);
        }

        private void UnsubscribeFromEvents()
        {
            watcher.GetEvent<FileheaderEvent>()?.RemoveHandler(OnFileHeaderEvent);

            watcher.GetEvent<CommanderEvent>()?.RemoveHandler(OnCommanderEvent);

            watcher.GetEvent<LocationEvent>()?.RemoveHandler(OnLocationEvent);

            watcher.GetEvent<DockedEvent>()?.RemoveHandler(OnDockedAtStation);

            watcher.GetEvent<UndockedEvent>()?.RemoveHandler(OnUndockedFromStation);

            watcher.GetEvent<MissionAcceptedEvent>()?.RemoveHandler(OnMissionAccepted);

            watcher.GetEvent<MissionRedirectedEvent>()?.RemoveHandler(OnMissionRedirected);

            watcher.GetEvent<MissionCompletedEvent>()?.RemoveHandler(OnMissionCompleted);

            watcher.GetEvent<MissionAbandonedEvent>()?.RemoveHandler(OnMissionAbandonded);

            watcher.GetEvent<MissionFailedEvent>()?.RemoveHandler(OnMissionFailed);

            watcher.GetEvent<MarketBuyEvent>()?.RemoveHandler(OnMarketBuy);

            watcher.GetEvent<CargoDepotEvent>()?.RemoveHandler(OnCargoDepot);

            //watcher.GetEvent<CargoEvent>()?.RemoveHandler(OnCargo);

            //watcher.GetEvent<LoadoutEvent>()?.RemoveHandler(OnLoadoutEvent);

            watcher.GetEvent<CarrierJumpEvent>()?.RemoveHandler(OnCarrierJump);

            watcher.GetEvent<FSDJumpEvent>()?.RemoveHandler(OnFSDJump);
        }
    }
}
