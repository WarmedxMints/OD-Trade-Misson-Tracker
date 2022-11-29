using EliteJournalReader.Events;
using OD_Trade_Mission_Tracker.CustomMessageBox;
using OD_Trade_Mission_Tracker.Utils;
using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;

namespace OD_Trade_Mission_Tracker.Missions
{
    public class TradeMissionData : PropertyChangeNotify
    {
        private TradeMissionsContainer container;
        public void SetContainer(TradeMissionsContainer container) => this.container = container; 
        public TradeMissionData() { }
        public TradeMissionData(MissionAcceptedEvent.MissionAcceptedEventArgs e, Station station, TradeMissionsContainer container)
        {
            TimeStamp = e.Timestamp;
            Expiry = e.Expiry ?? DateTime.Now;
            MissionID = e.MissionID;
            LocalisedName = e.LocalisedName;
            Commodity = e.Commodity_Localised;
            Count = e.Count ?? 0;
            SourceFaction = e.Faction;
            DestinationFaction = e.TargetFaction;
            SourceSystem = station.StarSystem;
            SourceStation = station.StationName;
            SourceSystemId = station.SystemAddress;
            DestinationSystem = e.DestinationSystem;
            DestinationStation = e.DestinationStation;
            WingMission = e.Wing;
            OdysseyMission = container.CurrentGameMode == TradeMissionsContainer.GameVersion.Odyssey;
            Reward = e.Reward;
            this.container = container;

            switch (e.Name)
            {
                case string a when a.StartsWith("Mission_Mining", StringComparison.OrdinalIgnoreCase):
                    MissionType = MissionType.Mining;
                    break;
                case string b when b.StartsWith("Mission_Delivery", StringComparison.OrdinalIgnoreCase):
                    MissionType = MissionType.Delivery;
                    break;
                case string c when c.StartsWith("Mission_Collect", StringComparison.OrdinalIgnoreCase):
                    MissionType = MissionType.SourceAndReturn;
                    break;
            }
        }

        private int itemsCollected;
        private int itemsDelivered;
        private MissionState currentState;
        
        public DateTime TimeStamp { get; set; }
        public DateTime Expiry { get; set; }
        public long MissionID { get; set; }
        public string LocalisedName { get; set; }
        public string Commodity { get; set; }
        public int Count { get; set; }
        public string SourceFaction { get; set; }
        public string DestinationFaction { get; set; }
        public string SourceSystem { get; set; }
        public long SourceSystemId { get; set; }
        public string SourceStation { get; set; }
        public string DestinationSystem { get; set; }
        public string DestinationStation { get; set; }
        public bool WingMission { get; set; }
        public bool OdysseyMission { get; set; }
        public int Reward { get; set; }
        public int ItemsCollected { get => itemsCollected; set { itemsCollected = value; OnPropertyChanged(); OnPropertyChanged("ItemsToCollectRemaing"); } }
        public int ItemsToCollectRemaing => Count - itemsCollected;
        public int ItemsDelivered { get => itemsDelivered; set { itemsDelivered = value; OnPropertyChanged(); OnPropertyChanged("ItemsToDeliverRemaining"); } }
        public int ItemsToDeliverRemaining => Count - itemsDelivered;
        public MissionState CurrentState { get => currentState; set { currentState = value; OnPropertyChanged(); } }
        public MissionType MissionType { get; set; }

        [IgnoreDataMember]
        public ContextMenu ContextMenu
        {
            get
            {
                ContextMenu menu = new();

                MenuItem item = new()
                {
                    Header = $"Add {SourceSystem} - {SourceStation} To Mission Source Clipboard",
                };
                item.Click += (sender, e) => AddToMissionSource(SourceSystem, SourceStation);
                _ = menu.Items.Add(item);

                if(MissionType == MissionType.Delivery)
                {
                    item = new()
                    {
                        Header = $"Add {DestinationSystem} - {DestinationStation} To Mission Source Clipboard",
                    };
                    item.Click += (sender, e) => AddToMissionSource(DestinationSystem, DestinationStation);
                    _ = menu.Items.Add(item);
                }

                if (currentState != MissionState.Active)
                {
                    item = new()
                    {
                        Header = "Mark as Active"
                    };

                    item.Click += (sender, e) => MarkMission(MissionState.Active, true);
                    _ = menu.Items.Add(item);
                }

                if (currentState != MissionState.Completed)
                {
                    item = new()
                    {
                        Header = "Mark as Complete"
                    };
                    item.Click += (sender, e) => MarkMission(MissionState.Completed, false);
                    _ = menu.Items.Add(item);
                }

                if (currentState != MissionState.Abandonded)
                {
                    item = new()
                    {
                        Header = "Mark as Abandonded"
                    };
                    item.Click += (sender, e) => MarkMission(MissionState.Abandonded, false);
                    _ = menu.Items.Add(item);
                }

                if (currentState != MissionState.Failed)
                {
                    item = new()
                    {
                        Header = "Mark as Failed"
                    };
                    item.Click += (sender, e) => MarkMission(MissionState.Failed, false);
                    _ = menu.Items.Add(item);
                }

                item = new()
                {
                    Header = "Delete Mission"
                };

                item.Click += DeleteMission;
                _ = menu.Items.Add(item);

                return menu;
            }
        }

        private void MarkMission(MissionState state, bool moveToActive)
        {
            MessageBoxResult ret = ODMessageBox.Show(Application.Current.Windows.OfType<MainWindow>().First(),
                                         $"Mark Mission from {SourceSystem} at {SourceStation} for {Count} {Commodity} as {state}?",
                                         MessageBoxButton.YesNo);

            if (ret == MessageBoxResult.Yes)
            {
                CurrentState = state;
                container?.MoveMission(this, moveToActive);
                OnPropertyChanged("ContextMenu");
            };
        }

        private void DeleteMission(object sender, RoutedEventArgs e)
        {
            MessageBoxResult ret = ODMessageBox.Show(Application.Current.Windows.OfType<MainWindow>().First(),
                                                     $"Delete Mission from {SourceSystem} at {SourceStation} for {Count} {Commodity}?",
                                                     MessageBoxButton.YesNo);

            if (ret == MessageBoxResult.Yes)
            {
                container?.DeleteMission(this);
            }
        }

        private void AddToMissionSource(string sourceSystem, string sourceStation)
        {
            MessageBoxResult ret = ODMessageBox.Show(Application.Current.Windows.OfType<MainWindow>().First(),
                                         $"Add {sourceSystem} - {sourceStation} to Mission Source Clipboard?",
                                         MessageBoxButton.YesNo);

            if (ret == MessageBoxResult.Yes)
            {
                container?.AddToMissionSourceClipBoard(sourceSystem, sourceStation);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            return obj is TradeMissionData missionData && missionData.MissionID == MissionID;
        }

        public override int GetHashCode()
        {
            return MissionID.GetHashCode();
        }
    }
}
