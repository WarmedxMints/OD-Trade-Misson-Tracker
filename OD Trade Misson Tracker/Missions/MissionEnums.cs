namespace OD_Trade_Mission_Tracker.Missions
{
    public enum MissionState
    {
        Active,
        Redirected,
        Completed,
        Abandonded,
        Failed
    }

    public enum MissionType
    {
        SourceAndReturn,
        Mining,
        Delivery
    }
}
