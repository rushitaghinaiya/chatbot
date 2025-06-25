using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VRMDBCommon2023
{
    public static partial class StringUtilities
    {
        private static Random random = new Random();
        [DebuggerStepThrough]
        public static string ReturnString(this object obj)
        {
            if (obj == null || Convert.IsDBNull(obj))
                return "";
            else
                return obj.ToString().Trim();

        }

        [System.Diagnostics.DebuggerStepThrough]
        public static bool ToBool(this string objString)
        {
            if (string.IsNullOrEmpty(objString))
                return false;

            if ((objString.Contains("1")) || (objString.Trim().ToUpper().Contains("TRUE")))
                return true;
            else
                return false;
        }

        public static string RandomString(int length)
        {

            const string chars = "0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// This extension method allows you to get the display name of any Enum.
        /// </summary>
        /// <param name="value">Enum object</param>
        /// <remarks>Ref: https://forums.asp.net/t/2085611.aspx?Enum+and+Display+Name+</remarks>
        /// <returns></returns>
        public static string DisplayName(this Enum value)
        {
            Type enumType = value.GetType();
            var enumValue = Enum.GetName(enumType, value);
            MemberInfo member = enumType.GetMember(enumValue)[0];

            var attrs = member.GetCustomAttributes(typeof(DisplayAttribute), false);
            var outString = ((DisplayAttribute)attrs[0]).Name;

            if (((DisplayAttribute)attrs[0]).ResourceType != null)
            {
                outString = ((DisplayAttribute)attrs[0]).GetName();
            }

            return outString;
        }

        [System.Diagnostics.DebuggerStepThrough]
        public static int ToInt(this string objString)
        {
            int number = 0;

            if (objString != null)
            {
                int.TryParse(objString.ToString(), out number);
            }
            return number;
        }

        [System.Diagnostics.DebuggerStepThrough]
        public static decimal ToDecimal(this string objString)
        {
            decimal number = 0;

            if (objString != null)
            {
                decimal.TryParse(objString.ToString(), out number);
            }
            return number;
        }
        [System.Diagnostics.DebuggerStepThrough]
        public static Nullable<DateTime> ToDateTime(this string objString)
        {
            DateTime? retDate = new Nullable<DateTime>();
            if (objString.Trim() != string.Empty)
            {
                try
                {
                    DateTime _date = new DateTime();
                    if (DateTime.TryParse(objString, System.Threading.Thread.CurrentThread.CurrentCulture, DateTimeStyles.None, out _date))
                    {
                        retDate = _date;
                    }
                }
                catch (Exception)
                {
                    //
                }
            }
            return retDate;
        }

        [System.Diagnostics.DebuggerStepThrough]
        public static string RemoveSpecialCharacters(this string objString)
        {
            // Regular expression pattern to match special characters
            string pattern = @"[^a-zA-Z0-9\s]";

            // Remove special characters using regex substitution
            string cleanedString = Regex.Replace(objString, pattern, "");

            return cleanedString;
        }

        [System.Diagnostics.DebuggerStepThrough]
        public static Double ToDouble(this string objString)
        {
            Double number = 0;

            if (objString != null)
            {
                if (!objString.Contains("."))
                {
                    objString += ".0";
                }
                Double.TryParse(objString.ToString(), out number);
            }
            return number;
        }
    }
}
