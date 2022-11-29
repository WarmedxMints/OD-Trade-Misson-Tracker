using System.ComponentModel;

namespace OD_Trade_Mission_Tracker.Settings
{
    public enum DisplayMode
    {
        Legacy,
        Horizons,
        Odyssey,
        Completed
    }

    public enum GridSorting
    {
        [Description("Source System")]
        SourceSystem,
        [Description("Source Station")]
        SourceStation,
        [Description("Destination System")]
        DestinationSystem,
        [Description("Destination Station")]
        DestinationStation,
        [Description("Collections Remaining")]
        CollectionsRemaining,
        [Description("Deliveries Remaining")]
        DeliveriesRemaining,
        [Description("Reward")]
        Reward,
        [Description("Expiry Time")]
        Expiry,
        [Description("Wing Mission")]
        Wing
    }
}
