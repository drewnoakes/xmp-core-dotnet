// =================================================================================================
// ADOBE SYSTEMS INCORPORATED
// Copyright 2006 Adobe Systems Incorporated
// All Rights Reserved
//
// NOTICE:  Adobe permits you to use, modify, and distribute this file in accordance with the terms
// of the Adobe license agreement accompanying it.
// =================================================================================================


using System;
using System.Text;

namespace XmpCore.Impl
{
    /// <summary>Converts between ISO 8601 Strings and <c>Calendar</c> with millisecond resolution.</summary>
    /// <since>16.02.2006</since>
    public static class Iso8601Converter
    {
        /// <summary>Converts an ISO 8601 string to an <c>XMPDateTime</c>.</summary>
        /// <remarks>
        /// Converts an ISO 8601 string to an <c>XMPDateTime</c>.
        /// Parse a date according to ISO 8601 and
        /// http://www.w3.org/TR/NOTE-datetime:
        /// <list type="bullet">
        /// <item>YYYY</item>
        /// <item>YYYY-MM</item>
        /// <item>YYYY-MM-DD</item>
        /// <item>YYYY-MM-DDThh:mmTZD</item>
        /// <item>YYYY-MM-DDThh:mm:ssTZD</item>
        /// <item>YYYY-MM-DDThh:mm:ss.sTZD</item>
        /// </list>
        /// Data fields:
        /// <list type="bullet">
        /// <item>YYYY = four-digit year</item>
        /// <item>MM = two-digit month (01=January, etc.)</item>
        /// <item>DD = two-digit day of month (01 through 31)</item>
        /// <item>hh = two digits of hour (00 through 23)</item>
        /// <item>mm = two digits of minute (00 through 59)</item>
        /// <item>ss = two digits of second (00 through 59)</item>
        /// <item>s = one or more digits representing a decimal fraction of a second</item>
        /// <item>TZD = time zone designator (Z or +hh:mm or -hh:mm)</item>
        /// </list>
        /// Note that ISO 8601 does not seem to allow years less than 1000 or greater
        /// than 9999. We allow any year, even negative ones. The year is formatted
        /// as "%.4d".
        /// <para />
        /// <em>Note:</em> Tolerate missing TZD, assume is UTC. Photoshop 8 writes
        /// dates like this for exif:GPSTimeStamp.<br />
        /// <em>Note:</em> DOES NOT APPLY ANYMORE.
        /// Tolerate missing date portion, in case someone foolishly
        /// writes a time-only value that way.
        /// </remarks>
        /// <param name="iso8601String">a date string that is ISO 8601 conform.</param>
        /// <returns>Returns a <c>Calendar</c>.</returns>
        /// <exception cref="XmpException">Is thrown when the string is non-conform.</exception>
        public static IXmpDateTime Parse(string iso8601String)
        {
            return Parse(iso8601String, new XmpDateTime());
        }

