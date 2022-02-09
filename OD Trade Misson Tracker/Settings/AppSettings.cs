using OD_Trade_Mission_Tracker.Utils;
using System;
using System.IO;
using System.Windows;

namespace OD_Trade_Mission_Tracker.Settings
{
    public class AppSettings : PropertyChangeNotify
    {
        private readonly string settingsSaveFile = Path.Combine(Directory.GetCurrentDirectory(), "Data", "AppSettings.json");

        private AppSettingsValues values;
        public AppSettingsValues Values { get => values; set { values = value; OnPropertyChanged(); } }

        public event EventHandler CommanderChanged;

        private Commander currentCommander;
        public Commander CurrentCommander
        {
            get => currentCommander;
            set
            {
                if (currentCommander is null || currentCommander.FID != value.FID)
                {
                    currentCommander = value;
                    CommanderChanged?.Invoke(this, EventArgs.Empty);
                    OnPropertyChanged();
                }
            }
        }

        public void SetWindowPos()
        {
            Values.LastWindowPos.Width = 1320;
            Values.LastWindowPos.Height = 850;

            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            double windowWidth = Values.LastWindowPos.Width;
            double windowHeight = Values.LastWindowPos.Height;
            Values.LastWindowPos.Left = (screenWidth / 2) - (windowWidth / 2);
            Values.LastWindowPos.Top = (screenHeight / 2) - (windowHeight / 2);

            if (Values.LastWindowPos.Height > SystemParameters.VirtualScreenHeight)
            {
                Values.LastWindowPos.Height = SystemParameters.VirtualScreenHeight;
            }

            if (Values.LastWindowPos.Width > SystemParameters.VirtualScreenWidth)
            {
                Values.LastWindowPos.Width = SystemParameters.VirtualScreenWidth;
            }
        }

        public void LoadSettings()
        {
            var values = LoadSaveJson.LoadJson<AppSettingsValues>(settingsSaveFile);

            if(values is not null)
            {
                Values = values;
                return;
            }
            Values = new();
            SetWindowPos();
        }

        public bool SaveSettings()
        {
            return LoadSaveJson.SaveJson(Values, settingsSaveFile);
        }
    }
}
