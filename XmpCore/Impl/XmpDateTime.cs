// =================================================================================================
// ADOBE SYSTEMS INCORPORATED
// Copyright 2006 Adobe Systems Incorporated
// All Rights Reserved
//
// NOTICE:  Adobe permits you to use, modify, and distribute this file in accordance with the terms
// of the Adobe license agreement accompanying it.
// =================================================================================================


using System;
using System.Globalization;
using JetBrains.Annotations;
using Sharpen;
using Calendar = Sharpen.Calendar;
using GregorianCalendar = Sharpen.GregorianCalendar;

namespace XmpCore.Impl
{
    /// <summary>The default implementation of <see cref="IXmpDateTime"/>.</summary>
    /// <since>16.02.2006</since>
    public sealed class XmpDateTime : IXmpDateTime
    {
        private int _year;
        private int _month;
        private int _day;
        private int _hour;
        private int _minute;
        private int _second;

        /// <summary>Use NO time zone as default</summary>
        private TimeZoneInfo _timeZone;

        /// <summary>The nano seconds take micro and nano seconds, while the milli seconds are in the calendar.</summary>
        private int _nanoseconds;

        /// <summary>
        /// Creates an <c>XMPDateTime</c>-instance with the current time in the default time zone.
        /// </summary>
        public XmpDateTime()
        {
        }

        /// <summary>Creates an <c>XMPDateTime</c>-instance from a calendar.</summary>
        /// <param name="calendar">a <c>Calendar</c></param>
        public XmpDateTime(Calendar calendar)
        {
            // extract the date and timezone from the calendar provided
            var date = calendar.GetTime();
            var zone = calendar.GetTimeZone();
            // put that date into a calendar the pretty much represents ISO8601
            // I use US because it is close to the "locale" for the ISO8601 spec
            var intCalendar = (GregorianCalendar)Calendar.GetInstance(CultureInfo.InvariantCulture);
            intCalendar.SetGregorianChange(UnixTimeToDateTime(long.MinValue));
            intCalendar.SetTimeZone(zone);
            intCalendar.SetTime(date);
            _year = intCalendar.Get(CalendarEnum.Year);
            _month = intCalendar.Get(CalendarEnum.Month) + 1;
            // cal is from 0..12
            _day = intCalendar.Get(CalendarEnum.DayOfMonth);
            _hour = intCalendar.Get(CalendarEnum.HourOfDay);
            _minute = intCalendar.Get(CalendarEnum.Minute);
            _second = intCalendar.Get(CalendarEnum.Second);
            _nanoseconds = intCalendar.Get(CalendarEnum.Millisecond) * 1000000;
            _timeZone = intCalendar.GetTimeZone();
            // object contains all date components
            HasDate = HasTime = HasTimeZone = true;
        }

        /// <summary>
        /// Creates an <c>XMPDateTime</c>-instance from
        /// a <c>Date</c> and a <c>TimeZone</c>.
        /// </summary>
        /// <param name="date">a date describing an absolute point in time</param>
        /// <param name="timeZone">a TimeZone how to interpret the date</param>
        public XmpDateTime(DateTime date, TimeZoneInfo timeZone)
        {
            var calendar = new GregorianCalendar(timeZone);
            calendar.SetTime(date);
            _year = calendar.Get(CalendarEnum.Year);
            _month = calendar.Get(CalendarEnum.Month) + 1;
            // cal is from 0..12
            _day = calendar.Get(CalendarEnum.DayOfMonth);
            _hour = calendar.Get(CalendarEnum.HourOfDay);
            _minute = calendar.Get(CalendarEnum.Minute);
            _second = calendar.Get(CalendarEnum.Second);
            _nanoseconds = calendar.Get(CalendarEnum.Millisecond) * 1000000;
            _timeZone = timeZone;
            // object contains all date components
            HasDate = HasTime = HasTimeZone = true;
        }

