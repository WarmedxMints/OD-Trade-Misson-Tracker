using EliteJournalReader.Events;
using OD_Trade_Mission_Tracker.Utils;
using System;
using System.Collections.ObjectModel;
using System.Globalization;

namespace OD_Trade_Mission_Tracker.Missions
{
    public class ShipCargo : PropertyChangeNotify
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class ShipLoadout : PropertyChangeNotify
    {
        private string shipType;
        private string shipName;
        private string shipIdent;
        private int cargoCapacity;
        private int usedCapacity;

        public string ShipType { get => shipType; set { shipType = SetShipType(value); OnPropertyChanged(); } }
        public string ShipName { get => shipName; set { shipName = value; OnPropertyChanged(); } }
        public string ShipIdent { get => shipIdent; set { shipIdent = value; OnPropertyChanged(); } }
        public int CargoCapacity { get => cargoCapacity; set { cargoCapacity = value; OnPropertyChanged(); } }
        public int UsedCapacity { get => usedCapacity; set { usedCapacity = value; OnPropertyChanged(); } }

        public ObservableCollection<ShipCargo> CurrentCargo { get; set; } = new();

        private static string SetShipType(string name)
        {
            if(ItemInfo.ShipIdents.ContainsKey(name))
            {
                return ItemInfo.ShipIdents[name];
            }

            TextInfo textInfo = new CultureInfo("en-GB", false).TextInfo;

            return textInfo.ToTitleCase(name);
        }
        public void UpdateFromLoadoutEvent(LoadoutEvent.LoadoutEventArgs e)
        {
            ShipType = e.Ship;
            ShipName = e.ShipName;
            ShipIdent = e.ShipIdent;
            CargoCapacity = e.CargoCapacity;
        }

        public void UpdateCargo(CargoEvent.CargoEventArgs e)
        {
            if(e is null)
            {
                return;
            }

            CurrentCargo.ClearCollection();

            UsedCapacity = e.Count;

            if (e.Count == 0 || string.Equals(e.Vessel, "Ship", StringComparison.OrdinalIgnoreCase) == false)
            {
                return;
            }

            TextInfo textInfo = new CultureInfo("en-GB", false).TextInfo;

            for (int i = 0; i < e.Inventory.Length; i++)
            {
                EliteJournalReader.Commodity cargoItem = e.Inventory[i];

                ShipCargo item = new()
                {
                    Name = textInfo.ToTitleCase(cargoItem.Name.ToLowerInvariant()),
                    Count = cargoItem.Count
                };

                CurrentCargo.AddToCollection(item);
            }
        }
    }
}
