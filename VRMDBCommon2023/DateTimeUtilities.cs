namespace VRMDBCommon2023
{
    public static partial class DateTimeUtilities
    {
        /// <summary>
        /// declared different types of date formats 
        /// </summary>
        public enum FormatType
        {
            MMMMDDYYYY,
            DateForSql,
            DateTimeForSql,
            DDMMYYYY,
            DDMMYYYY_HHMM,
            DDMMMYYYY,
            HHMMSS,
            DDDMMMYYYY,
            DDDMMMYYYY_HHMM,
            DDDMMMYYYY_HHMMSS,
            DDDMMMYYYY_withoutseparator,
            DDDDMMMYYYY,
            DDDDMMMYYYY_withoutseparator,
            YYYYMMDD,
            YYYY_MM_DD,
            DDMMMYYYYDDD,
            HHMM,
            DDDMMMYY,
            DDDMM,
            DDDMMMYY_yearwithpostrophe,
            DDMMYYYY_withoutseperator,
            DD_MM_YYYY,
            DDMMMMYYYY,
            HHMMSS_withoutseperator,
            DDMMYYYYTIME,
            DOW_Abbr,
            DOW,
            DDMMMYYYY_hyphen,
            DDMMMYYYYTIME_hyphen,
            DDMMMYY,
            DDMMMCommaHHMM,
            d,
            t,
            g,
            DDMMMYYYY_hhmmsstt,
            YYYYMMDD_HHmmssfff,
            DDMMYYYY_withdotseparate,
            DDMMMYYYY_hhmmtt_hyphen
        }

        /// <summary>
        /// Interval between dates
        /// </summary>
        public enum DateInterval
        {
            Day,
            DayOfYear,
            Hour,
            Minute,
            Month,
            Quarter,
            Second,
            Weekday,
            WeekOfYear,
            Year
        }

        public static string GetFormatOfDateTime(FormatType formatType)
        {
            switch (formatType)
            {
                case FormatType.MMMMDDYYYY:
                    return "MMMM dd yyyy";
                case FormatType.DateForSql:
                    return "MM/dd/yyyy";
                case FormatType.DateTimeForSql:
                    return "MM/dd/yyyy hh:mm:ss";
                case FormatType.DDMMYYYY:
                    return "dd/MM/yyyy";
                case FormatType.DDMMYYYY_HHMM:
                    return "dd/MM/yyyy HH:mm";
                case FormatType.DDMMMYYYY:
                    return "dd MMM yyyy";
                case FormatType.DDMMMYYYY_hyphen:
                    return "dd-MMM-yyyy";
                case FormatType.DDMMMYYYYTIME_hyphen:
                    return "dd-MMM-yyyy HH:mm";
                case FormatType.HHMMSS:
                    return "HH:mm:ss";
                case FormatType.HHMM:
                    return "HH:mm";
                case FormatType.DDDMMMYYYY:
                    return "ddd, dd MMM yyyy";
                case FormatType.DDDMMMYYYY_HHMM:
                    return "ddd, dd MMM yyyy HH:mm";
                case FormatType.DDDMMMYYYY_HHMMSS:
                    return "ddd dd MMM yyyy HH:mm:ss";
                case FormatType.DDDMMMYYYY_withoutseparator:
                    return "ddd dd MMM yyyy";
                case FormatType.DDMMYYYYTIME:
                    return "dd/MM/yyyy HH:mm:ss";
                case FormatType.DDDDMMMYYYY:
                    return "dddd, dd MMMM yyyy";
                case FormatType.DDDDMMMYYYY_withoutseparator:
                    return "dddd dd MMMM yyyy";
                case FormatType.YYYYMMDD:
                    return "yyyyMMdd";
                case FormatType.YYYY_MM_DD:
                    return "yyyy-MM-dd";
                case FormatType.DDDMMMYY:
                    return "ddd dd MMM yy";
                case FormatType.DDDMM:
                    return "ddd, dd MMM";
                case FormatType.DDDMMMYY_yearwithpostrophe:
                    return "ddd dd MMM -yy";
                case FormatType.DDMMYYYY_withoutseperator:
                    return "ddMMyyyy";
                case FormatType.DD_MM_YYYY:
                    return "dd-MM-yyyy";
                case FormatType.DDMMMMYYYY:
                    return "dd MMMM yyyy";
                case FormatType.HHMMSS_withoutseperator:
                    return "HHmmss";
                case FormatType.DOW_Abbr:
                    return "ddd";
                case FormatType.DOW:
                    return "dddd";
                case FormatType.DDMMMYYYYDDD:
                    return " dd-MMM-yyyy (dddd)";
                case FormatType.DDMMMYY:
                    return "dd MMM yy";
                case FormatType.DDMMMCommaHHMM:
                    return "dd MMM, HH:mm";
                case FormatType.d:
                    return "d";
                case FormatType.t:
                    return "t";
                case FormatType.g:
                    return "g";
                case FormatType.DDMMMYYYY_hhmmsstt:
                    return "ddd, dd MMM yyyy hh:mm:ss tt";
                case FormatType.YYYYMMDD_HHmmssfff:
                    return "yyyy-MM-dd HH:mm:ss.fff";
                case FormatType.DDMMMYYYY_hhmmtt_hyphen:
                    return "dd-MM-yyyy HH:mm:ss.fff";
                default:
                    return "";
            }
        }

        /// <summary>
        /// format nullable date and returns empty string
        /// </summary>
        /// <param name="objDateTime">datetime value</param>
        /// <param name="formatString"> convert to date format</param>
        /// <returns></returns>
        public static string FormateNullableDate(this Nullable<DateTime> objDateTime, FormatType formatString)
        {
            var format = GetFormatOfDateTime(formatString);
            if (objDateTime.HasValue && !string.IsNullOrWhiteSpace(format))
            {

                return objDateTime.Value.ToString(format);
            }
            return "";
        }

        public static string FormatDate(this Nullable<DateTime> objDateTime, FormatType formatString)
        {
            var format = GetFormatOfDateTime(formatString);
            if (objDateTime.HasValue && !string.IsNullOrWhiteSpace(format))
            {

                return objDateTime.Value.ToString(format);
            }
            return "";
        }

        public static string FormatDate(this DateTime objDateTime, FormatType formatString)
        {
            var format = GetFormatOfDateTime(formatString);
            if (!string.IsNullOrWhiteSpace(format))
                return objDateTime.ToString(format);

            return "";
        }

        public static DateTime NullableDateTimeToDateTime(this Nullable<DateTime> objDateTime)
        {
            if (objDateTime == null)
                return new DateTime();
            else
                return DateTime.Parse(objDateTime.ToString());
        }
    }
}