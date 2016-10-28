// =================================================================================================
// ADOBE SYSTEMS INCORPORATED
// Copyright 2006 Adobe Systems Incorporated
// All Rights Reserved
//
// NOTICE:  Adobe permits you to use, modify, and distribute this file in accordance with the terms
// of the Adobe license agreement accompanying it.
// =================================================================================================


using System;
using Sharpen;

namespace XmpCore
{
    /// <summary>
    /// The <c>XMPDateTime</c>-class represents a point in time up to a resolution of nano seconds.
    /// </summary>
    /// <remarks>
    /// Dates and time in the serialized XMP are ISO 8601 strings. There are utility functions
    /// to convert to the ISO format, a <c>Calendar</c> or get the Timezone. The fields of
    /// <c>XMPDateTime</c> are:
    /// <list type="bullet">
    /// <item>month - The month in the range 1..12.</item>
    /// <item>day - The day of the month in the range 1..31.</item>
    /// <item>minute - The minute in the range 0..59.</item>
    /// <item>hour - The time zone hour in the range 0..23.</item>
    /// <item>minute - The time zone minute in the range 0..59.</item>
    /// <item>nanosecond - The nano seconds within a second. <em>Note:</em> if the XMPDateTime is
    /// converted into a calendar, the resolution is reduced to milli seconds.</item>
    /// <item>timeZone - a <c>TimeZone</c>-object.</item>
    /// </list>
    /// DateTime values are occasionally used in cases with only a date or only a time component. A date
    /// without a time has zeros for all the time fields. A time without a date has zeros for all date
    /// fields (year, month, and day).
    /// </remarks>
    public interface IXmpDateTime : IComparable
    {
        /// <value>Get and set the year value. Can be negative.</value>
        int Year { get; set; }

        /// <value>Get and set the month, within range 1..12.</value>
        int Month { get; set; }

        /// <value>Get and set the day of the month, within range 1..31.</value>
        int Day { get; set; }

        /// <value>Returns hour - The hour in the range 0..23.</value>
        int Hour { get; set; }

        /// <value>Get and set the minute, within range 0..59.</value>
        int Minute { get; set; }

        /// <value>Get and set the second, within range 0..59.</value>
        int Second { get; set; }

        /// <value>Get and set the sub-second period, in nanoseconds.</value>
        int Nanosecond { get; set; }

        /// <value>Get and set the time zone.</value>
        TimeZoneInfo TimeZone { get; set; }

        /// <value>Get and set the offset, primarily for ISO8601 converter.</value>
        TimeSpan Offset { get; set; }

        /// <summary>This flag is set either by parsing or by setting year, month or day.</summary>
        /// <value>Returns true if the XMPDateTime object has a date portion.</value>
        bool HasDate { get; }

        /// <summary>This flag is set either by parsing or by setting hours, minutes, seconds or milliseconds.</summary>
        /// <value>Returns true if the XMPDateTime object has a time portion.</value>
        bool HasTime { get; }

        /// <summary>This flag is set either by parsing or by setting hours, minutes, seconds or milliseconds.</summary>
        /// <value>Returns true if the XMPDateTime object has a defined timezone.</value>
        bool HasTimeZone { get; }

        /// <value>
        /// Returns a <c>Calendar</c> (only with milli second precision). <br />
        /// <em>Note:</em> the dates before Oct 15th 1585 (which normally fall into validity of
        /// the Julian calendar) are also rendered internally as Gregorian dates.
        /// </value>
        Calendar Calendar { get; }

        /// <returns>Returns the ISO 8601 string representation of the date and time.</returns>
        string ToIso8601String();
    }
}
