using OD_Trade_Mission_Tracker.Utils;

namespace OD_Trade_Mission_Tracker.Missions
{
    public class TradeStackCommodity : PropertyChangeNotify
    {
        private int amountToDeliver;
        private int amountDelivered;
        private ulong missionsValue;
        private long valuePerTonne;
        private int missionCount;
        public string CommodityName { get; set; }
        public int AmountToDeliver { get => amountToDeliver; set { amountToDeliver = value; OnPropertyChanged(); } }
        public int AmountDelivered { get => amountDelivered; set { amountDelivered = value; OnPropertyChanged(); OnPropertyChanged("AmountRemaining"); } }
        public int AmountRemaining => AmountToDeliver - AmountDelivered;
        public ulong MissionsValue { get => missionsValue; set { missionsValue = value; OnPropertyChanged(); } }
        public long ValuePerTonne { get => valuePerTonne; set { valuePerTonne = value; OnPropertyChanged(); } }
        public int MissionCount { get => missionCount; set { missionCount = value; OnPropertyChanged(); } }
    }
}
