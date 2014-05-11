using System;

namespace SpagetiLib
{
    public static class EpochConverter
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime ToDateTimeOfEpoch(this long epoch)
        {
            return Epoch.AddMilliseconds(epoch);
        }

        public static long ToEpoch(this DateTime date)
        {
            return Convert.ToInt64((date.ToUniversalTime() - Epoch).TotalMilliseconds);
        }

        public static DateTime ToMillisecondPrecision(this DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Millisecond,
                date.Kind);
        }
    }
}