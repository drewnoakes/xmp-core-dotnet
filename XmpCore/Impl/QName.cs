// =================================================================================================
// ADOBE SYSTEMS INCORPORATED
// Copyright 2006 Adobe Systems Incorporated
// All Rights Reserved
//
// NOTICE:  Adobe permits you to use, modify, and distribute this file in accordance with the terms
// of the Adobe license agreement accompanying it.
// =================================================================================================

namespace XmpCore.Impl
{
    /// <since>09.11.2006</since>
    public sealed class QName
    {
        /// <summary>Splits a qname into prefix and localname.</summary>
        /// <param name="qname">a QName</param>
        public QName(string qname)
        {
            var colon = qname.IndexOf(':');
            if (colon >= 0)
            {
                Prefix = qname.Substring(0, colon - 0);
                LocalName = qname.Substring(colon + 1);
            }
            else
            {
                Prefix = string.Empty;
                LocalName = qname;
            }
        }

        /// <summary>Constructor that initializes the fields</summary>
        /// <param name="prefix">the prefix</param>
        /// <param name="localName">the name</param>
        public QName(string prefix, string localName)
        {
            Prefix = prefix;
            LocalName = localName;
        }

        /// <value>Returns whether the QName has a prefix.</value>
        public bool HasPrefix => !string.IsNullOrEmpty(Prefix);

        /// <summary>XML localname</summary>
        /// <value>the localName</value>
        public string LocalName { get; }

        /// <summary>XML namespace prefix</summary>
        /// <value>the prefix</value>
        public string Prefix { get; }
    }
}
