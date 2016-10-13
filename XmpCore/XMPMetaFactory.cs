//=================================================================================================
//ADOBE SYSTEMS INCORPORATED
//Copyright 2006-2007 Adobe Systems Incorporated
//All Rights Reserved
//
//NOTICE:  Adobe permits you to use, modify, and distribute this file in accordance with the terms
//of the Adobe license agreement accompanying it.
//=================================================================================================


using System;
using System.IO;
using XmpCore.Impl;
using XmpCore.Options;

namespace XmpCore
{
    /// <summary>Parses and serialises <see cref="IXmpMeta"/> instances.</summary>
    /// <since>30.01.2006</since>
    public static class XmpMetaFactory
    {
        /// <value>Returns the singleton instance of the <see cref="XmpSchemaRegistry"/>.</value>
        public static IXmpSchemaRegistry SchemaRegistry { get; private set; }

        static XmpMetaFactory()
        {
            SchemaRegistry = new XmpSchemaRegistry();
        }

        /// <returns>Returns an empty <c>XMPMeta</c>-object.</returns>
        public static IXmpMeta Create()
        {
            return new XmpMeta();
        }

        /// <summary>
        /// These functions support parsing serialized RDF into an XMP object, and serializing an XMP
        /// object into RDF.
        /// </summary>
        /// <remarks>
        /// These functions support parsing serialized RDF into an XMP object, and serializing an XMP
        /// object into RDF. The input for parsing may be any valid Unicode
        /// encoding. ISO Latin-1 is also recognized, but its use is strongly discouraged. Serialization
        /// is always as UTF-8.
        /// <para />
        /// <c>parseFromBuffer()</c> parses RDF from an <c>Stream</c>. The encoding
        /// is recognized automatically.
        /// </remarks>
        /// <param name="stream">an <c>Stream</c></param>
        /// <param name="options">Options controlling the parsing.
        /// The available options are:
        /// <list type="bullet">
        ///   <item>XMP_REQUIRE_XMPMETA - The &lt;x:xmpmeta&gt; XML element is required around <tt>&lt;rdf:RDF&gt;</tt>.</item>
        ///   <item>XMP_STRICT_ALIASING - Do not reconcile alias differences, throw an exception.</item>
        /// </list>
        /// Note: The XMP_STRICT_ALIASING option is not yet implemented.
        /// </param>
        /// <returns>Returns the <c>XMPMeta</c>-object created from the input.</returns>
        /// <exception cref="XmpException">If the file is not well-formed XML or if the parsing fails.</exception>
        public static IXmpMeta Parse(Stream stream, ParseOptions options = null)
        {
            return XmpMetaParser.Parse(stream, options);
        }

        /// <summary>Creates an <c>XMPMeta</c>-object from a string.</summary>
        /// <seealso cref="ParseFromString(string, ParseOptions)"/>
        /// <param name="packet">a String contain an XMP-file.</param>
        /// <param name="options">Options controlling the parsing.</param>
        /// <returns>Returns the <c>XMPMeta</c>-object created from the input.</returns>
        /// <exception cref="XmpException">If the file is not well-formed XML or if the parsing fails.</exception>
        public static IXmpMeta ParseFromString(string packet, ParseOptions options = null)
        {
            return XmpMetaParser.Parse(packet, options);
        }

        /// <summary>Creates an <c>XMPMeta</c>-object from a byte-buffer.</summary>
        /// <seealso cref="Parse(Stream, ParseOptions)"/>
        /// <param name="buffer">a String contain an XMP-file.</param>
        /// <param name="options">Options controlling the parsing.</param>
        /// <returns>Returns the <c>XMPMeta</c>-object created from the input.</returns>
        /// <exception cref="XmpException">If the file is not well-formed XML or if the parsing fails.</exception>
        public static IXmpMeta ParseFromBuffer(byte[] buffer, ParseOptions options = null)
        {
            return XmpMetaParser.Parse(buffer, options);
        }

        /// <summary>Serializes an <c>XMPMeta</c>-object as RDF into an <c>OutputStream</c>.</summary>
        /// <param name="xmp">a metadata object</param>
        /// <param name="options">Options to control the serialization (see <see cref="SerializeOptions"/>).</param>
        /// <param name="stream">an <c>OutputStream</c> to write the serialized RDF to.</param>
        /// <exception cref="XmpException">on serialization errors.</exception>
        public static void Serialize(IXmpMeta xmp, Stream stream, SerializeOptions options = null)
        {
            AssertImplementation(xmp);
            XmpSerializerHelper.Serialize((XmpMeta)xmp, stream, options);
        }

        /// <summary>Serializes an <c>XMPMeta</c>-object as RDF into a byte buffer.</summary>
        /// <param name="xmp">a metadata object</param>
        /// <param name="options">Options to control the serialization (see <see cref="SerializeOptions"/>).</param>
        /// <returns>Returns a byte buffer containing the serialized RDF.</returns>
        /// <exception cref="XmpException">on serialization errors.</exception>
        public static byte[] SerializeToBuffer(IXmpMeta xmp, SerializeOptions options)
        {
            AssertImplementation(xmp);
            return XmpSerializerHelper.SerializeToBuffer((XmpMeta)xmp, options);
        }

        /// <summary>Serializes an <c>XMPMeta</c>-object as RDF into a string.</summary>
        /// <remarks>
        /// Serializes an <c>XMPMeta</c>-object as RDF into a string. <em>Note:</em> Encoding
        /// is ignored when serializing to a string.
        /// </remarks>
        /// <param name="xmp">a metadata object</param>
        /// <param name="options">Options to control the serialization (see <see cref="SerializeOptions"/>).</param>
        /// <returns>Returns a string containing the serialized RDF.</returns>
        /// <exception cref="XmpException">on serialization errors.</exception>
        public static string SerializeToString(IXmpMeta xmp, SerializeOptions options)
        {
            AssertImplementation(xmp);
            return XmpSerializerHelper.SerializeToString((XmpMeta)xmp, options);
        }

        /// <param name="xmp">Asserts that xmp is compatible to <c>XMPMetaImpl</c>.s</param>
        private static void AssertImplementation(IXmpMeta xmp)
        {
            if (!(xmp is XmpMeta))
                throw new NotSupportedException($"The serializing service works only with the {nameof(XmpMeta)} implementation of this library");
        }

        /// <summary>Resets the schema registry to its original state (creates a new one).</summary>
        /// <remarks>
        /// Resets the schema registry to its original state (creates a new one).
        /// Be careful this might break all existing XMPMeta-objects and should be used
        /// only for testing purposes.
        /// </remarks>
        public static void Reset()
        {
            SchemaRegistry = new XmpSchemaRegistry();
        }

        /// <summary>Obtain version information.</summary>
        public static IXmpVersionInfo VersionInfo
        {
            get { return new XmpVersionInfo(5, 1, 0, false, 3, "Adobe XMP Core 5.1.0-jc003"); }
        }

        private sealed class XmpVersionInfo : IXmpVersionInfo
        {
            public int Major { get; }
            public int Minor { get; }
            public int Micro { get; }
            public bool IsDebug { get; }
            public int Build { get; }
            public string Message { get; }

            public XmpVersionInfo(int major, int minor, int micro, bool debug, int engBuild, string message)
            {
                Major = major;
                Minor = minor;
                Micro = micro;
                IsDebug = debug;
                Build = engBuild;
                Message = message;
            }

            public override string ToString()
            {
                return Message;
            }
        }
    }
}
