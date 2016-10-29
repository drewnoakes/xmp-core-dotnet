// =================================================================================================
// ADOBE SYSTEMS INCORPORATED
// Copyright 2006 Adobe Systems Incorporated
// All Rights Reserved
//
// NOTICE:  Adobe permits you to use, modify, and distribute this file in accordance with the terms
// of the Adobe license agreement accompanying it.
// =================================================================================================


using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace XmpCore.Impl
{
    /// <summary>Utility functions for the XMPToolkit implementation.</summary>
    /// <since>06.06.2006</since>
    public static class Utils
    {
        /// <summary>segments of a UUID</summary>
        public const int UuidSegmentCount = 4;

        /// <summary>length of a UUID</summary>
        public const int UuidLength = 32 + UuidSegmentCount;

        /// <summary>table of XML name start chars (&lt;= 0xFF)</summary>
        private static readonly bool[] _xmlNameStartChars;

        /// <summary>table of XML name chars (&lt;= 0xFF)</summary>
        private static readonly bool[] _xmlNameChars;

        static Utils()
        {
            _xmlNameChars = new bool[0x0100];
            _xmlNameStartChars = new bool[0x0100];

            for (var ch = (char)0; ch < _xmlNameChars.Length; ch++)
            {
                var isNameStartChar =
                    ch == ':' ||
                    ('A' <= ch && ch <= 'Z') ||
                    ch == '_' ||
                    ('a' <= ch && ch <= 'z') ||
                    (0xC0 <= ch && ch <= 0xD6) ||
                    (0xD8 <= ch && ch <= 0xF6) ||
                    (0xF8 <= ch && ch <= 0xFF);

                _xmlNameStartChars[ch] = isNameStartChar;
                _xmlNameChars[ch] =
                    isNameStartChar ||
                    ch == '-' ||
                    ch == '.' ||
                    ('0' <= ch && ch <= '9') ||
                    ch == 0xB7;
            }
        }

        /// <summary>
        /// Normalize an xml:lang value so that comparisons are effectively case
        /// insensitive as required by RFC 3066 (which supersedes RFC 1766).
        /// </summary>
        /// <remarks>
        /// The normalization rules:
        /// <list type="bullet">
        ///   <item>The primary subtag is lower case, the suggested practice of ISO 639.</item>
        ///   <item>All 2 letter secondary subtags are upper case, the suggested practice of ISO 3166.</item>
        ///   <item>All other subtags are lower case.</item>
        /// </list>
        /// </remarks>
        /// <param name="value">raw value</param>
        /// <returns>Returns the normalized value.</returns>
        public static string NormalizeLangValue(string value)
        {
            // don't normalize x-default
            if (value == XmpConstants.XDefault)
                return value;

            var subTag = 1;
            var buffer = new StringBuilder();
            foreach (var t in value)
            {
                switch (t)
                {
                    case '-':
                    case '_':
                    {
                        // move to next subtag and convert underscore to hyphen
                        buffer.Append('-');
                        subTag++;
                        break;
                    }
                    case ' ':
                    {
                        // remove spaces
                        break;
                    }
                    default:
                    {
                        // convert second subtag to uppercase, all other to lowercase
                        buffer.Append(subTag != 2 ? char.ToLower(t) : char.ToUpper(t));
                        break;
                    }
                }
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Split the name and value parts for field and qualifier selectors.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        ///   <item><c>[qualName="value"]</c> - An element in an array of structs, chosen by a field value.</item>
        ///   <item><c>[?qualName="value"]</c> - An element in an array, chosen by a qualifier value.</item>
        /// </list>
        /// The value portion is a string quoted by <c>'</c> or <c>"</c>. The value may contain
        /// any character including a doubled quoting character. The value may be
        /// empty.
        /// <para />
        /// <em>Note:</em> It is assumed that the expression is formal correct.
        /// </remarks>
        /// <param name="selector">The selector</param>
        /// <param name="name">The name string</param>
        /// <param name="value">The value string</param>
        internal static void SplitNameAndValue(string selector, out string name, out string value)
        {
            // get the name
            var eq = selector.IndexOf('=');
            var pos = 1;
            if (selector[pos] == '?')
                pos++;
            name = selector.Substring (pos, eq - pos);

            // get the value
            pos = eq + 1;
            var quote = selector[pos];
            pos++;
            var end = selector.Length - 2;

            // quote and ]
            var valueStr = new StringBuilder(end - eq);
            while (pos < end)
            {
                valueStr.Append(selector[pos]);
                pos++;
                if (selector[pos] == quote)
                {
                    // skip one quote in value
                    pos++;
                }
            }

            value = valueStr.ToString();
        }

        /// <param name="schema">a schema namespace</param>
        /// <param name="prop">an XMP Property</param>
        /// <returns>
        /// Returns true if the property is defined as &quot;Internal
        /// Property&quot;, see XMP Specification.
        /// </returns>
        internal static bool IsInternalProperty(string schema, string prop)
        {
            switch (schema)
            {
                case XmpConstants.NsDC:
                    return prop == "dc:format" || prop == "dc:language";
                case XmpConstants.NsXmp:
                    return prop == "xmp:BaseURL" || prop == "xmp:CreatorTool" || prop == "xmp:Format" || prop == "xmp:Locale" || prop == "xmp:MetadataDate" || "xmp:ModifyDate" == prop;
                case XmpConstants.NsPdf:
                    return prop == "pdf:BaseURL" || prop == "pdf:Creator" || prop == "pdf:ModDate" || prop == "pdf:PDFVersion" || prop == "pdf:Producer";
                case XmpConstants.NsTiff:
                    return prop != "tiff:ImageDescription" && prop != "tiff:Artist" && prop != "tiff:Copyright";
                case XmpConstants.NsExif:
                    return prop != "exif:UserComment";
                case XmpConstants.NsPhotoshop:
                    return prop == "photoshop:ICCProfile";
                case XmpConstants.NsCameraraw:
                    return prop == "crs:Version" || prop == "crs:RawFileName" || prop == "crs:ToneCurveName";
                case XmpConstants.NsExifAux:
                case XmpConstants.NsAdobestockphoto:
                case XmpConstants.NsXmpMm:
                case XmpConstants.TypeText:
                case XmpConstants.TypePagedfile:
                case XmpConstants.TypeGraphics:
                case XmpConstants.TypeImage:
                case XmpConstants.TypeFont:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Check some requirements for an UUID:
        /// <list type="bullet">
        ///   <item>Length of the UUID is 32</item>
        ///   <item>The Delimiter count is 4 and all the 4 delimiter are on their right position (8,13,18,23)</item>
        /// </list>
        /// </summary>
        /// <param name="uuid">uuid to test</param>
        /// <returns>true - this is a well formed UUID, false - UUID has not the expected format</returns>
        internal static bool CheckUuidFormat(string uuid)
        {
            if (uuid == null)
                return false;

            var result = true;
            var delimCnt = 0;
            int delimPos;
            for (delimPos = 0; delimPos < uuid.Length; delimPos++)
            {
                if (uuid[delimPos] == '-')
                {
                    delimCnt++;
                    result = result && (delimPos == 8 || delimPos == 13 || delimPos == 18 || delimPos == 23);
                }
            }

            return result && UuidSegmentCount == delimCnt && UuidLength == delimPos;
        }

        /// <summary>Simple check for valid XML names.</summary>
        /// <remarks>
        /// Within ASCII range
        /// <para />
        /// ":" | [A-Z] | "_" | [a-z] | [#xC0-#xD6] | [#xD8-#xF6]
        /// <para />
        /// are accepted, above all characters (which is not entirely
        /// correct according to the XML Spec.
        /// </remarks>
        /// <param name="name">an XML Name</param>
        /// <returns>Return <c>true</c> if the name is correct.</returns>
        [Pure]
        public static bool IsXmlName(string name)
        {
            if (name.Length > 0 && !IsNameStartChar(name[0]))
                return false;

            for (var i = 1; i < name.Length; i++)
            {
                if (!IsNameChar(name[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the value is a legal "unqualified" XML name, as
        /// defined in the XML Namespaces proposed recommendation.
        /// </summary>
        /// <remarks>
        /// These are XML names, except that they must not contain a colon.
        /// </remarks>
        /// <param name="name">the value to check</param>
        /// <returns>Returns true if the name is a valid "unqualified" XML name.</returns>
        public static bool IsXmlNameNs(string name)
        {
            if (name.Length > 0 && (!IsNameStartChar(name[0]) || name[0] == ':'))
                return false;

            for (var i = 1; i < name.Length; i++)
            {
                if (!IsNameChar(name[i]) || name[i] == ':')
                    return false;
            }

            return true;
        }

        /// <param name="c">a char</param>
        /// <returns>Returns true if the char is an ASCII control char.</returns>
        internal static bool IsControlChar(char c)
        {
            return (c <= 0x1F || c == 0x7F) && c != 0x09 && c != 0x0A && c != 0x0D;
        }

        /// <summary>Serializes the node value in XML encoding.</summary>
        /// <remarks>
        /// Its used for tag bodies and attributes.
        /// <para />
        /// <em>Note:</em> The attribute is always limited by quotes,
        /// thats why <c>&amp;apos;</c> is never serialized.
        /// <para />
        /// <em>Note:</em> Control chars are written unescaped, but if the user uses others than tab, LF
        /// and CR the resulting XML will become invalid.
        /// </remarks>
        /// <param name="value">a string</param>
        /// <param name="forAttribute">flag if string is attribute value (need to additional escape quotes)</param>
        /// <param name="escapeWhitespaces">Decides if LF, CR and TAB are escaped.</param>
        /// <returns>Returns the value ready for XML output.</returns>
        public static string EscapeXml(string value, bool forAttribute, bool escapeWhitespaces)
        {
            // quick check if character are contained that need special treatment
            var needsEscaping = value.ToCharArray()
                .Any(c => c == '<' || c == '>' || c == '&' || (escapeWhitespaces && (c == '\t' || c == '\n' || c == '\r')) || (forAttribute && c == '"'));

            if (!needsEscaping)
            {
                // fast path
                return value;
            }

            // slow path with escaping
            var buffer = new StringBuilder(value.Length * 4 / 3);
            foreach (var c in value)
            {
                if (!(escapeWhitespaces && (c == '\t' || c == '\n' || c == '\r')))
                {
                    switch (c)
                    {
                        case '<':
                        {
                            // we do what "Canonical XML" expects
                            // AUDIT: &apos; not serialized as only outer qoutes are used
                            buffer.Append("&lt;");
                            continue;
                        }
                        case '>':
                        {
                            buffer.Append("&gt;");
                            continue;
                        }
                        case '&':
                        {
                            buffer.Append("&amp;");
                            continue;
                        }
                        case '"':
                        {
                            buffer.Append(forAttribute ? "&quot;" : "\"");
                            continue;
                        }
                        default:
                        {
                            buffer.Append(c);
                            continue;
                        }
                    }
                }

                // write control chars escaped,
                // if there are others than tab, LF and CR the xml will become invalid.
                buffer.AppendFormat("&#x{0:X};", (int)c);
            }

            return buffer.ToString();
        }

        /// <summary>Replaces the ASCII control chars with a space.</summary>
        /// <param name="value">a node value</param>
        /// <returns>Returns the cleaned up value</returns>
        internal static string RemoveControlChars(string value)
        {
            var buffer = new StringBuilder(value);

            for (var i = 0; i < buffer.Length; i++)
            {
                if (IsControlChar(buffer[i]))
                    buffer[i] = ' ';
            }

            return buffer.ToString();
        }

        /// <summary>Simple check if a character is a valid XML start name char.</summary>
        /// <remarks>
        /// Simple check if a character is a valid XML start name char.
        /// All characters according to the XML Spec 1.1 are accepted:
        /// http://www.w3.org/TR/xml11/#NT-NameStartChar
        /// </remarks>
        /// <param name="ch">a character</param>
        /// <returns>Returns true if the character is a valid first char of an XML name.</returns>
        private static bool IsNameStartChar(char ch)
        {
            return
                (ch <= 0xFF && _xmlNameStartChars[ch]) ||
                (ch >= 0x100 && ch <= 0x2FF) ||
                (ch >= 0x370 && ch <= 0x37D) ||
                (ch >= 0x37F && ch <= 0x1FFF) ||
                (ch >= 0x200C && ch <= 0x200D) ||
                (ch >= 0x2070 && ch <= 0x218F) ||
                (ch >= 0x2C00 && ch <= 0x2FEF) ||
                (ch >= 0x3001 && ch <= 0xD7FF) ||
                (ch >= 0xF900 && ch <= 0xFDCF) ||
                (ch >= 0xFDF0 && ch <= 0xFFFD);
        }

        /// <summary>
        /// Simple check if a character is a valid XML name char
        /// (every char except the first one), according to the XML Spec 1.1:
        /// http://www.w3.org/TR/xml11/#NT-NameChar
        /// </summary>
        /// <param name="ch">a character</param>
        /// <returns>Returns true if the character is a valid char of an XML name.</returns>
        private static bool IsNameChar(char ch)
        {
            return
                (ch <= 0xFF && _xmlNameChars[ch]) ||
                IsNameStartChar(ch) ||
                (ch >= 0x300 && ch <= 0x36F) ||
                (ch >= 0x203F && ch <= 0x2040);
        }
    }
}
