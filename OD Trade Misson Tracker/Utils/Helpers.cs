using System;
using System.Windows;

namespace OD_Trade_Mission_Tracker.Utils
{
    public class Helpers
    {
        public static void SetClipboard(object data)
        {
            try
            {
                Clipboard.SetDataObject(data, true);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Error sending Name to Clipboard\nError : {ex.Message}");
            }
        }

        public static long SafeDivision(long Numeraotr, long Denominator)
        {
            return (Denominator == 0) ? 0 : Numeraotr / Denominator;
        }
    }
}