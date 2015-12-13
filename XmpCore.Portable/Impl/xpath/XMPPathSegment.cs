// =================================================================================================
// ADOBE SYSTEMS INCORPORATED
// Copyright 2006 Adobe Systems Incorporated
// All Rights Reserved
//
// NOTICE:  Adobe permits you to use, modify, and distribute this file in accordance with the terms
// of the Adobe license agreement accompanying it.
// =================================================================================================

namespace XmpCore.Impl.XPath
{
    /// <summary>A segment of a parsed <c>XMPPath</c>.</summary>
    /// <since>23.06.2006</since>
    public sealed class XmpPathSegment
    {
        /// <summary>Constructor with initial values.</summary>
        /// <param name="name">the name of the segment</param>
        public XmpPathSegment(string name)
        {
            Name = name;
        }

        /// <summary>Constructor with initial values.</summary>
        /// <param name="name">the name of the segment</param>
        /// <param name="kind">the kind of the segment</param>
        public XmpPathSegment(string name, int kind)
        {
            Name = name;
            Kind = kind;
        }

        /// <value>Get and set the kind of the path segment.</value>
        public int Kind { get; set; }

        /// <value>Get and set the name of the path segment.</value>
        public string Name { get; set; }

        /// <value>Get and set whether the segment is an alias.</value>
        public bool IsAlias { get; set; }

        /// <value>Get and set the alias form, if this segment has been created by an alias.</value>
        public int AliasForm { get; set; }

        public override string ToString()
        {
            switch (Kind)
            {
                case XmpPath.StructFieldStep:
                case XmpPath.ArrayIndexStep:
                case XmpPath.QualifierStep:
                case XmpPath.ArrayLastStep:
                    return Name;
                case XmpPath.QualSelectorStep:
                case XmpPath.FieldSelectorStep:
                    return Name;
                default:
                    // no defined step
                    return Name;
            }
        }
    }
}
