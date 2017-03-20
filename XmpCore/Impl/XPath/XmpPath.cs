// =================================================================================================
// ADOBE SYSTEMS INCORPORATED
// Copyright 2006 Adobe Systems Incorporated
// All Rights Reserved
//
// NOTICE:  Adobe permits you to use, modify, and distribute this file in accordance with the terms
// of the Adobe license agreement accompanying it.
// =================================================================================================


using System.Collections.Generic;
using System.Text;

namespace XmpCore.Impl.XPath
{
    public enum XmpPathStepType
    {
        /// <summary>Marks a struct field step, also for top level nodes (schema "fields").</summary>
        StructFieldStep = 0x01,

        /// <summary>Marks a qualifier step.</summary>
        /// <remarks>
        /// Marks a qualifier step.
        /// Note: Order is significant to separate struct/qual from array kinds!
        /// </remarks>
        QualifierStep = 0x02,

        /// <summary>Marks an array index step</summary>
        ArrayIndexStep = 0x03,

        ArrayLastStep = 0x04,

        QualSelectorStep = 0x05,

        FieldSelectorStep = 0x06,

        SchemaNode = unchecked((int)0x80000000)
    }

    /// <summary>Represents an XMP XmpPath with segment accessor methods.</summary>
    /// <since>28.02.2006</since>
    public sealed class XmpPath
    {
        public const int StepSchema = 0;
        public const int StepRootProp = 1;

        /// <summary>stores the segments of an <see cref="XmpPath"/></summary>
        private readonly List<XmpPathSegment> _segments = new List<XmpPathSegment>(5);

        // Bits for XPathStepInfo options.
        //
        /// <summary>Append a path segment</summary>
        /// <param name="segment">the segment to add</param>
        public void Add(XmpPathSegment segment)
        {
            _segments.Add(segment);
        }

        /// <param name="index">the index of the segment to return</param>
        /// <returns>Returns a path segment.</returns>
        public XmpPathSegment GetSegment(int index)
        {
            return _segments[index];
        }

        /// <returns>Returns the size of the xmp path.</returns>
        public int Size()
        {
            return _segments.Count;
        }

        /// <summary>Serializes the normalized XMP-path.</summary>
        public override string ToString()
        {
            var result = new StringBuilder();
            var index = 1;
            while (index < Size())
            {
                result.Append(GetSegment(index));
                if (index < Size() - 1)
                {
                    var kind = GetSegment(index + 1).Kind;
                    if (kind == XmpPathStepType.StructFieldStep || kind == XmpPathStepType.QualifierStep)
                    {
                        // all but last and array indices
                        result.Append('/');
                    }
                }
                index++;
            }
            return result.ToString();
        }
    }
}