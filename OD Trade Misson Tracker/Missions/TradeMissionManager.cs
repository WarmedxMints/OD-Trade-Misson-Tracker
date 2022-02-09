using OD_Trade_Mission_Tracker.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OD_Trade_Mission_Tracker.Missions
{
    public class TradeMissionManager : PropertyChangeNotify
    {
        private bool hasDeliveries;
        private ObservableCollection<TradeMissionData> missions = new();
        private ObservableCollection<TradeStackInfo> stackInfo = new();
        private ObservableCollection<TradeStackCommodity> commoditiesStack = new();

        private long totalCommodities;
        private int totalMissionCount;
        private ulong totalMissionValue;
        private ulong totalShareValue;
        public bool HasDeliveries { get => hasDeliveries; set { hasDeliveries = value; OnPropertyChanged(); } }
        public ObservableCollection<TradeMissionData> Missions { get => missions; set { missions = value; OnPropertyChanged(); } }
        public ObservableCollection<TradeStackInfo> StackInfo { get => stackInfo; set { stackInfo = value; OnPropertyChanged(); } }
        public ObservableCollection<TradeStackCommodity> CommoditiesStack { get => commoditiesStack; set { commoditiesStack = value; OnPropertyChanged(); } }

        public ulong TotalShareValue { get => totalShareValue; set { totalShareValue = value; OnPropertyChanged(); } }
        public ulong TotalMissionValue { get => totalMissionValue; set { totalMissionValue = value; OnPropertyChanged(); } }
        public int TotalMissionCount { get => totalMissionCount; set { totalMissionCount = value; OnPropertyChanged(); } }
        public long TotalCommodities { get => totalCommodities; set { totalCommodities = value; OnPropertyChanged(); } }

        public void AddMission(TradeMissionData missionData)
        {
            //Add the Mission
            if (Missions.Contains(missionData) == false)
            {
                Missions.AddToCollection(missionData);
            }

            if (missionData.CurrentState is MissionState.Abandonded or MissionState.Failed)
            {
                return;
            }

            //Find to the Stack
            TradeStackInfo stack = StackInfo.FirstOrDefault(x => x.SystemName == missionData.SourceSystem && x.StationName == missionData.SourceStation);

            if (stack is null)
            {
                stack = new()
                {
                    StationName = missionData.SourceStation,
                    SystemName = missionData.SourceSystem
                };

                StackInfo.AddToCollection(stack);
                StackInfo.Sort((x, y) =>
                {
                    int ret = string.Compare(x.SystemName, y.SystemName, StringComparison.OrdinalIgnoreCase);
                    if (ret == 0)
                    {
                        ret = string.Compare(x.StationName, y.StationName, StringComparison.OrdinalIgnoreCase);
                    }
                    return ret;
                });
            }

            if (stack.Missions.Contains(missionData) == false)
            {
                stack.Missions.Add(missionData);
            }

            HasDeliveries = Missions.Any(x => x.MissionType == MissionType.Delivery);

            TradeStackCommodity commodity = stack.Commodities.FirstOrDefault(x => x.CommodityName == missionData.Commodity);

            if (commodity is null)
            {
                commodity = new()
                {
                    CommodityName = missionData.Commodity
                };

                stack.Commodities.AddToCollection(commodity);
                stack.Commodities.Sort((x, y) => string.Compare(x.CommodityName, y.CommodityName, StringComparison.OrdinalIgnoreCase));
            }

            stack.UpdateCommodityValues(missionData);

            TradeStackCommodity stackCommodity = CommoditiesStack.FirstOrDefault(x => x.CommodityName == missionData.Commodity);

            if (stackCommodity is null)
            {
                stackCommodity = new()
                {
                    CommodityName = missionData.Commodity
                };

                CommoditiesStack.AddToCollection(commodity);
                CommoditiesStack.Sort((x, y) => string.Compare(x.CommodityName, y.CommodityName, StringComparison.OrdinalIgnoreCase));
            }

            UpdateCommoditiesStack(missionData.Commodity);
            UpdateTotals();
        }

        public void AddMissions(List<TradeMissionData> missions)
        {
            if (Missions.Count > 0)
            {
                missions.AddRange(Missions);
            }

            BuildStacks(missions);
        }

        private void BuildStacks(List<TradeMissionData> missions)
        {

            List<TradeStackInfo> stackInfo = new();
            List<TradeStackCommodity> tradeStacks = new();

            List<string> stackCommodities = new();

            foreach (TradeMissionData missionData in missions)
            {
                if (missionData.CurrentState is MissionState.Abandonded or MissionState.Failed)
                {
                    continue;
                }

                TradeStackInfo stack = stackInfo.FirstOrDefault(x => x.SystemName == missionData.SourceSystem && x.StationName == missionData.SourceStation);

                if (stack is null)
                {
                    stack = new()
                    {
                        StationName = missionData.SourceStation,
                        SystemName = missionData.SourceSystem
                    };

                    stackInfo.Add(stack);
                }

                if (stack.Missions.Contains(missionData) == false)
                {
                    stack.Missions.Add(missionData);
                }

                TradeStackCommodity commodity = stack.Commodities.FirstOrDefault(x => x.CommodityName == missionData.Commodity);

                if (commodity is null)
                {
                    commodity = new()
                    {
                        CommodityName = missionData.Commodity
                    };

                    stack.Commodities.AddToCollection(commodity);
                }

                stack.UpdateCommodityValues(missionData);

                TradeStackCommodity stackCommodity = tradeStacks.FirstOrDefault(x => x.CommodityName == missionData.Commodity);

                if (stackCommodity is null)
                {
                    stackCommodity = new()
                    {
                        CommodityName = missionData.Commodity
                    };

                    tradeStacks.Add(stackCommodity);
                    stackCommodities.Add(missionData.Commodity);
                }
            }

            stackInfo.Sort((x, y) =>
            {
                int ret = string.Compare(x.SystemName, y.SystemName, StringComparison.OrdinalIgnoreCase);
                if (ret == 0)
                {
                    ret = string.Compare(x.StationName, y.StationName, StringComparison.OrdinalIgnoreCase);
                }
                return ret;
            });

            foreach (TradeStackInfo stack in stackInfo)
            {
                stack.Commodities.Sort((x, y) => string.Compare(x.CommodityName, y.CommodityName, StringComparison.OrdinalIgnoreCase));
            }

            HasDeliveries = missions.Any(x => x.MissionType == MissionType.Delivery);

            tradeStacks.Sort((x, y) => string.Compare(x.CommodityName, y.CommodityName, StringComparison.OrdinalIgnoreCase));

            foreach (string commodityName in stackCommodities)
            {
                TradeStackCommodity stackCommodity = tradeStacks.FirstOrDefault(x => x.CommodityName == commodityName);

                if (stackCommodity is null || missions is null || missions.Count == 0)
                {
                    return;
                }

                int amountToDeliver = 0;
                int amountDelivered = 0;
                ulong missionsValue = 0;
                int missionCount = 0;
                for (int i = 0; i < missions.Count; i++)
                {
                    TradeMissionData tradeMissionData = missions[i];

                    if (tradeMissionData.Commodity != commodityName || tradeMissionData.CurrentState is MissionState.Abandonded or MissionState.Failed)
                    {
                        continue;
                    }

                    amountToDeliver += tradeMissionData.Count;
                    amountDelivered += tradeMissionData.ItemsDelivered;
                    missionsValue += (ulong)tradeMissionData.Reward;
                    missionCount++;
                }

                if (missionCount == 0)
                {
                    continue;
                }

                stackCommodity.AmountToDeliver = amountToDeliver;
                stackCommodity.AmountDelivered = amountDelivered;
                stackCommodity.MissionsValue = missionsValue;
                stackCommodity.ValuePerTonne = Helpers.SafeDivision((long)missionsValue, (long)amountToDeliver);
                stackCommodity.MissionCount = missionCount;
            }
           
            Missions = new(missions);
            StackInfo = new(stackInfo);
            CommoditiesStack = new(tradeStacks);
            UpdateTotals();
        }

        public void UpdateMission(TradeMissionData missionData)
        {
            if (Missions.Contains(missionData) == false)
            {
                AddMission(missionData);
                return;
            }

            //Find the Stack
            TradeStackInfo stack = StackInfo.FirstOrDefault(x => x.SystemName == missionData.SourceSystem && x.StationName == missionData.SourceStation);

            if (stack is null)
            {
                AddMission(missionData);
                return;
            }

            TradeStackCommodity commodity = stack.Commodities.FirstOrDefault(x => x.CommodityName == missionData.Commodity);

            if (commodity is null)
            {
                AddMission(missionData);
                return;
            }

            stack.UpdateCommodityValues(missionData);
            UpdateCommoditiesStack(missionData.Commodity);
            HasDeliveries = Missions.Any(x => x.MissionType == MissionType.Delivery);
            UpdateTotals();
        }

        public void RemoveMission(TradeMissionData missionData)
        {
            if (Missions.Contains(missionData) == false)
            {
                return;
            }

            Missions.RemoveFromCollection(missionData);
            HasDeliveries = Missions.Any(x => x.MissionType == MissionType.Delivery);
            UpdateCommoditiesStack(missionData.Commodity);
            UpdateTotals();
            TradeStackInfo stack = StackInfo.FirstOrDefault(x => x.SystemName == missionData.SourceSystem && x.StationName == missionData.SourceStation);

            if (stack is null)
            {
                return;
            }

            if (stack.Missions.Contains(missionData))
            {
                stack.Missions.Remove(missionData);
                //if we have no mission left at that station, remove the collection
                if (stack.Missions.Count <= 0)
                {
                    StackInfo.RemoveFromCollection(stack);
                    return;
                }
            }

            TradeStackCommodity commodity = stack.Commodities.FirstOrDefault(x => x.CommodityName == missionData.Commodity);

            if (commodity is null)
            {
                return;
            }

            //Get number of missions for that commoodity
            int count = stack.Missions.Count(x => x.Commodity == missionData.Commodity);

            //If we still have missions for the commodity, update the values

            if (count > 0)
            {
                stack.UpdateCommodityValues(missionData);
                return;
            }

            stack.Commodities.RemoveFromCollection(commodity);
        }

        public void RemoveMissions(List<TradeMissionData> missionsToRemove)
        {
            List<TradeMissionData> missions = Missions.Except(missionsToRemove).ToList();

            BuildStacks(missions);

            UpdateTotals();
        }

        public void UpdateCommoditiesStack(string commodity)
        {
            TradeStackCommodity stackCommodity = CommoditiesStack.FirstOrDefault(x => x.CommodityName == commodity);

            if (stackCommodity is null || Missions is null || Missions.Count == 0)
            {
                return;
            }

            int amountToDeliver = 0;
            int amountDelivered = 0;
            ulong missionsValue = 0;
            int missionCount = 0;

            for (int i = 0; i < Missions.Count; i++)
            {
                TradeMissionData tradeMissionData = Missions[i];

                if (tradeMissionData.Commodity != commodity || tradeMissionData.CurrentState is MissionState.Abandonded or MissionState.Failed)
                {
                    continue;
                }

                amountToDeliver += tradeMissionData.Count;
                amountDelivered += tradeMissionData.ItemsDelivered;
                missionsValue += (ulong)tradeMissionData.Reward;

                missionCount++;
            }

            if (missionCount == 0)
            {
                CommoditiesStack.RemoveFromCollection(stackCommodity);
                return;
            }

            stackCommodity.AmountToDeliver = amountToDeliver;
            stackCommodity.AmountDelivered = amountDelivered;
            stackCommodity.MissionsValue = missionsValue;
            stackCommodity.ValuePerTonne = Helpers.SafeDivision((long)missionsValue, (long)amountToDeliver);
            stackCommodity.MissionCount = missionCount;
        }

        private void UpdateTotals()
        {
            if(Missions == null || Missions.Count == 0)
            {
                TotalCommodities = 0;
                TotalMissionCount = 0;
                TotalMissionValue = 0;
                TotalShareValue = 0;
                return;
            }

            long totalCommodities = 0;
            int totalMissionCount = 0;
            ulong totalMissionValue = 0;
            ulong totalShareValue = 0;

            for (int i = 0; i < Missions.Count; i++)
            {
                TradeMissionData tradeMissionData = Missions[i];

                if (tradeMissionData.CurrentState is MissionState.Abandonded or MissionState.Failed)
                {
                    continue;
                }

                totalCommodities += tradeMissionData.Count;
                totalMissionCount++;
                totalMissionValue += (ulong)tradeMissionData.Reward;
                
                if(tradeMissionData.WingMission)
                {
                    totalShareValue += (ulong)tradeMissionData.Reward;
                }
            }

            TotalCommodities = totalCommodities;
            TotalMissionCount = totalMissionCount;
            TotalMissionValue = totalMissionValue;
            TotalShareValue = totalShareValue;
        }

        public void ClearData()
        {
            Missions.ClearCollection();
            StackInfo.ClearCollection();
            HasDeliveries = false;
            TotalCommodities = 0;
            TotalMissionCount = 0;
            TotalMissionValue = 0;
            TotalShareValue = 0;
        }
    }
}