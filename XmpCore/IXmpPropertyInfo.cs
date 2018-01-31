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
    /// <summary>Models a property together with its path and namespace.</summary>
    /// <remarks>Instances of this type are are iterated via <see cref="IXmpIterator"/>.</remarks>
    /// <author>Stefan Makswit</author>
    /// <since>06.07.2006</since>
    public interface IXmpPropertyInfo : IXmpProperty
    {
        /// <value>Returns the namespace of the property</value>
        string Namespace { get; }

        /// <value>Returns the path of the property, but only if returned by the iterator.</value>
        string Path { get; }
    }
}
