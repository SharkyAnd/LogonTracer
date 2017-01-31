using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogonTracerAdministrationLib.AppData
{
    public sealed class DateTimeConvertUtils
    {
        public static DateTime? ConvertTimeByUtcOffset(DateTime? originalDate, double utcOffset)
        {
            if (!originalDate.HasValue)
                return originalDate;
            originalDate = originalDate.Value.ToUniversalTime();
            TimeZoneInfo endTimeZone = TimeZoneInfo.GetSystemTimeZones().Where(stz => stz.BaseUtcOffset == TimeSpan.FromHours(utcOffset)).FirstOrDefault();
            //originalDate = DateTime.SpecifyKind(originalDate, DateTimeKind.Local);
            DateTimeOffset localTime = originalDate.Value;

            DateTimeOffset endDateTimeOffset = TimeZoneInfo.ConvertTime(localTime, endTimeZone);

            return endDateTimeOffset.DateTime;
        }
        
    }
}
