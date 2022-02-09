using OD_Trade_Mission_Tracker.Utils;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;

namespace OD_Trade_Mission_Tracker.Settings
{
    public class AppSettingsValues : PropertyChangeNotify
    {
        private static readonly string defaultJournalPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Saved Games",
            "Frontier Developments",
            "Elite Dangerous");

        private DisplayMode viewDisplayMode;
        private GridSorting mainGridSorting;
        private bool showMarketHistory;
        private bool showClipBoard;
        private WindowPos lastWindowPos = new();
        private ObservableCollection<Commander> commanders = new();
        private string customJournalPath;

        public string CustomJournalPath { get => customJournalPath; set { customJournalPath = value; OnPropertyChanged(); } }
        public DisplayMode ViewDisplayMode { get => viewDisplayMode; set { viewDisplayMode = value; OnPropertyChanged(); } }
        public GridSorting MainGridSorting { get => mainGridSorting; set { mainGridSorting = value; OnPropertyChanged(); } }
        public bool ShowClipBoard { get => showClipBoard; set { showClipBoard = value; OnPropertyChanged(); } }
        public bool ShowMarketHistory { get => showMarketHistory; set { showMarketHistory = value; OnPropertyChanged(); } }
        public WindowPos LastWindowPos { get => lastWindowPos; set { lastWindowPos = value; OnPropertyChanged(); } }
        public ObservableCollection<Commander> Commanders { get => commanders; set { commanders = value; OnPropertyChanged(); } }

        [IgnoreDataMember]
        public string JournalPath 
        {
            get 
            {
                if (string.IsNullOrEmpty(CustomJournalPath))
                {
                    return defaultJournalPath;
                }
                return CustomJournalPath;
            }
        }
    }
}
