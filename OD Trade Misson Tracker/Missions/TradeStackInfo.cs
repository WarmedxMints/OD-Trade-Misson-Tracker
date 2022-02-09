using OD_Trade_Mission_Tracker.Utils;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OD_Trade_Mission_Tracker.Missions
{
    public class TradeStackInfo : PropertyChangeNotify
    {
        public string SystemName { get; set; }
        public string StationName { get; set; }
        public List<TradeMissionData> Missions { get; set; } = new();
        public ObservableCollection<TradeStackCommodity> Commodities { get; set; } = new();

        internal void UpdateCommodityValues(TradeMissionData missionData)
        {
            TradeStackCommodity commodity = Commodities.FirstOrDefault(x => x.CommodityName == missionData.Commodity);

            if (commodity is null || Missions is null || Missions.Count == 0 || missionData.CurrentState is MissionState.Abandonded or MissionState.Failed)
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

                if (tradeMissionData.Commodity != missionData.Commodity)
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
                Commodities.RemoveFromCollection(commodity);
                return;
            }

            commodity.AmountToDeliver = amountToDeliver;
            commodity.AmountDelivered = amountDelivered;
            commodity.MissionsValue = missionsValue;
            commodity.ValuePerTonne = Helpers.SafeDivision((long)missionsValue , (long)amountToDeliver);
            commodity.MissionCount = missionCount;
        }
    }
}
