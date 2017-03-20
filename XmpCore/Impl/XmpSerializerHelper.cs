// =================================================================================================
// ADOBE SYSTEMS INCORPORATED
// Copyright 2006 Adobe Systems Incorporated
// All Rights Reserved
//
// NOTICE:  Adobe permits you to use, modify, and distribute this file in accordance with the terms
// of the Adobe license agreement accompanying it.
// =================================================================================================


using System.IO;
using XmpCore.Options;

namespace XmpCore.Impl
{
    /// <summary>
    /// Serializes the <c>XMPMeta</c>-object to an <c>OutputStream</c> according to the
    /// <c>SerializeOptions</c>.
    /// </summary>
    /// <since>11.07.2006</since>
    public static class XmpSerializerHelper
    {
        /// <summary>Static method to serialize the metadata object.</summary>
        /// <remarks>
        /// For each serialisation, a new XMPSerializer
        /// instance is created, either XMPSerializerRDF or XMPSerializerPlain so that its possible to
        /// serialize the same XMPMeta objects in two threads.
        /// </remarks>
        /// <param name="xmp">a metadata implementation object</param>
        /// <param name="stream">the output stream to serialize to</param>
        /// <param name="options">serialization options, can be <c>null</c> for default.</param>
        /// <exception cref="XmpException" />
        public static void Serialize(XmpMeta xmp, Stream stream, SerializeOptions options)
        {
            options = options ?? new SerializeOptions();

            // sort the internal data model on demand
            if (options.Sort)
                xmp.Sort();

            new XmpSerializerRdf().Serialize(xmp, stream, options);
        }

        /// <summary>Serializes an <c>XMPMeta</c>-object as RDF into a string.</summary>
        /// <remarks>
        /// <em>Note:</em> Encoding is forced to UTF-16 when serializing to a
        /// string to ensure the correctness of &quot;exact packet size&quot;.
        /// </remarks>
        /// <param name="xmp">a metadata implementation object</param>
        /// <param name="options">Options to control the serialization (see <see cref="SerializeOptions"/>).</param>
        /// <returns>Returns a string containing the serialized RDF.</returns>
        /// <exception cref="XmpException">on serialization errors.</exception>
        public static string SerializeToString(XmpMeta xmp, SerializeOptions options)
        {
            // forces the encoding to be UTF-16 to get the correct string length
            options = options ?? new SerializeOptions();
            options.EncodeUtf16Be = true;

            var output = new MemoryStream(2048);
            Serialize(xmp, output, options);
            try
            {
                return options.GetEncoding().GetString(output.ToArray(), 0, (int)output.Length);
            }
            catch
            {
                // Should not happen as UTF-8/16LE/BE are all available
                return output.ToString();
            }
        }

        /// <summary>Serializes an <c>XMPMeta</c>-object as RDF into a byte buffer.</summary>
        /// <param name="xmp">a metadata implementation object</param>
        /// <param name="options">Options to control the serialization (see <see cref="SerializeOptions"/>).</param>
        /// <returns>Returns a byte buffer containing the serialized RDF.</returns>
        /// <exception cref="XmpException">on serialization errors.</exception>
        public static byte[] SerializeToBuffer(XmpMeta xmp, SerializeOptions options)
        {
            var output = new MemoryStream(2048);
            Serialize(xmp, output, options);
            return output.ToArray();
        }
    }
}