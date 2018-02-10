// =================================================================================================
// ADOBE SYSTEMS INCORPORATED
// Copyright 2006 Adobe Systems Incorporated
// All Rights Reserved
//
// NOTICE:  Adobe permits you to use, modify, and distribute this file in accordance with the terms
// of the Adobe license agreement accompanying it.
// =================================================================================================


using System.Collections.Generic;

namespace XmpCore.Options
{
    /// <summary>
    /// Options for <see cref="XmpMetaFactory.Parse(System.IO.Stream, ParseOptions)"/>.
    /// </summary>
    /// <author>Stefan Makswit</author>
    /// <since>24.01.2006</since>
    public sealed class ParseOptions : Options
    {
        /// <summary>Require a surrounding &quot;x:xmpmeta&quot; element in the xml-document.</summary>
        private const int RequireXmpMetaFlag = 0x0001;

        /// <summary>Do not reconcile alias differences, throw an exception instead.</summary>
        private const int StrictAliasingFlag = 0x0004;

        /// <summary>Convert ASCII control characters 0x01 - 0x1F (except tab, cr, and lf) to spaces.</summary>
        private const int FixControlCharsFlag = 0x0008;

        /// <summary>If the input is not unicode, try to parse it as ISO-8859-1.</summary>
        private const int AcceptLatin1Flag = 0x0010;

        /// <summary>Do not carry run the XMPNormalizer on a packet, leave it as it is.</summary>
        private const int OmitNormalizationFlag = 0x0020;

        /// <summary>Disallow DOCTYPE declarations to prevent entity expansion attacks.</summary>
        public const int DisallowDoctypeFlag = 0x0040;

        /// <summary>Map of nodes whose children are to be limited.</summary>
        private Dictionary<string, int> mXMPNodesToLimit = new Dictionary<string, int>();

        /// <summary>Sets the options to the default values.</summary>
        public ParseOptions()
        {
            SetOption(FixControlCharsFlag | AcceptLatin1Flag | DisallowDoctypeFlag, true);
        }

        public bool RequireXmpMeta
        {
            get => GetOption(RequireXmpMetaFlag);
            set => SetOption(RequireXmpMetaFlag, value);
        }

        public bool StrictAliasing
        {
            get => GetOption(StrictAliasingFlag);
            set => SetOption(StrictAliasingFlag, value);
        }

        public bool FixControlChars
        {
            get => GetOption(FixControlCharsFlag);
            set => SetOption(FixControlCharsFlag, value);
        }

        public bool AcceptLatin1
        {
            get => GetOption(AcceptLatin1Flag);
            set => SetOption(AcceptLatin1Flag, value);
        }

        public bool OmitNormalization
        {
            get => GetOption(OmitNormalizationFlag);
            set => SetOption(OmitNormalizationFlag, value);
        }

        public bool DisallowDoctype
        {
            get => GetOption(DisallowDoctypeFlag);
            set => SetOption(DisallowDoctypeFlag, value);
        }

        /// <summary>Returns true if some XMP nodes have been limited.</summary>
        public bool AreXMPNodesLimited => mXMPNodesToLimit.Count > 0;

        /// <param name="nodeMap">the Map with name of nodes and number-of-items to limit them to</param>
        /// <summary>Returns the instance to call more set-methods.</summary>
        public ParseOptions SetXMPNodesToLimit(Dictionary<string, int> nodeMap)
        {
            //mXMPNodesToLimit.putAll(nodeMap);
            foreach (var node in nodeMap)
                mXMPNodesToLimit[node.Key] = node.Value;

            return this;
        }

        /// <summary>Returns map containing names oF XMP nodes to limit and number-of-items limit corresponding to the XMP nodes.</summary>
        public Dictionary<string, int> GetXMPNodesToLimit() => new Dictionary<string, int>(mXMPNodesToLimit);

        protected override string DefineOptionName(int option)
        {
            switch (option)
            {
                case RequireXmpMetaFlag:
                    return "REQUIRE_XMP_META";
                case StrictAliasingFlag:
                    return "STRICT_ALIASING";
                case FixControlCharsFlag:
                    return "FIX_CONTROL_CHARS";
                case AcceptLatin1Flag:
                    return "ACCEPT_LATIN_1";
                case OmitNormalizationFlag:
                    return "OMIT_NORMALIZATION";
                case DisallowDoctypeFlag:
                    return "DISALLOW_DOCTYPE";
                default:
                    return null;
            }
        }

        protected override int GetValidOptions() => RequireXmpMetaFlag | StrictAliasingFlag | FixControlCharsFlag | AcceptLatin1Flag | OmitNormalizationFlag | DisallowDoctypeFlag;
    }
}
