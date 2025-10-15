namespace OrderManagementSystem.Helpers
{
    public static class DateTimeHelper
    {
        // Bangladesh is UTC+6
        private static readonly int BangladeshUtcOffset = 6;

        public static DateTime ToLocalTime(this DateTime utcDateTime)
        {
            if (utcDateTime.Kind == DateTimeKind.Utc)
            {
                return utcDateTime.AddHours(BangladeshUtcOffset);
            }
            return utcDateTime.AddHours(BangladeshUtcOffset);
        }

        public static string ToLocalTimeString(this DateTime utcDateTime, string format = "MMM dd, yyyy hh:mm tt")
        {
            return utcDateTime.ToLocalTime().ToString(format);
        }
    }
}