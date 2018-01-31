// =================================================================================================
// ADOBE SYSTEMS INCORPORATED
// Copyright 2006 Adobe Systems Incorporated
// All Rights Reserved
//
// NOTICE:  Adobe permits you to use, modify, and distribute this file in accordance with the terms
// of the Adobe license agreement accompanying it.
// =================================================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using XmpCore.Options;

namespace XmpCore.Impl
{
    /// <summary>
    /// This class replaces the <c>ExpatAdapter.cpp</c> and does the
    /// XML-parsing and fixes the prefix.
    /// </summary>
    /// <remarks>
    /// After the parsing several normalisations are applied to the XMP tree.
    /// </remarks>
    /// <since>01.02.2006</since>
    public static class XmpMetaParser
    {
        private static readonly object XmpRdf = new object();

        /// <summary>
        /// Parses an XMP metadata object from a stream, including de-aliasing and normalisation.
        /// </summary>
        /// <exception cref="XmpException">Thrown if parsing or normalisation fails.</exception>
        public static IXmpMeta Parse(Stream stream, ParseOptions options = null)
        {
            ParameterAsserts.AssertNotNull(stream);
            options = options ?? new ParseOptions();
            var doc = ParseXmlFromInputStream(stream, options);
            return ParseXmlDoc(doc, options);
        }

        /// <summary>
        /// Parses an XMP metadata object from a stream, including de-aliasing and normalisation.
        /// </summary>
        /// <exception cref="XmpException">Thrown if parsing or normalisation fails.</exception>
        public static IXmpMeta Parse(byte[] bytes, ParseOptions options = null)
        {
            ParameterAsserts.AssertNotNull(bytes);
            options = options ?? new ParseOptions();
            var doc = ParseXmlFromByteBuffer(new ByteBuffer(bytes), options);
            return ParseXmlDoc(doc, options);
        }

        /// <summary>
        /// Parses an XMP metadata object from a stream, including de-aliasing and normalisation.
        /// </summary>
        /// <exception cref="XmpException">Thrown if parsing or normalisation fails.</exception>
        public static IXmpMeta Parse(ByteBuffer byteBuffer, ParseOptions options = null)
        {
            ParameterAsserts.AssertNotNull(byteBuffer);
            options = options ?? new ParseOptions();
            var doc = ParseXmlFromByteBuffer(byteBuffer, options);
            return ParseXmlDoc(doc, options);
        }

        /// <summary>
        /// Parses an XMP metadata object from a stream, including de-aliasing and normalisation.
        /// </summary>
        /// <exception cref="XmpException">Thrown if parsing or normalisation fails.</exception>
        public static IXmpMeta Parse(string xmlStr, ParseOptions options = null)
        {
            ParameterAsserts.AssertNotNullOrEmpty(xmlStr);
            options = options ?? new ParseOptions();
            var doc = ParseXmlString(xmlStr, options);
            return ParseXmlDoc(doc, options);
        }

        /// <summary>
        /// Parses an XMP metadata object from a XDocument, including de-aliasing and normalisation.
        /// </summary>
        /// <exception cref="XmpException">Thrown if parsing or normalisation fails.</exception>
        public static IXmpMeta Parse(XDocument doc, ParseOptions options = null)
        {
            ParameterAsserts.AssertNotNull(doc);
            options = options ?? new ParseOptions();
            return ParseXmlDoc(doc, options);
        }

        /// <summary>
        /// Parses XML from a byte buffer,
        /// fixing the encoding (Latin-1 to UTF-8) and illegal control character optionally.
        /// </summary>
        /// <param name="bytes">a byte buffer containing the XMP packet</param>
        /// <param name="options">the parsing options</param>
        /// <returns>Returns an XML DOM-Document.</returns>
        /// <exception cref="XmpException">Thrown when the parsing fails.</exception>
        public static XDocument Extract(byte[] bytes, ParseOptions options = null)
        {
            ParameterAsserts.AssertNotNull(bytes);
            options = options ?? new ParseOptions();
            return ParseXmlFromByteBuffer(new ByteBuffer(bytes), options);
        }

        private static IXmpMeta ParseXmlDoc(XDocument document, ParseOptions options)
        {
            // sort node Attributes to match Java DocumentBuilder (attribute order isn't supposed to matter but DocumentBuilder does some sorting)
            // namespace declarations come first, and then all are sorted in prefix:localname order
            foreach (var node in document.Descendants().Where(d => d.Attributes().Count() > 1))
            {
                var orderedattribs = node.Attributes()
                    .OrderBy(n => !n.IsNamespaceDeclaration)
                    .ThenBy(t => node.GetPrefixOfNamespace(t.Name.Namespace), StringComparer.Ordinal)
                    .ThenBy(s => s.Name.LocalName, StringComparer.Ordinal);
                node.ReplaceAttributes(orderedattribs);
            }

            var result = FindRootNode(document.Nodes(), options.RequireXmpMeta, new object[3]);

            if (result == null || result[1] != XmpRdf)
                // no appropriate root node found, return empty metadata object
                return new XmpMeta();

            var xmp = ParseRdf.Parse((XElement)result[0]);
            xmp.SetPacketHeader((string)result[2]);

            // Check if the XMP object shall be normalized
            return !options.OmitNormalization
                ? XmpNormalizer.Process(xmp, options)
                : xmp;
        }