        /// <param name="iso8601String">a date string that is ISO 8601 conform.</param>
        /// <param name="binValue">an existing XMPDateTime to set with the parsed date</param>
        /// <returns>Returns an XMPDateTime-object containing the ISO8601-date.</returns>
        /// <exception cref="XmpException">Is thrown when the string is non-conform.</exception>
        public static IXmpDateTime Parse(string iso8601String, IXmpDateTime binValue)
        {
            if (iso8601String == null)
            {
                throw new XmpException("Parameter must not be null", XmpErrorCode.BadParam);
            }
            if (iso8601String.Length == 0)
            {
                return binValue;
            }
            var input = new ParseState(iso8601String);
            if (input.Ch(0) == '-')
            {
                input.Skip();
            }
            // Extract the year.
            var value = input.GatherInt("Invalid year in date string", 9999);
            if (input.HasNext && input.Ch() != '-')
            {
                throw new XmpException("Invalid date string, after year", XmpErrorCode.BadValue);
            }
            if (input.Ch(0) == '-')
            {
                value = -value;
            }
            binValue.Year = value;
            if (!input.HasNext)
            {
                return binValue;
            }
            input.Skip();
            // Extract the month.
            value = input.GatherInt("Invalid month in date string", 12);
            if (input.HasNext && input.Ch() != '-')
            {
                throw new XmpException("Invalid date string, after month", XmpErrorCode.BadValue);
            }
            binValue.Month = value;
            if (!input.HasNext)
            {
                return binValue;
            }
            input.Skip();
            // Extract the day.
            value = input.GatherInt("Invalid day in date string", 31);
            if (input.HasNext && input.Ch() != 'T')
            {
                throw new XmpException("Invalid date string, after day", XmpErrorCode.BadValue);
            }
            binValue.Day = value;
            if (!input.HasNext)
            {
                return binValue;
            }
            input.Skip();
            // Extract the hour.
            value = input.GatherInt("Invalid hour in date string", 23);
            binValue.Hour = value;
            if (!input.HasNext)
            {
                return binValue;
            }
            // Extract the minute.
            if (input.Ch() == ':')
            {
                input.Skip();
                value = input.GatherInt("Invalid minute in date string", 59);
                if (input.HasNext && input.Ch() != ':' && input.Ch() != 'Z' && input.Ch() != '+' && input.Ch() != '-')
                {
                    throw new XmpException("Invalid date string, after minute", XmpErrorCode.BadValue);
                }
                binValue.Minute = value;
            }
            if (!input.HasNext)
            {
                return binValue;
            }
            if (input.HasNext && input.Ch() == ':')
            {
                input.Skip();
                value = input.GatherInt("Invalid whole seconds in date string", 59);
                if (input.HasNext && input.Ch() != '.' && input.Ch() != 'Z' && input.Ch() != '+' && input.Ch() != '-')
                {
                    throw new XmpException("Invalid date string, after whole seconds", XmpErrorCode.BadValue);
                }
                binValue.Second = value;
                if (input.Ch() == '.')
                {
                    input.Skip();
                    var digits = input.Pos();
                    value = input.GatherInt("Invalid fractional seconds in date string", 999999999);
                    if (input.HasNext && (input.Ch() != 'Z' && input.Ch() != '+' && input.Ch() != '-'))
                    {
                        throw new XmpException("Invalid date string, after fractional second", XmpErrorCode.BadValue);
                    }
                    digits = input.Pos() - digits;
                    for (; digits > 9; --digits)
                    {
                        value = value / 10;
                    }
                    for (; digits < 9; ++digits)
                    {
                        value = value * 10;
                    }
                    binValue.Nanosecond = value;
                }
            }
            else
            {
                if (input.Ch() != 'Z' && input.Ch() != '+' && input.Ch() != '-')
                {
                    throw new XmpException("Invalid date string, after time", XmpErrorCode.BadValue);
                }
            }
            var tzSign = 0;
            var tzHour = 0;
            var tzMinute = 0;
            if (!input.HasNext)
            {
                // no Timezone at all
                return binValue;
            }
            if (input.Ch() == 'Z')
            {
                input.Skip();
            }
            else
            {
                if (input.HasNext)
                {
                    if (input.Ch() == '+')
                    {
                        tzSign = 1;
                    }
                    else
                    {
                        if (input.Ch() == '-')
                        {
                            tzSign = -1;
                        }
                        else
                        {
                            throw new XmpException("Time zone must begin with 'Z', '+', or '-'", XmpErrorCode.BadValue);
                        }
                    }
                    input.Skip();
                    // Extract the time zone hour.
                    tzHour = input.GatherInt("Invalid time zone hour in date string", 23);
                    if (input.HasNext)
                    {
                        if (input.Ch() == ':')
                        {
                            input.Skip();
                            // Extract the time zone minute.
                            tzMinute = input.GatherInt("Invalid time zone minute in date string", 59);
                        }
                        else
                        {
                            throw new XmpException("Invalid date string, after time zone hour", XmpErrorCode.BadValue);
                        }
                    }
                }
            }
            // create a corresponding TZ and set it time zone
            var offset = (TimeSpan.FromHours(tzHour) + TimeSpan.FromMinutes(tzMinute));
            if (tzSign < 0)
                offset = -offset;

#if !PORTABLE
            binValue.TimeZone = TimeZoneInfo.CreateCustomTimeZone("OFFSET" + offset, offset, string.Empty, string.Empty);
#else
            binValue.TimeZone = TimeZoneInfo.Local;
            binValue.Offset = offset;
#endif

            if (input.HasNext)
                throw new XmpException("Invalid date string, extra chars at end", XmpErrorCode.BadValue);

            return binValue;
        }