        /// <summary>Creates an <c>XMPDateTime</c>-instance from an ISO 8601 string.</summary>
        /// <param name="strValue">an ISO 8601 string</param>
        /// <exception cref="XmpException">If the string is a non-conform ISO 8601 string, an exception is thrown</exception>
        public XmpDateTime(string strValue)
        {
            Iso8601Converter.Parse(strValue, this);
        }

        public int Year
        {
            get { return _year; }
            set
            {
                _year = Math.Min(Math.Abs(value), 9999);
                HasDate = true;
            }
        }

        public int Month
        {
            get { return _month; }
            set
            {
                _month = value < 1
                    ? 1
                    : value > 12
                        ? 12
                        : value;
                HasDate = true;
            }
        }

        public int Day
        {
            get { return _day; }
            set
            {
                _day = value < 1
                    ? 1
                    : value > 31
                        ? 31
                        : value;
                HasDate = true;
            }
        }

        public int Hour
        {
            get { return _hour; }
            set
            {
                _hour = Math.Min(Math.Abs(value), 23);
                HasTime = true;
            }
        }

        public int Minute
        {
            get { return _minute; }
            set
            {
                _minute = Math.Min(Math.Abs(value), 59);
                HasTime = true;
            }
        }

        public int Second
        {
            get { return _second; }
            set
            {
                _second = Math.Min(Math.Abs(value), 59);
                HasTime = true;
            }
        }

        public int Nanosecond
        {
            get { return _nanoseconds; }
            set
            {
                _nanoseconds = value;
                HasTime = true;
            }
        }

        public int CompareTo(object dt)
        {
            var xmpDateTime = (IXmpDateTime)dt;
            var d = Calendar.GetTimeInMillis() - xmpDateTime.Calendar.GetTimeInMillis();
            if (d != 0)
                return Math.Sign(d);
            // if millis are equal, compare nanoseconds
            d = _nanoseconds - xmpDateTime.Nanosecond;
            return Math.Sign(d);
        }

        public TimeZoneInfo TimeZone
        {
            get { return _timeZone; }
            set
            {
                _timeZone = value;
                HasTime = true;
                HasTimeZone = true;
            }
        }

        public bool HasDate { get; private set; }

        public bool HasTime { get; private set; }

        public bool HasTimeZone { get; private set; }

        public Calendar Calendar
        {
            get
            {
                var calendar = (GregorianCalendar)Calendar.GetInstance(CultureInfo.InvariantCulture);
                calendar.SetGregorianChange(UnixTimeToDateTime(long.MinValue));
                if (HasTimeZone)
                    calendar.SetTimeZone(_timeZone);
                calendar.Set(CalendarEnum.Year, _year);
                calendar.Set(CalendarEnum.Month, _month - 1);
                calendar.Set(CalendarEnum.DayOfMonth, _day);
                calendar.Set(CalendarEnum.HourOfDay, _hour);
                calendar.Set(CalendarEnum.Minute, _minute);
                calendar.Set(CalendarEnum.Second, _second);
                calendar.Set(CalendarEnum.Millisecond, _nanoseconds/1000000);
                return calendar;
            }
        }

        public string ToIso8601String()
        {
            return Iso8601Converter.Render(this);
        }

        /// <returns>Returns the ISO string representation.</returns>
        public override string ToString()
        {
            return ToIso8601String();
        }

        #region Conversions

        private static readonly DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <param name="unixTime">Number of milliseconds since the Unix epoch (1970-01-01 00:00:00).</param>
        [Pure]
        internal static DateTime UnixTimeToDateTime(long unixTime)
        {
            return new DateTime(_unixEpoch.Ticks + unixTime*10000);
        }

        /// <param name="unixTime">Number of milliseconds since the Unix epoch (1970-01-01 00:00:00).</param>
        [Pure]
        public static DateTimeOffset UnixTimeToDateTimeOffset(long unixTime)
        {
            return new DateTimeOffset(
                _unixEpoch.Ticks + (unixTime*10000),
                TimeSpan.Zero);
        }

        #endregion
    }
}
