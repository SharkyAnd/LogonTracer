using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogonTracerLib.AppData
{
    public sealed class DateTimeConvertUtils
    {
        public static DateTime? ConvertTimeByUtcOffset(DateTime? originalDate, double utcOffset)
        {
            if (!originalDate.HasValue || LogonTracerConfig.Instance.MonitorMode == LogonTracerWorker.MonitorMode.Remote || LogonTracerConfig.Instance.UseLocalTimeZone)
                return originalDate;
            originalDate = originalDate.Value.ToUniversalTime();
            TimeZoneInfo endTimeZone = TimeZoneInfo.GetSystemTimeZones().Where(stz => stz.BaseUtcOffset == TimeSpan.FromHours(utcOffset)).FirstOrDefault();
            //originalDate = DateTime.SpecifyKind(originalDate, DateTimeKind.Local);
            DateTimeOffset localTime = originalDate.Value;

            DateTimeOffset endDateTimeOffset = TimeZoneInfo.ConvertTime(localTime, endTimeZone);

            return endDateTimeOffset.DateTime;
        }
        /// <summary>
        /// Compare two DateTime values
        /// </summary>
        /// <param name="firstDate">DateTime value converted by needed utc offset</param>
        /// <param name="secondDate">Second DateTime value</param>
        /// <param name="utcOffset">[optinal] utcOffset of second value</param>
        /// <returns></returns>
        public static int Compare(DateTime? firstDate, DateTime? secondDate)
        {
            if (!firstDate.HasValue || !secondDate.HasValue)
                return -1;

            secondDate = ConvertTimeByUtcOffset(secondDate.Value, LogonTracerConfig.Instance.UtcOffset);

            if (firstDate.Value.ToString("dd.MM.yyyy HH:mm:ss") != secondDate.Value.ToString("dd.MM.yyyy HH:mm:ss"))
                return -1;
            return 1;
        }
    }
}
