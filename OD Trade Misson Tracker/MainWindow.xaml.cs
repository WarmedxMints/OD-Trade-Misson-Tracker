using OD_Trade_Mission_Tracker.Missions;
using OD_Trade_Mission_Tracker.Settings;
using OD_Trade_Mission_Tracker.SourceClipboard;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace OD_Trade_Mission_Tracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Custom Title Bar
        // Can execute
        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        // Minimize
        private void CommandBinding_Executed_Minimize(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        // Maximize
        private void CommandBinding_Executed_Maximize(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(this);
        }

        // Restore
        private void CommandBinding_Executed_Restore(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(this);
        }

        // Close
        private void CommandBinding_Executed_Close(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        // State change
        private void MainWindowStateChangeRaised(object sender, EventArgs e)
        {
            if (Settings is not null)
            {
                Settings.Values.LastWindowPos.State = WindowState;
            }

            if (WindowState == WindowState.Maximized)
            {
                MainWindowBorder.BorderThickness = new Thickness(8);
                RestoreButton.Visibility = Visibility.Visible;
                MaximizeButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                MainWindowBorder.BorderThickness = new Thickness(0);
                RestoreButton.Visibility = Visibility.Collapsed;
                MaximizeButton.Visibility = Visibility.Visible;
            }
        }
        #endregion
        public CollectionViewSource ViewSource { get; set; } = new();
        public AppSettings Settings { get; set; } = new();
        public TradeMissionsContainer MissionsContainer { get; set; }
        public MainWindow()
        {
            MissionsContainer = new(Settings);
            Settings.LoadSettings();
            InitializeComponent();
        }

        #region Window Methods and Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {            
            WindowState = Settings.Values.LastWindowPos.State;
            ShowSiderBar(Settings.Values.ShowMarketHistory, "\xE973", "\xE974", MarketHistoryColumn, MarketHistoryExapander);
            ShowSiderBar(Settings.Values.ShowClipBoard, "\xE974", "\xE973", ClipboardColumn, ClipboardExpander);
        }
        private void Root_ContentRendered(object sender, EventArgs e)
        {
            MissionsContainer.Init();
            MissionsContainer.OnDataLoaded += OnMissionLoaded;
        }
        private void Root_Closing(object sender, CancelEventArgs e)
        {
            MissionsContainer.SaveData();
            _ = Settings.SaveSettings();
        }

        private void OnMissionLoaded(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ViewSource.Source = MissionsContainer.CurrentManager.Missions;

                SortMissionDataGrid();
            });
        }

        private void SimpleCommand_OnExecuted(object sender, object e)
        {
            Settings.SetWindowPos();
            WindowState = WindowState.Normal;
        }
        #endregion

        #region Combobox Methods
        private void DisplayModeSelecter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MissionsContainer?.SetCurrentManager();

            ViewSource.Source = MissionsContainer?.CurrentManager.Missions;

            SortMissionDataGrid();
        }

        private void SortingModeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SortMissionDataGrid();
        }
        #endregion

        #region DataGrid Methods
        private void MissionDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            DataGrid grid = (DataGrid)sender;
           
            SortMissionDataGrid();

            grid.ItemContainerGenerator.StatusChanged += (sender, e) => ItemContainerGenerator_StatusChanged(sender, grid);

            grid.Items.IsLiveSorting = true;
        }

        private void SortMissionDataGrid()
        {
            List<SortDescription> sortDescriptions = new();

            switch (Settings.Values.MainGridSorting)
            {
                case GridSorting.SourceSystem:
                    sortDescriptions.Add(new SortDescription("SourceSystem", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("SourceStation", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("Expiry", ListSortDirection.Ascending));
                    break;
                case GridSorting.SourceStation:
                    sortDescriptions.Add(new SortDescription("SourceStation", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("SourceSystem", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("Expiry", ListSortDirection.Ascending));
                    break;
                case GridSorting.DestinationSystem:
                    sortDescriptions.Add(new SortDescription("DestinationSystem", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("DestinationStation", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("Expiry", ListSortDirection.Ascending));
                    break;
                case GridSorting.DestinationStation:
                    sortDescriptions.Add(new SortDescription("DestinationStation", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("DestinationSystem", ListSortDirection.Ascending)); 
                    sortDescriptions.Add(new SortDescription("Expiry", ListSortDirection.Ascending));
                    break;
                case GridSorting.CollectionsRemaining:
                    sortDescriptions.Add(new SortDescription("ItemsToCollectRemaing", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("Expiry", ListSortDirection.Ascending));
                    break;
                case GridSorting.DeliveriesRemaining:
                    sortDescriptions.Add(new SortDescription("ItemsToDeliverRemaining", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("Expiry", ListSortDirection.Ascending));
                    break;
                case GridSorting.Reward:
                    sortDescriptions.Add(new SortDescription("Reward", ListSortDirection.Ascending));
                    break;
                case GridSorting.Expiry:
                    sortDescriptions.Add(new SortDescription("Expiry", ListSortDirection.Ascending));
                    break;
                case GridSorting.Wing:
                    sortDescriptions.Add(new SortDescription("WingMission", ListSortDirection.Ascending));
                    break;

                default:
                    sortDescriptions.Add(new SortDescription("SourceSystem", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("SourceStation", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("Expiry", ListSortDirection.Ascending));
                    break;
            }

            //SortDataGrid(MissionDataGrid, sortDescriptions);
            ViewSource.SortDescriptions.Clear();
            for (int i = 0; i < sortDescriptions.Count; i++)
            {
                SortDescription description = sortDescriptions[i];
                ViewSource.SortDescriptions.Add(description);
            }
        }

        private void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            DataGrid grid = (DataGrid)sender;

            ItemContainerGenerator_StatusChanged(grid.ItemContainerGenerator, grid);

            grid.ItemContainerGenerator.StatusChanged += (sender, e) => ItemContainerGenerator_StatusChanged(sender, grid);
        }

        private static void ItemContainerGenerator_StatusChanged(object sender, DataGrid grid)
        {
            ItemContainerGenerator icg = (ItemContainerGenerator)sender;

            if (icg.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
            {
                foreach (DataGridColumn col in grid.Columns)
                {
                    DataGridLength width = col.Width;
                    col.Width = 0;
                    col.Width = width;
                }
            }
        }

        private static void SortDataGrid(DataGrid dataGrid, List<SortDescription> sortDescriptions)
        {
            if (dataGrid == null)
            {
                return;
            }

            dataGrid.Items.SortDescriptions.Clear();
            // Add the new sort descriptions
            foreach (SortDescription sort in sortDescriptions)
            {
                dataGrid.Items.SortDescriptions.Add(sort);
            }

            // Refresh items to display sort
            dataGrid.Items.Refresh();
        }

        private void ClipBoardDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            DataGrid grid = sender as DataGrid;

            grid.Items.IsLiveSorting = true;

            List<SortDescription> sortDescriptions = new();

            sortDescriptions.Add(new SortDescription("SystemName", ListSortDirection.Ascending));
            sortDescriptions.Add(new SortDescription("StationName", ListSortDirection.Ascending));

            SortDataGrid(grid, sortDescriptions);

            grid.ItemContainerGenerator.StatusChanged += (sender, e) => ItemContainerGenerator_StatusChanged(sender, grid);
        }

        private void StationStackDataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            StackInfoByStationScrollView.ScrollToVerticalOffset(StackInfoByStationScrollView.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private void DataGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid grid)
            {
                grid.UnselectAllCells();
            }
        }

        private void MissionDataGrid_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            MissionDataGrid?.UnselectAllCells();
        }
        #endregion

        #region Sidebar Methods
        private void MarketHistoryExapander_Click(object sender, RoutedEventArgs e)
        {
            Settings.Values.ShowMarketHistory = !Settings.Values.ShowMarketHistory;

            ShowSiderBar(Settings.Values.ShowMarketHistory, "\xE973", "\xE974", MarketHistoryColumn, MarketHistoryExapander);
        }

        private static void ShowSiderBar(bool show, string showText, string hideText, ColumnDefinition columnDefinition, Button button)
        {
            columnDefinition.Width = show ? new GridLength(1.0, GridUnitType.Auto) : new GridLength(0);

            button.Content = show ? showText : hideText;
        }

        private void ClipboardExpander_Click(object sender, RoutedEventArgs e)
        {
            Settings.Values.ShowClipBoard = !Settings.Values.ShowClipBoard;

            ShowSiderBar(Settings.Values.ShowClipBoard, "\xE974", "\xE973", ClipboardColumn, ClipboardExpander);
        }

        private void AddClipboardSource_Click(object sender, RoutedEventArgs e)
        {
            AddClipboardSourceView clipboardSourceView = new()
            {
                Owner = this
            };

            if(clipboardSourceView.ShowDialog().Value)
            {
                MissionsContainer.AddToMissionSourceClipBoard(clipboardSourceView.SourceSystem, clipboardSourceView.SourceStation);
            }
        }
        #endregion

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            SettingsView settingsView = new(Settings, MissionsContainer);
            settingsView.Owner = this;
            settingsView.ShowDialog();
        }

        private void MissionDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(sender is DataGrid grid)
            {
                grid.UnselectAll();
            }
        }
    }
}