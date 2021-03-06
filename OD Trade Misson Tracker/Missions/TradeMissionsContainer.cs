using EliteJournalReader;
using EliteJournalReader.Events;
using OD_Trade_Mission_Tracker.Commodities;
using OD_Trade_Mission_Tracker.CustomMessageBox;
using OD_Trade_Mission_Tracker.Settings;
using OD_Trade_Mission_Tracker.SourceClipboard;
using OD_Trade_Mission_Tracker.SystemInfo;
using OD_Trade_Mission_Tracker.Utils;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace OD_Trade_Mission_Tracker.Missions
{
    public class TradeMissionsContainer : PropertyChangeNotify
    {
        #region Private Values
        private string HorizonDataSaveFile => Path.Combine(Directory.GetCurrentDirectory(), "Data", $"[{CommanderFID}] HorizonsMissions.json");
        private string OdysseyDataSaveFile => Path.Combine(Directory.GetCurrentDirectory(), "Data", $"[{CommanderFID}] OdysseyMissions.json");
        private string MarketDataSaveFile => Path.Combine(Directory.GetCurrentDirectory(), "Data", $"[{CommanderFID}] MarketData.json");

        private static readonly string clipboarDataSaveFile = Path.Combine(Directory.GetCurrentDirectory(), "Data", "MissionSourceClipboard.json");

        private readonly string[] validMissionNames = new string[] { "Mission_Collect", "Mission_Delivery", "Mission_Mining", "Mission_Altruism" };

        private Dictionary<long, TradeMissionData> horizonMissionsData, odysseyMissionsData;

        private readonly AppSettings appSettings;
        #endregion

        #region Properties and Constructor
        public JournalWatcher JournalWatcher { get; private set; }
        public string CommanderFID => appSettings.CurrentCommander?.FID;
        public bool Odyssey { get; private set; }

        private TradeMissionManager horizonMissions = new();
        private TradeMissionManager odysseyMissions = new();
        private TradeMissionManager completedMissions = new();
        private ShipLoadout currentShip = new();
        private SystemData currentSystem = new();
        private Station currentStation = new();
        private ObservableCollection<CommodityData> commodities = new();
        private ObservableCollection<MissionSourceClipboardData> missionSourceClipboards = new();

        public TradeMissionManager HorizionMissions { get => horizonMissions; set { horizonMissions = value; OnPropertyChanged(); } }
        public TradeMissionManager OdysseyMissions { get => odysseyMissions; set { odysseyMissions = value; OnPropertyChanged(); } }
        public TradeMissionManager CompletedMissions { get => completedMissions; set { completedMissions = value; OnPropertyChanged(); } }
        public ShipLoadout CurrentShip { get => currentShip; set { currentShip = value; OnPropertyChanged(); } }
        public SystemData CurrentSystem { get => currentSystem; set { currentSystem = value; OnPropertyChanged(); } }
        public Station CurrentStation { get => currentStation; set { currentStation = value; OnPropertyChanged(); } }
        public ObservableCollection<CommodityData> Commodities { get => commodities; set { commodities = value; OnPropertyChanged(); } }
        public ObservableCollection<MissionSourceClipboardData> MissionSourceClipboards { get => missionSourceClipboards; set { missionSourceClipboards = value; OnPropertyChanged(); } }

        public TradeMissionManager CurrentManager => appSettings.Values.ViewDisplayMode switch
        {
            DisplayMode.Horizons => HorizionMissions,
            DisplayMode.Odyssey => OdysseyMissions,
            DisplayMode.Completed => CompletedMissions,
            _ => HorizionMissions,
        };

        public TradeMissionsContainer(AppSettings settings)
        {
            appSettings = settings;
            appSettings.CommanderChanged += AppSettings_CommanderChanged;
        }
        #endregion

        #region Init and Events
        public void Init()
        {          
            SetCurrentManager();

            RestartWatcher();       
        }

        public void RestartWatcher()
        {
            if (JournalWatcher is not null)
            {
                UnSubscribeFromEvents();
                JournalWatcher.StopWatching();
            }

            if (Directory.Exists(appSettings.Values.JournalPath) == false)
            {
                _ = ODMessageBox.Show(Application.Current.Windows.OfType<MainWindow>().First(),
                                                         $"Journal Directory Not Found\nPlease Specify Journal Log Folder",
                                                         MessageBoxButton.OK);

                if (FindJournalDir() == false)
                {
                    return;
                }
            }

            JournalWatcher = new(appSettings.Values.JournalPath);

            SubscribeToEvents();

            JournalWatcher.StartWatching().ConfigureAwait(false);
        }

        private bool FindJournalDir()
        {
            VistaFolderBrowserDialog folder = new()
            {
                Multiselect = false,
                Description = "Select ED Journal Folder",
                UseDescriptionForTitle = true
            };

            if (folder.ShowDialog().Value)
            {
                appSettings.Values.CustomJournalPath = folder.SelectedPath;
                return true;
            }

            _ = ODMessageBox.Show(Application.Current.Windows.OfType<MainWindow>().First(),
                                            "Journal Directory Not Set\n\nA Valid Directory Is Required",
                                             MessageBoxButton.OK);
            return false;
        }

        private void SubscribeToEvents()
        {
            JournalWatcher.GetEvent<FileheaderEvent>()?.AddHandler(OnFileHeaderEvent);

            JournalWatcher.GetEvent<LocationEvent>()?.AddHandler(OnLocationEvent);

            JournalWatcher.GetEvent<DockedEvent>()?.AddHandler(OnDockedAtStation);

            JournalWatcher.GetEvent<UndockedEvent>()?.AddHandler(OnUndockedFromStation);

            JournalWatcher.GetEvent<MissionAcceptedEvent>()?.AddHandler(OnMissionAccepted);

            JournalWatcher.GetEvent<MissionRedirectedEvent>()?.AddHandler(OnMissionRedirected);

            JournalWatcher.GetEvent<MissionCompletedEvent>()?.AddHandler(OnMissionCompleted);

            JournalWatcher.GetEvent<MissionAbandonedEvent>()?.AddHandler(OnMissionAbandonded);

            JournalWatcher.GetEvent<MissionFailedEvent>()?.AddHandler(OnMissionFailed);

            JournalWatcher.GetEvent<CommanderEvent>()?.AddHandler(OnCommanderEvent);

            JournalWatcher.GetEvent<MarketBuyEvent>()?.AddHandler(OnMarketBuy);

            JournalWatcher.GetEvent<CargoDepotEvent>()?.AddHandler(OnCargoDepot);

            JournalWatcher.GetEvent<CargoEvent>()?.AddHandler(OnCargo);

            JournalWatcher.GetEvent<LoadoutEvent>()?.AddHandler(OnLoadoutEvent);

            JournalWatcher.GetEvent<CarrierJumpEvent>()?.AddHandler(OnCarrierJump);

            JournalWatcher.GetEvent<FSDJumpEvent>()?.AddHandler(OnFSDJump);
        }

        private void UnSubscribeFromEvents()
        {
            JournalWatcher.GetEvent<FileheaderEvent>()?.RemoveHandler(OnFileHeaderEvent);

            JournalWatcher.GetEvent<LocationEvent>()?.RemoveHandler(OnLocationEvent);

            JournalWatcher.GetEvent<DockedEvent>()?.RemoveHandler(OnDockedAtStation);

            JournalWatcher.GetEvent<UndockedEvent>()?.RemoveHandler(OnUndockedFromStation);

            JournalWatcher.GetEvent<MissionAcceptedEvent>()?.RemoveHandler(OnMissionAccepted);

            JournalWatcher.GetEvent<MissionRedirectedEvent>()?.RemoveHandler(OnMissionRedirected);

            JournalWatcher.GetEvent<MissionCompletedEvent>()?.RemoveHandler(OnMissionCompleted);

            JournalWatcher.GetEvent<MissionAbandonedEvent>()?.RemoveHandler(OnMissionAbandonded);

            JournalWatcher.GetEvent<MissionFailedEvent>()?.RemoveHandler(OnMissionFailed);

            JournalWatcher.GetEvent<CommanderEvent>()?.RemoveHandler(OnCommanderEvent);

            JournalWatcher.GetEvent<MarketBuyEvent>()?.RemoveHandler(OnMarketBuy);

            JournalWatcher.GetEvent<CargoDepotEvent>()?.RemoveHandler(OnCargoDepot);

            JournalWatcher.GetEvent<CargoEvent>()?.RemoveHandler(OnCargo);

            JournalWatcher.GetEvent<LoadoutEvent>()?.RemoveHandler(OnLoadoutEvent);

            JournalWatcher.GetEvent<CarrierJumpEvent>()?.RemoveHandler(OnCarrierJump);

            JournalWatcher.GetEvent<FSDJumpEvent>()?.RemoveHandler(OnFSDJump);
        }

        private void OnFileHeaderEvent(object sender, FileheaderEvent.FileheaderEventArgs e)
        {
            if (JournalWatcher.ReadingHistory)
            {
                return;
            }

            Odyssey = e.Odyssey;
        }

        private void OnLocationEvent(object sender, LocationEvent.LocationEventArgs e)
        {
            if (JournalWatcher.ReadingHistory)
            {
                return;
            }

            CurrentSystem = new()
            {
                SystemName = e.StarSystem,
                SystemAddress = e.SystemAddress,
                SystemPos = e.StarPos
            };

            UpdateDistances();

            if (e.Docked == false)
            {
                return;
            }

            CurrentStation = new()
            {
                StarSystem = e.StarSystem,
                SystemAddress = e.SystemAddress,
                StationName = e.StationName,
                MarketID = e.MarketID
            };
        }

        private void OnDockedAtStation(object sender, DockedEvent.DockedEventArgs e)
        {
            if (JournalWatcher.ReadingHistory)
            {
                return;
            }

            CurrentStation = new()
            {
                StarSystem = e.StarSystem,
                SystemAddress = e.SystemAddress,
                StationName = e.StationName,
                MarketID = e.MarketID
            };
        }

        private void OnUndockedFromStation(object sender, UndockedEvent.UndockedEventArgs e)
        {
            if (JournalWatcher.ReadingHistory)
            {
                return;
            }

            CurrentStation = null;
        }

        private void OnMissionAccepted(object sender, MissionAcceptedEvent.MissionAcceptedEventArgs e)
        {
            if (JournalWatcher.IsLive == false || currentStation == null)
            {
                return;
            }

            if (ValidMission(e.Count, e.Name) == false)
            {
                return;
            }

            TradeMissionData missionData = new(e, currentStation, this);
            missionData.CurrentState = MissionState.Active;

            TradeMissionManager manager = Odyssey ? OdysseyMissions : HorizionMissions;

            AddMissionToDictionary(ref Odyssey ? ref odysseyMissionsData : ref horizonMissionsData, missionData);
            AddMissionToGui(manager, missionData);

            SaveData();
        }

        private void OnMissionRedirected(object sender, MissionRedirectedEvent.MissionRedirectedEventArgs e)
        {
            if (JournalWatcher.IsLive == false)
            {
                return;
            }

            if (Odyssey ? odysseyMissionsData is null || !odysseyMissionsData.ContainsKey(e.MissionID) : horizonMissionsData is null || !horizonMissionsData.ContainsKey(e.MissionID))
            {
                return;
            }

            if (Odyssey)
            {
                odysseyMissionsData[e.MissionID].CurrentState = MissionState.Redirected;
                OdysseyMissions.UpdateMission(odysseyMissionsData[e.MissionID]);
                SaveData();
                return;
            }

            horizonMissionsData[e.MissionID].CurrentState = MissionState.Redirected;
            HorizionMissions.UpdateMission(odysseyMissionsData[e.MissionID]);
            SaveData();
        }

        private void OnMissionCompleted(object sender, MissionCompletedEvent.MissionCompletedEventArgs e)
        {
            if (JournalWatcher.IsLive == false)
            {
                return;
            }

            if (Odyssey ? odysseyMissionsData is null || !odysseyMissionsData.ContainsKey(e.MissionID) : horizonMissionsData is null || !horizonMissionsData.ContainsKey(e.MissionID))
            {
                return;
            }

            TradeMissionData missionData;

            if (Odyssey)
            {
                missionData = odysseyMissionsData[e.MissionID];
                OdysseyMissions.RemoveMission(missionData);
            }
            else
            {
                missionData = horizonMissionsData[e.MissionID];
                HorizionMissions.RemoveMission(missionData);
            }

            missionData.CurrentState = MissionState.Completed;
            missionData.Reward = e.Reward;

            CompletedMissions.AddMission(missionData);
            SaveData();
        }

        private void OnMissionAbandonded(object sender, MissionAbandonedEvent.MissionAbandonedEventArgs e)
        {
            if (JournalWatcher.IsLive == false)
            {
                return;
            }

            if (Odyssey ? odysseyMissionsData is null || !odysseyMissionsData.ContainsKey(e.MissionID) : horizonMissionsData is null || !horizonMissionsData.ContainsKey(e.MissionID))
            {
                return;
            }

            TradeMissionData missionData;

            if (Odyssey)
            {
                missionData = odysseyMissionsData[e.MissionID];
                OdysseyMissions.RemoveMission(missionData);
            }
            else
            {
                missionData = horizonMissionsData[e.MissionID];
                HorizionMissions.RemoveMission(missionData);
            }

            missionData.CurrentState = MissionState.Abandonded;
            missionData.Reward = 0;

            CompletedMissions.AddMission(missionData);
            SaveData();
        }

        private void OnMissionFailed(object sender, MissionFailedEvent.MissionFailedEventArgs e)
        {
            if (JournalWatcher.IsLive == false)
            {
                return;

            }
            if (Odyssey ? odysseyMissionsData is null || !odysseyMissionsData.ContainsKey(e.MissionID) : horizonMissionsData is null || !horizonMissionsData.ContainsKey(e.MissionID))
            {
                return;
            }


            TradeMissionData missionData;

            if (Odyssey)
            {
                missionData = odysseyMissionsData[e.MissionID];
                OdysseyMissions.RemoveMission(missionData);
            }
            else
            {
                missionData = horizonMissionsData[e.MissionID];
                HorizionMissions.RemoveMission(missionData);
            }

            missionData.CurrentState = MissionState.Failed;
            missionData.Reward = 0;

            CompletedMissions.AddMission(missionData);
            SaveData();
        }

        private void OnCommanderEvent(object sender, CommanderEvent.CommanderEventArgs e)
        {
            if (JournalWatcher.ReadingHistory)
            {
                return;
            }

            Commander cmdr = appSettings.Values.Commanders.FirstOrDefault(x => x.FID == e.FID);

            if (cmdr == default)
            {
                cmdr = new()
                {
                    FID = e.FID,
                    Name = e.Name
                };

                appSettings.Values.Commanders.AddToCollection(cmdr);
                appSettings.CurrentCommander = cmdr;

                CompletedMissions.ClearData();
                HorizionMissions.ClearData();
                OdysseyMissions.ClearData();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBoxResult result = ODMessageBox.Show(Application.Current.Windows.OfType<MainWindow>().First(),
                                                         $"New Commander Detected - {cmdr.Name}\nWould you like to read mission history?",
                                                         MessageBoxButton.YesNo);

                    if (result == MessageBoxResult.Yes)
                    {
                        MissionHistoryReaderView reader = new(this, true);
                        reader.Owner = Application.Current.Windows.OfType<MainWindow>().First();
                        _ = reader.ShowDialog();
                    }
                });

                return;
            }

            appSettings.CurrentCommander = cmdr;
        }

        private void OnMarketBuy(object sender, MarketBuyEvent.MarketBuyEventArgs e)
        {
            if (JournalWatcher.IsLive == false || CurrentSystem is null || CurrentStation is null || e.MarketID != currentStation.MarketID)
            {
                return;
            }

            TextInfo textInfo = new CultureInfo("en-GB", false).TextInfo;

            CommodityData commodity = new()
            {
                CommodityName = textInfo.ToTitleCase(e.Type.ToLowerInvariant()),
                System = CurrentSystem.SystemName,
                SystemAddress = CurrentSystem.SystemAddress,
                SystemPos = CurrentSystem.SystemPos.Copy(),
                Station = CurrentStation.StationName,
                Cost = e.BuyPrice,
                Container = this
            };

            Commodities.InsertAt(commodity, 0);

            if (Commodities.Count > 20)
            {
                for (int i = 20; i < Commodities.Count; i++)
                {
                    Commodities.RemoveAtIndex(i);
                }
            }

            UpdateDistances();
        }

        private void UpdateDistances()
        {
            if (currentSystem is null || commodities is null || commodities.Count == 0)
            {
                return;
            }

            foreach (CommodityData commodity in commodities)
            {
                commodity.Distance = SystemPosition.Distance(currentSystem.SystemPos, commodity.SystemPos);
            }
        }

        private void OnCargoDepot(object sender, CargoDepotEvent.CargoDepotEventArgs e)
        {
            if (JournalWatcher.IsLive == false)
            {
                return;
            }

            Dictionary<long, TradeMissionData> dict = Odyssey ? odysseyMissionsData : horizonMissionsData;

            if (dict.ContainsKey(e.MissionID) == false)
            {
                return;
            }

            dict[e.MissionID].ItemsCollected = e.ItemsCollected;
            dict[e.MissionID].ItemsDelivered = e.ItemsDelivered;

            TradeMissionManager manager = Odyssey ? OdysseyMissions : HorizionMissions;
            manager.UpdateMission(dict[e.MissionID]);
        }

        private void OnCargo(object sender, CargoEvent.CargoEventArgs e)
        {
            if (JournalWatcher.IsLive == false)
            {
                return;
            }

            CurrentShip?.UpdateCargo(JournalWatcher.ReadCargoJson() ?? e);
        }

        private void OnLoadoutEvent(object sender, LoadoutEvent.LoadoutEventArgs e)
        {
            if (JournalWatcher.ReadingHistory)
            {
                return;
            }

            CurrentShip?.UpdateFromLoadoutEvent(e);
            CurrentShip?.UpdateCargo(JournalWatcher.ReadCargoJson());
        }

        private void OnFSDJump(object sender, FSDJumpEvent.FSDJumpEventArgs e)
        {
            if (JournalWatcher.ReadingHistory)
            {
                return;
            }

            CurrentSystem = new()
            {
                SystemName = e.StarSystem,
                SystemAddress = e.SystemAddress,
                SystemPos = e.StarPos
            };

            UpdateDistances();
        }

        private void OnCarrierJump(object sender, CarrierJumpEvent.CarrierJumpEventArgs e)
        {
            if (JournalWatcher.ReadingHistory || e.Docked == false)
            {
                return;
            }

            CurrentSystem = new()
            {
                SystemName = e.StarSystem,
                SystemAddress = e.SystemAddress,
                SystemPos = e.StarPos
            };

            UpdateDistances();
        }

        private void AppSettings_CommanderChanged(object sender, EventArgs e)
        {
            LoadData();
        }
        #endregion

        public EventHandler OnDataLoaded;

        public void SetCurrentManager()
        {
            OnPropertyChanged("CurrentManager");
        }

        #region Mission Processing
        private static void AddMissionToDictionary(ref Dictionary<long, TradeMissionData> missionDictionary, TradeMissionData missionData)
        {
            if (missionDictionary == null)
            {
                missionDictionary = new();
            }

            if (missionDictionary.ContainsKey(missionData.MissionID))
            {
                return;
            }

            missionDictionary.Add(missionData.MissionID, missionData);
        }

        private static void AddMissionToGui(TradeMissionManager manager, TradeMissionData missionData)
        {
            if (manager == null)
            {
                return;
            }
            manager.AddMission(missionData);
        }

        private void ProcessDictionary(Dictionary<long, TradeMissionData> missionData, TradeMissionManager missionManager)
        {
            List<TradeMissionData> compeletedMissions = new();
            List<TradeMissionData> activeMissions = new();

            foreach (KeyValuePair<long, TradeMissionData> mission in missionData)
            {
                mission.Value.SetContainer(this);

                if (mission.Value.CurrentState is MissionState.Completed or MissionState.Abandonded or MissionState.Failed)
                {
                    compeletedMissions.Add(mission.Value);
                    continue;
                }

                activeMissions.Add(mission.Value);
            }

            missionManager.AddMissions(activeMissions);
            CompletedMissions.AddMissions(compeletedMissions);
        }

        internal void ProcessHistory(Dictionary<long, TradeMissionData> horizonMissions, Dictionary<long, TradeMissionData> odysseyMissions, List<CommodityData> commodities, IProgress<string> progress)
        {
            CompletedMissions.ClearData();
            HorizionMissions.ClearData();
            OdysseyMissions.ClearData();
            Commodities.ClearCollection();

            if (commodities is not null)
            {
                Commodities = new ObservableCollection<CommodityData>(commodities);

                foreach (CommodityData commodity in Commodities)
                {
                    commodity.Container = this;
                }
            }

            if (horizonMissions is not null)
            {
                horizonMissionsData = new(horizonMissions);
                progress.Report("Horizon Missions");
                ProcessDictionary(horizonMissions, HorizionMissions);
            }


            if (odysseyMissions is not null)
            {
                odysseyMissionsData = new(odysseyMissions);
                progress.Report("Odyssey Missions");
                ProcessDictionary(odysseyMissions, OdysseyMissions);
            }

            OnDataLoaded?.Invoke(this, new EventArgs());

            SaveData();
        }

        private bool ValidMission(int? count, string missionName)
        {
            if (count is null || count == 0)
            {
                return false;
            }

            for (int i = 0; i < validMissionNames.Length; i++)
            {
                if (missionName.StartsWith(validMissionNames[i]))
                {
                    return true;
                }
            }

            return false;
        }

        internal void PurgeMissions(MissionState stateToPurge)
        {
            if (CompletedMissions is null || CompletedMissions.Missions.Count == 0)
            {
                return;
            }
            _ = Task.Run(() =>
            {
                List<TradeMissionData> missionsToPurge = CompletedMissions.Missions.Where(x => x.CurrentState == stateToPurge).ToList();

                foreach (TradeMissionData mission in missionsToPurge)
                {
                    if (horizonMissionsData.ContainsKey(mission.MissionID))
                    {
                        horizonMissionsData.Remove(mission.MissionID);
                        continue;
                    }

                    if (odysseyMissionsData.ContainsKey(mission.MissionID))
                    {
                        odysseyMissionsData.Remove(mission.MissionID);
                    }
                }

                CompletedMissions.RemoveMissions(missionsToPurge);

                OnDataLoaded?.Invoke(this, new EventArgs());

                SaveData();
            });
        }

        internal void MoveMission(TradeMissionData missionData, bool moveToActive)
        {
            bool horizonsMission = false;

            if (horizonMissionsData is not null && horizonMissionsData.ContainsKey(missionData.MissionID))
            {
                horizonsMission = true;
            }
            else if (odysseyMissionsData is not null && odysseyMissionsData.ContainsKey(missionData.MissionID) == false)
            {
                return;
            }

            if (moveToActive)
            {
                if (CompletedMissions.Missions.Contains(missionData))
                {
                    CompletedMissions.RemoveMission(missionData);
                }

                if (horizonsMission && !HorizionMissions.Missions.Contains(missionData))
                {
                    HorizionMissions.AddMission(missionData);
                    return;
                }

                if (horizonsMission || OdysseyMissions.Missions.Contains(missionData))
                {
                    return;
                }

                OdysseyMissions.AddMission(missionData);
                return;
            }

            if (!CompletedMissions.Missions.Contains(missionData))
            {
                CompletedMissions.AddMission(missionData);
            }
            else
            {
                CompletedMissions.UpdateCommoditiesStack(missionData.Commodity);
            }

            if (horizonsMission && HorizionMissions.Missions.Contains(missionData))
            {
                HorizionMissions.RemoveMission(missionData);
                return;
            }

            if (horizonsMission || !OdysseyMissions.Missions.Contains(missionData))
            {
                return;
            }

            OdysseyMissions.RemoveMission(missionData);
        }

        public void DeleteMission(TradeMissionData missionData)
        {
            TradeMissionManager manager = GetMissionsManager(missionData);

            if (manager is null)
            {
                return;
            }

            manager.RemoveMission(missionData);

            Dictionary<long, TradeMissionData> dictionary = GetMissionDictionary(missionData);

            if (dictionary is not null)
            {
                dictionary.Remove(missionData.MissionID);
            }

            SaveData();
        }

        private Dictionary<long, TradeMissionData> GetMissionDictionary(TradeMissionData missionData)
        {
            if (horizonMissionsData is not null && horizonMissionsData.ContainsKey(missionData.MissionID))
            {
                return horizonMissionsData;
            }
            else if (odysseyMissionsData is not null && odysseyMissionsData.ContainsKey(missionData.MissionID))
            {
                return odysseyMissionsData;
            }

            return null;
        }

        private TradeMissionManager GetMissionsManager(TradeMissionData missionData)
        {
            if (horizonMissions.Missions.Contains(missionData))
            {
                return horizonMissions;
            }

            if (odysseyMissions.Missions.Contains(missionData))
            {
                return odysseyMissions;
            }

            if (completedMissions.Missions.Contains(missionData))
            {
                return completedMissions;
            }

            return null;
        }
        #endregion

        #region Clipboard Methods
        public void AddToMissionSourceClipBoard(string system, string station)
        {
            MissionSourceClipboardData clipboard = MissionSourceClipboards.FirstOrDefault(x => x.SystemName == system &&
                                                                                            x.StationName == station);
            if (clipboard is null)
            {
                clipboard = new()
                {
                    SystemName = system,
                    StationName = station
                };

                clipboard.SetContainer(this);

                MissionSourceClipboards.AddToCollection(clipboard);

                _ = LoadSaveJson.SaveJson(MissionSourceClipboards, clipboarDataSaveFile);
            }
        }

        public void RemoveClipboardEntry(MissionSourceClipboardData data)
        {
            if (MissionSourceClipboards.Contains(data))
            {
                MissionSourceClipboards.Remove(data);

                _ = LoadSaveJson.SaveJson(MissionSourceClipboards, clipboarDataSaveFile);
            }
        }
        #endregion

        #region Persistance
        private void LoadData()
        {
            if (appSettings == null || appSettings.CurrentCommander == null)
            {
                return;
            }

            CompletedMissions.ClearData();
            HorizionMissions.ClearData();
            OdysseyMissions.ClearData();

            horizonMissionsData = LoadSaveJson.LoadJson<Dictionary<long, TradeMissionData>>(HorizonDataSaveFile);

            if (horizonMissionsData is not null)
            {
                ProcessDictionary(horizonMissionsData, HorizionMissions);
            }

            odysseyMissionsData = LoadSaveJson.LoadJson<Dictionary<long, TradeMissionData>>(OdysseyDataSaveFile);

            if (odysseyMissionsData is not null)
            {
                ProcessDictionary(odysseyMissionsData, OdysseyMissions);
            }

            ObservableCollection<CommodityData> save = LoadSaveJson.LoadJson<ObservableCollection<CommodityData>>(MarketDataSaveFile);

            if (save is not null)
            {
                Commodities = save;

                foreach (CommodityData commodity in Commodities)
                {
                    commodity.Container = this;
                }
            }

            ObservableCollection<MissionSourceClipboardData> missionSourceClipboards = LoadSaveJson.LoadJson<ObservableCollection<MissionSourceClipboardData>>(clipboarDataSaveFile);

            if (missionSourceClipboards is not null)
            {
                foreach (MissionSourceClipboardData clipboard in missionSourceClipboards)
                {
                    if (MissionSourceClipboards.Contains(clipboard) == false)
                    {
                        MissionSourceClipboards.AddToCollection(clipboard);
                        clipboard.SetContainer(this);
                    }
                }
            }

            OnDataLoaded?.Invoke(this, new EventArgs());
        }

        public void SaveData()
        {
            if (appSettings == null || appSettings.CurrentCommander == null)
            {
                return;
            }

            _ = LoadSaveJson.SaveJson(horizonMissionsData, HorizonDataSaveFile);
            _ = LoadSaveJson.SaveJson(odysseyMissionsData, OdysseyDataSaveFile);
            _ = LoadSaveJson.SaveJson(commodities, MarketDataSaveFile);
            _ = LoadSaveJson.SaveJson(missionSourceClipboards, clipboarDataSaveFile);
        }
        #endregion
    }
}