        /// <summary>
        /// Parses XML from an <see cref="Stream"/>,
        /// fixing the encoding (Latin-1 to UTF-8) and illegal control character optionally.
        /// </summary>
        /// <param name="stream">an <c>Stream</c></param>
        /// <param name="options">the parsing options</param>
        /// <returns>Returns an XML DOM-Document.</returns>
        /// <exception cref="XmpException">Thrown when the parsing fails.</exception>
        private static XDocument ParseXmlFromInputStream(Stream stream, ParseOptions options)
        {
            if (!options.AcceptLatin1 && !options.FixControlChars)
                return ParseStream(stream, options);

            try
            {
                // load stream into bytebuffer
                return ParseXmlFromByteBuffer(new ByteBuffer(stream), options);
            }
            catch (IOException e)
            {
                throw new XmpException("Error reading the XML-file", XmpErrorCode.BadStream, e);
            }
        }

        /// <summary>
        /// Parses XML from a byte buffer,
        /// fixing the encoding (Latin-1 to UTF-8) and illegal control character optionally.
        /// </summary>
        /// <param name="buffer">a byte buffer containing the XMP packet</param>
        /// <param name="options">the parsing options</param>
        /// <returns>Returns an XML DOM-Document.</returns>
        /// <exception cref="XmpException">Thrown when the parsing fails.</exception>
        private static XDocument ParseXmlFromByteBuffer(ByteBuffer buffer, ParseOptions options)
        {
            try
            {
                return ParseStream(buffer.GetByteStream(), options);
            }
            catch (XmpException e)
            {
                if (e.ErrorCode == XmpErrorCode.BadXml || e.ErrorCode == XmpErrorCode.BadStream)
                {
                    if (options.AcceptLatin1)
                        buffer = Latin1Converter.Convert(buffer);

                    if (options.FixControlChars)
                    {
                        try
                        {
                            return ParseTextReader(new FixAsciiControlsReader(new StreamReader(buffer.GetByteStream(), buffer.GetEncoding())), options);
                        }
                        catch
                        {
                            // can normally not happen as the encoding is provided by a util function
                            throw new XmpException("Unsupported Encoding", XmpErrorCode.InternalFailure, e);
                        }
                    }

                    return ParseStream(buffer.GetByteStream(), options);
                }

                throw;
            }
        }

