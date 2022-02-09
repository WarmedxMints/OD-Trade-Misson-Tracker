using OD_Trade_Mission_Tracker.Commodities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace OD_Trade_Mission_Tracker.Missions
{
    /// <summary>
    /// Interaction logic for MissionHistoryReaderView.xaml
    /// </summary>
    public partial class MissionHistoryReaderView : Window
    {
        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !readingHistory;
        }

        // Close
        private void CommandBinding_Executed_Close(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private readonly TradeMissionsContainer container;
        private bool readingHistory;
        public MissionHistoryReaderView(TradeMissionsContainer container, bool autoRun = false)
        {
            this.container = container;
            InitializeComponent();
            if (autoRun)
            {
                ReadHistoryBtn_Click(null, null);
            }
        }

        private async void ReadHistoryBtn_Click(object sender, RoutedEventArgs e)
        {
            if (readingHistory)
            {
                return;
            }

            readingHistory = true;
            Header.Text = "Reading History, Please Wait....";
            ReadHistoryBtn.Content = "Reading History...";
            ReadHistoryBtn.IsEnabled = false;
            ReadHistoryBtn.Visibility = Visibility.Hidden;
            ProgressPanel.Visibility = Visibility.Visible;
            AutoClose.Visibility = Visibility.Visible;
            TitleText.Text = "Processing Journal File : ";

            Progress<string> progress = new();
            progress.ProgressChanged += (_, newText) => ProgressText.Text = newText; 

            try
            {
                MissionHistoryBuilder builder = new(container.JournalWatcher, container.CommanderFID);

                Tuple<Dictionary<long, TradeMissionData>, Dictionary<long, TradeMissionData>, List<CommodityData>> ret = await Task.Run(() => builder.GetHistory(progress, container));
                TitleText.Text = "Processing Journal File : ";
                TitleText.Text = "Processing : ";
                await Task.Run(() => container.ProcessHistory(ret.Item1, ret.Item2, ret.Item3, progress));
            }
            catch (OperationCanceledException)
            {
                // Handle the cancelled Task
            }

            DialogResult = true;
        }
    }
}
