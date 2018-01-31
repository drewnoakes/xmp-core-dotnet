// =================================================================================================
// ADOBE SYSTEMS INCORPORATED
// Copyright 2006 Adobe Systems Incorporated
// All Rights Reserved
//
// NOTICE:  Adobe permits you to use, modify, and distribute this file in accordance with the terms
// of the Adobe license agreement accompanying it.
// =================================================================================================


using System;

namespace XmpCore
{
    /// <summary>This exception wraps all errors that occur in the XMP Toolkit.</summary>
    /// <author>Stefan Makswit</author>
    /// <since>16.02.2006</since>
    public sealed class XmpException : Exception
    {
        /// <value>Gets the error code of the XMP toolkit.</value>
        public XmpErrorCode ErrorCode { get; }

        /// <summary>Constructs an exception with a message and an error code.</summary>
        /// <param name="message">the message</param>
        /// <param name="errorCode">the error code</param>
        public XmpException(string message, XmpErrorCode errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>Constructs an exception with a message, an error code and an inner exception.</summary>
        /// <param name="message">the error message.</param>
        /// <param name="errorCode">the error code</param>
        /// <param name="innerException">the exception source</param>
        public XmpException(string message, XmpErrorCode errorCode, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
