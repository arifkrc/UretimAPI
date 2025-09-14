using System;

namespace UretimAPI.Helpers
{
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo TurkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        
        /// <summary>
        /// Türkiye saat diliminde ?u anki tarih ve saati döndürür
        /// </summary>
        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TurkeyTimeZone);
        
        /// <summary>
        /// UTC tarihini Türkiye saat dilimine çevirir
        /// </summary>
        /// <param name="utcDateTime">UTC tarih</param>
        /// <returns>Türkiye saat dilimindeki tarih</returns>
        public static DateTime ConvertFromUtc(DateTime utcDateTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, TurkeyTimeZone);
        }
        
        /// <summary>
        /// Türkiye saat dilimindeki tarihi UTC'ye çevirir
        /// </summary>
        /// <param name="turkeyDateTime">Türkiye saat dilimindeki tarih</param>
        /// <returns>UTC tarih</returns>
        public static DateTime ConvertToUtc(DateTime turkeyDateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(turkeyDateTime, TurkeyTimeZone);
        }
        
        /// <summary>
        /// Türkiye saat dilimi bilgisini döndürür
        /// </summary>
        public static TimeZoneInfo TurkeyTimeZoneInfo => TurkeyTimeZone;
    }
}