using EliteJournalReader;
using OD_Trade_Mission_Tracker.Missions;
using OD_Trade_Mission_Tracker.Utils;
using System.Runtime.Serialization;
using System.Windows.Controls;

namespace OD_Trade_Mission_Tracker.Commodities
{
    public class CommodityData : PropertyChangeNotify
    {
        private double distance;

        public string CommodityName { get; set; }
        public string System { get; set; }
        public long SystemAddress { get; set; }
        public SystemPosition SystemPos { get; set; }
        public string Station { get; set; }
        public int Cost { get; set; }
        [IgnoreDataMember]
        public double Distance { get => distance; set { distance = value; OnPropertyChanged(); } }
        [IgnoreDataMember]
        public TradeMissionsContainer Container { get; set; }
        [IgnoreDataMember]
        public ContextMenu ContextMenu
        {
            get
            {
                ContextMenu menu = new();

                MenuItem item = new()
                {
                    Header = $"Copy {System} To Clipboard",
                };
                item.Click += (sender, e) => Helpers.SetClipboard(System);
                _ = menu.Items.Add(item);

                item = new()
                {
                    Header = $"Add {System} - {Station} To Source Clipboard",
                };
                item.Click += (sender, e) => Container.AddToMissionSourceClipBoard(System, Station);
                _ = menu.Items.Add(item);

                return menu;
            }
        }
    }
}
