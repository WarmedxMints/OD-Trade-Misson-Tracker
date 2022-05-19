using OD_Trade_Mission_Tracker.CustomMessageBox;
using OD_Trade_Mission_Tracker.Missions;
using Ookii.Dialogs.Wpf;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace OD_Trade_Mission_Tracker.Settings
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : Window
    {
        public AppSettings ApplicationSettings { get; set; }
        public TradeMissionsContainer MissionsContainer { get; set; }
        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        // Close
        private void CommandBinding_Executed_Close(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        public SettingsView(AppSettings settings, TradeMissionsContainer missionsContainer)
        {
            ApplicationSettings = settings;
            MissionsContainer = missionsContainer;
            InitializeComponent();
        }

        private void PurgeCompleted_Click(object sender, RoutedEventArgs e)
        {
            var del = ODMessageBox.Show(this, "Purge All Completed Missions?", MessageBoxButton.YesNo);

            if (del == MessageBoxResult.Yes)
            {
                MissionsContainer.PurgeMissions(MissionState.Completed);
            }
        }

        private void PurgeAbandonded_Click(object sender, RoutedEventArgs e)
        {
            var del = ODMessageBox.Show(this, "Purge All Abandonded Missions?", MessageBoxButton.YesNo);

            if (del == MessageBoxResult.Yes)
            {
                MissionsContainer.PurgeMissions(MissionState.Abandonded);
            }
        }

        private void PurgeFailed_Click(object sender, RoutedEventArgs e)
        {
            var del = ODMessageBox.Show(this, "Purge All Failed Missions?", MessageBoxButton.YesNo);

            if (del == MessageBoxResult.Yes)
            {
                MissionsContainer.PurgeMissions(MissionState.Failed);
            }
        }

        private void ReadHistory_Click(object sender, RoutedEventArgs e)
        {
            MissionHistoryReaderView ret = new(MissionsContainer)
            {
                Owner = this
            };

            if ((bool)ret.ShowDialog())
            {
                MissionsContainer.SaveData();
            }
        }

        private void BrowseJournalFolder_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog folder = new()
            {
                Multiselect = false,
                Description = "Select ED Journal Folder",
                UseDescriptionForTitle = true
            };

            if (folder.ShowDialog().Value)
            {
                ApplicationSettings.Values.CustomJournalPath = folder.SelectedPath;

                MissionsContainer.RestartWatcher();
            }
        }

        private void ClearJournalFolder_Click(object sender, RoutedEventArgs e)
        {
            ApplicationSettings.Values.CustomJournalPath = null;
        }

        private void PayPalDonateButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo psi = new();
            psi.UseShellExecute = true;
            psi.FileName = "https://www.paypal.com/donate/?business=UPEJS3PN7H4XJ&no_recurring=0&item_name=Creator+of+OD+Software.+Thank+you+for+your+donation.&currency_code=GBP";
            _ = Process.Start(psi);
            e.Handled = true;
        }
    }
}