        /// <summary>
        /// Parses XML from a <see cref="string"/>, fixing the illegal control character optionally.
        /// </summary>
        /// <param name="input">a <c>String</c> containing the XMP packet</param>
        /// <param name="options">the parsing options</param>
        /// <returns>Returns an XML DOM-Document.</returns>
        /// <exception cref="XmpException">Thrown when the parsing fails.</exception>
        private static XDocument ParseXmlString(string input, ParseOptions options)
        {
            try
            {
                using (var sr = new StringReader(input))
                    return ParseTextReader(sr, options);
            }
            catch (XmpException e)
            {
                if (e.ErrorCode == XmpErrorCode.BadXml && options.FixControlChars)
                    return ParseTextReader(new FixAsciiControlsReader(new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(input)))), options);
                throw;
            }
        }

        /// <exception cref="XmpException">Wraps parsing and I/O-exceptions into an XMPException.</exception>
        private static XDocument ParseStream(Stream stream, ParseOptions options)
        {
            try
            {
                // The settings simulate a non-validating processor with the external
                // entity resolution disabled. The processing of the internal subset is
                // enabled by default. In order to prevent DoS attacks, the expanded
                // size of the internal subset is limited to 10 million characters.

                var rdrSettings = new XmlReaderSettings();
                if (options.DisallowDoctype)
                {
#if NET35
                    // the older properties for disabling DTD processing
                    rdrSettings.XmlResolver = null;
                    rdrSettings.ProhibitDtd = true;
#else
                    rdrSettings.DtdProcessing = DtdProcessing.Prohibit;
#endif
                }
                else
                {
#if NET35
                    // the older property for enabling DTD processing
                    rdrSettings.ProhibitDtd = false;
#else
                    // DtdProcessing.Parse; Parse is not defined in the public contract
                    rdrSettings.DtdProcessing = (DtdProcessing)2;
#endif
                }

                rdrSettings.MaxCharactersFromEntities = (long)1e7;

                using (var rdr = XmlReader.Create(new StreamReader(stream), rdrSettings))
                    return XDocument.Load(rdr);
            }
            catch (XmlException e)
            {
                throw new XmpException("XML parsing failure", XmpErrorCode.BadXml, e);
            }
            catch (IOException e)
            {
                throw new XmpException("Error reading the XML-file", XmpErrorCode.BadStream, e);
            }
            catch (Exception e)
            {
                throw new XmpException("XML Parser not correctly configured", XmpErrorCode.Unknown, e);
            }
        }

        /// <exception cref="XmpException">Wraps parsing and I/O-exceptions into an XMPException.</exception>
        private static XDocument ParseTextReader(TextReader reader, ParseOptions options)
        {
            try
            {
                // The settings simulate a non-validating processor with the external
                // entity resolution disabled. The processing of the internal subset is
                // enabled by default. In order to prevent DoS attacks, the expanded
                // size of the internal subset is limited to 10 million characters.

                var rdrSettings = new XmlReaderSettings();
                if (options.DisallowDoctype)
                {
#if NET35
                    // the older properties for disabling DTD processing
                    rdrSettings.XmlResolver = null;
                    rdrSettings.ProhibitDtd = true;
#else
                    rdrSettings.DtdProcessing = DtdProcessing.Prohibit;
#endif
                }
                else
                {
#if NET35
                    // the older property for enabling DTD processing
                    rdrSettings.ProhibitDtd = false;
#else
                    // DtdProcessing.Parse; Parse is not defined in the public contract
                    rdrSettings.DtdProcessing = (DtdProcessing)2;
#endif
                }

                rdrSettings.MaxCharactersFromEntities = (long)1e7;

                using (var rdr = XmlReader.Create(reader, rdrSettings))
                    return XDocument.Load(rdr);
            }
            catch (XmlException e)
            {
                throw new XmpException("XML parsing failure", XmpErrorCode.BadXml, e);
            }
            catch (IOException e)
            {
                throw new XmpException("Error reading the XML-file", XmpErrorCode.BadStream, e);
            }
            catch (Exception e)
            {
                throw new XmpException("XML Parser not correctly configured", XmpErrorCode.Unknown, e);
            }
        }

        /// <summary>Find the XML node that is the root of the XMP data tree.</summary>
        /// <remarks>
        /// Find the XML node that is the root of the XMP data tree. Generally this
        /// will be an outer node, but it could be anywhere if a general XML document
        /// is parsed (e.g. SVG). The XML parser counted all rdf:RDF and
        /// pxmp:XMP_Packet nodes, and kept a pointer to the last one. If there is
        /// more than one possible root use PickBestRoot to choose among them.
        /// <para />
        /// If there is a root node, try to extract the version of the previous XMP
        /// toolkit.
        /// <para />
        /// Pick the first x:xmpmeta among multiple root candidates. If there aren't
        /// any, pick the first bare rdf:RDF if that is allowed. The returned root is
        /// the rdf:RDF child if an x:xmpmeta element was chosen. The search is
        /// breadth first, so a higher level candidate is chosen over a lower level
        /// one that was textually earlier in the serialized XML.
        /// </remarks>
        /// <param name="nodes">initially, the root of the xml document as a list</param>
        /// <param name="xmpmetaRequired">
        /// flag if the xmpmeta-tag is still required, might be set
        /// initially to <c>true</c>, if the parse option "REQUIRE_XMP_META" is set
        /// </param>
        /// <param name="result">The result array that is filled during the recursive process.</param>
        /// <returns>
        /// Returns an array that contains the result or <c>null</c>.
        /// The array contains:
        /// <list type="bullet">
        /// <item>[0] - the rdf:RDF-node</item>
        /// <item>[1] - an object that is either XMP_RDF or XMP_PLAIN (the latter is deprecated)</item>
        /// <item>[2] - the body text of the xpacket-instruction.</item>
        /// </list>
        /// </returns>
        private static object[] FindRootNode(IEnumerable<XNode> nodes, bool xmpmetaRequired, object[] result)
        {
            foreach (var root in nodes)
            {
                if (XmlNodeType.ProcessingInstruction == root.NodeType && ((XProcessingInstruction)root).Target == XmpConstants.XmpPi)
                {
                    // Store the processing instructions content
                    result[2] = ((XProcessingInstruction)root).Data;
                }
                else if (XmlNodeType.Element == root.NodeType)
                {
                    var rootElem = (XElement)root;
                    var rootNS = rootElem.Name.NamespaceName;
                    var rootLocal = rootElem.Name.LocalName;

                    if ((rootLocal == XmpConstants.TagXmpmeta || rootLocal == XmpConstants.TagXapmeta) && rootNS == XmpConstants.NsX)
                    {
                        // by not passing the RequireXMPMeta-option, the rdf-Node will be valid
                        return FindRootNode(rootElem.Nodes(), false, result);
                    }

                    if (!xmpmetaRequired && rootLocal == "RDF" && rootNS == XmpConstants.NsRdf)
                    {
                        if (result != null)
                        {
                            result[0] = root;
                            result[1] = XmpRdf;
                        }
                        return result;
                    }

                    // continue searching
                    var newResult = FindRootNode(rootElem.Nodes(), xmpmetaRequired, result);
                    if (newResult != null)
                        return newResult;
                }
            }

            // no appropriate node has been found
            return null;
        }
    }
}
