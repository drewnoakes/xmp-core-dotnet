// =================================================================================================
// ADOBE SYSTEMS INCORPORATED
// Copyright 2006 Adobe Systems Incorporated
// All Rights Reserved
//
// NOTICE:  Adobe permits you to use, modify, and distribute this file in accordance with the terms
// of the Adobe license agreement accompanying it.
// =================================================================================================

namespace XmpCore
{
    /// <summary>XMP Toolkit Version Information.</summary>
    /// <remarks>
    /// Version information for the XMP toolkit is available at runtime via <see cref="XmpMetaFactory.VersionInfo"/>.
    /// </remarks>
    /// <since>23.01.2006</since>
    public interface IXmpVersionInfo
    {
        /// <value>Returns the primary release number, the "1" in version "1.2.3".</value>
        int Major { get; }

        /// <value>Returns the secondary release number, the "2" in version "1.2.3".</value>
        int Minor { get; }

        /// <value>Returns the tertiary release number, the "3" in version "1.2.3".</value>
        int Micro { get; }

        /// <value>Returns a rolling build number, monotonically increasing in a release.</value>
        int Build { get; }

        /// <value>Returns true if this is a debug build.</value>
        bool IsDebug { get; }

        /// <value>Returns a comprehensive version information string.</value>
        string Message { get; }
    }
}
