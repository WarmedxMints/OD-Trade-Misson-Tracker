namespace OD_Trade_Mission_Tracker.Settings
{
    public class Commander
    {
        public string FID { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return $"{Name}";
        }
    }
}
