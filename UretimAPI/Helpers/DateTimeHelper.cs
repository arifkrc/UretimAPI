using System;

namespace UretimAPI.Helpers
{
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo TurkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        
        /// <summary>
        /// T�rkiye saat diliminde ?u anki tarih ve saati d�nd�r�r
        /// </summary>
        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TurkeyTimeZone);
        
        /// <summary>
        /// UTC tarihini T�rkiye saat dilimine �evirir
        /// </summary>
        /// <param name="utcDateTime">UTC tarih</param>
        /// <returns>T�rkiye saat dilimindeki tarih</returns>
        public static DateTime ConvertFromUtc(DateTime utcDateTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, TurkeyTimeZone);
        }
        
        /// <summary>
        /// T�rkiye saat dilimindeki tarihi UTC'ye �evirir
        /// </summary>
        /// <param name="turkeyDateTime">T�rkiye saat dilimindeki tarih</param>
        /// <returns>UTC tarih</returns>
        public static DateTime ConvertToUtc(DateTime turkeyDateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(turkeyDateTime, TurkeyTimeZone);
        }
        
        /// <summary>
        /// T�rkiye saat dilimi bilgisini d�nd�r�r
        /// </summary>
        public static TimeZoneInfo TurkeyTimeZoneInfo => TurkeyTimeZone;
    }
}