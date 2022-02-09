﻿using OD_Trade_Mission_Tracker.CustomMessageBox;
using OD_Trade_Mission_Tracker.Missions;
using OD_Trade_Mission_Tracker.Utils;
using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;

namespace OD_Trade_Mission_Tracker.SourceClipboard
{
    public class MissionSourceClipboardData : PropertyChangeNotify
    {
        private TradeMissionsContainer container;

        public void SetContainer(TradeMissionsContainer container) => this.container = container;

        private string systemName;
        public string SystemName { get => systemName; set { systemName = value; OnPropertyChanged(); } }

        private string stationName;
        public string StationName { get => stationName; set { stationName = value; OnPropertyChanged(); } }

        private ContextMenu contextMenu;
        [IgnoreDataMember]
        public ContextMenu ContextMenu
        {
            get
            {
                if (contextMenu is not null)
                {
                    return contextMenu;
                }

                contextMenu = new();

                MenuItem item = new()
                {
                    Header = $"Copy {SystemName} To Clipboard",
                    Tag = SystemName
                };
                item.Click += CopyToClipboard;
                _ = contextMenu.Items.Add(item);

                item = new()
                {
                    Header = $"Copy {StationName} To Clipboard",
                    Tag = StationName
                };
                item.Click += CopyToClipboard;
                _ = contextMenu.Items.Add(item);

                item = new()
                {
                    Header = $"Delete Entry",
                };
                item.Click += DeleteEntry;
                _ = contextMenu.Items.Add(item);

                return contextMenu;
            }
        }

        private void DeleteEntry(object sender, RoutedEventArgs e)
        {
            MessageBoxResult ret = ODMessageBox.Show(Application.Current.Windows.OfType<MainWindow>().First(), $"Delete Enrty {SystemName} - {SystemName} - {StationName}?", MessageBoxButton.YesNo);

            if (ret == MessageBoxResult.Yes)
            {
                container.RemoveClipboardEntry(this);
            }
        }

        private void CopyToClipboard(object sender, RoutedEventArgs e)
        {
            Helpers.SetClipboard(((MenuItem)sender).Tag);
        }

        public override bool Equals(object obj)
        {
            //Check for null and compare run-time types.
            if (obj == null || !GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                MissionSourceClipboardData p = (MissionSourceClipboardData)obj;
                return stationName == p.StationName && systemName == p.SystemName;
            }
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(stationName, systemName);
        }
    }
}