        /// <summary>Converts a <c>Calendar</c> into an ISO 8601 string.</summary>
        /// <remarks>
        /// Converts a <c>Calendar</c> into an ISO 8601 string.
        /// Format a date according to ISO 8601 and http://www.w3.org/TR/NOTE-datetime:
        /// <list type="bullet">
        /// <item>YYYY</item>
        /// <item>YYYY-MM</item>
        /// <item>YYYY-MM-DD</item>
        /// <item>YYYY-MM-DDThh:mmTZD</item>
        /// <item>YYYY-MM-DDThh:mm:ssTZD</item>
        /// <item>YYYY-MM-DDThh:mm:ss.sTZD</item>
        /// </list>
        /// Data fields:
        /// <list type="bullet">
        /// <item>YYYY = four-digit year</item>
        /// <item>MM     = two-digit month (01=January, etc.)</item>
        /// <item>DD     = two-digit day of month (01 through 31)</item>
        /// <item>hh     = two digits of hour (00 through 23)</item>
        /// <item>mm     = two digits of minute (00 through 59)</item>
        /// <item>ss     = two digits of second (00 through 59)</item>
        /// <item>s     = one or more digits representing a decimal fraction of a second</item>
        /// <item>TZD     = time zone designator (Z or +hh:mm or -hh:mm)</item>
        /// </list>
        /// <para />
        /// <em>Note:</em> ISO 8601 does not seem to allow years less than 1000 or greater than 9999.
        /// We allow any year, even negative ones. The year is formatted as "%.4d".
        /// <para />
        /// <em>Note:</em> Fix for bug 1269463 (silently fix out of range values) included in parsing.
        /// The quasi-bogus "time only" values from Photoshop CS are not supported.
        /// </remarks>
        /// <param name="dateTime">an XMPDateTime-object.</param>
        /// <returns>Returns an ISO 8601 string.</returns>
        public static string Render(IXmpDateTime dateTime)
        {
            var buffer = new StringBuilder();
            if (dateTime.HasDate)
            {
                // year is rendered in any case, even 0000
                buffer.Append(dateTime.Year.ToString("0000"));
                if (dateTime.Month == 0)
                    return buffer.ToString();

                // month
                buffer.Append('-');
                buffer.Append(dateTime.Month.ToString("00"));
                if (dateTime.Day == 0)
                    return buffer.ToString();

                // day
                buffer.Append('-');
                buffer.Append(dateTime.Day.ToString("00"));

                // time, rendered if any time field is not zero
                if (dateTime.HasTime)
                {
                    // hours and minutes
                    buffer.Append('T');
                    buffer.Append(dateTime.Hour.ToString("00"));
                    buffer.Append(':');
                    buffer.Append(dateTime.Minute.ToString("00"));
                    // seconds and nanoseconds
                    if (dateTime.Second != 0 || dateTime.Nanosecond != 0)
                    {
                        buffer.Append(':');
                        var seconds = dateTime.Second + dateTime.Nanosecond / 1e9d;
                        buffer.AppendFormat("{0:00.#########}", seconds);
                    }
                    // time zone
                    if (dateTime.HasTimeZone)
                    {
                        // used to calculate the time zone offset incl. Daylight Savings
                        var timeInMillis = dateTime.Calendar.GetTimeInMillis();
                        var offset = (int) dateTime.TimeZone.GetUtcOffset(XmpDateTime.UnixTimeToDateTimeOffset(timeInMillis).DateTime).TotalMilliseconds;
                        if (offset == 0)
                        {
                            // UTC
                            buffer.Append('Z');
                        }
                        else
                        {
                            var thours = offset / 3600000;
                            var tminutes = Math.Abs(offset % 3600000 / 60000);
                            buffer.Append(thours.ToString("+00;-00"));
                            buffer.Append(tminutes.ToString(":00"));
                        }
                    }
                }
            }
            return buffer.ToString();
        }
    }

    /// <since>22.08.2006</since>
    internal sealed class ParseState
    {
        private readonly string _str;
        private int _pos;

        /// <param name="str">initializes the parser container</param>
        public ParseState(string str)
        {
            _str = str;
        }

        /// <value>Returns whether there are more chars to come.</value>
        public bool HasNext
        {
            get { return _pos < _str.Length; }
        }

        /// <param name="index">index of char</param>
        /// <returns>Returns char at a certain index.</returns>
        public char Ch(int index)
        {
            return index < _str.Length ? _str[index] : (char)0x0000;
        }

        /// <returns>Returns the current char or 0x0000 if there are no more chars.</returns>
        public char Ch()
        {
            return _pos < _str.Length ? _str[_pos] : (char)0x0000;
        }

        /// <summary>Skips the next char.</summary>
        public void Skip()
        {
            _pos++;
        }

        /// <returns>Returns the current position.</returns>
        public int Pos()
        {
            return _pos;
        }

        /// <summary>Parses a integer from the source and sets the pointer after it.</summary>
        /// <param name="errorMsg">Error message to put in the exception if no number can be found</param>
        /// <param name="maxValue">the max value of the number to return</param>
        /// <returns>Returns the parsed integer.</returns>
        /// <exception cref="XmpException">Thrown if no integer can be found.</exception>
        public int GatherInt(string errorMsg, int maxValue)
        {
            var value = 0;
            var success = false;
            var ch = Ch(_pos);
            while ('0' <= ch && ch <= '9')
            {
                value = (value * 10) + (ch - '0');
                success = true;
                _pos++;
                ch = Ch(_pos);
            }

            if (!success)
                throw new XmpException(errorMsg, XmpErrorCode.BadValue);

            return value > maxValue
                ? maxValue
                : value < 0
                    ? 0
                    : value;
        }
    }
}
