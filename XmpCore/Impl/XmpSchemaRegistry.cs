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
using System.Text.RegularExpressions;
using Sharpen;
using XmpCore.Options;

namespace XmpCore.Impl
{
    /// <summary>The schema registry handles the namespaces, aliases and global options for the XMP Toolkit.</summary>
    /// <remarks>
    /// There is only one singleton instance used by the toolkit, accessed via <see cref="XmpMetaFactory.SchemaRegistry"/>.
    /// </remarks>
    /// <since>27.01.2006</since>
    public sealed class XmpSchemaRegistry : IXmpSchemaRegistry
    {
        /// <summary>a map from a namespace URI to its registered prefix.</summary>
        private readonly Dictionary<string, string> _namespaceToPrefixMap = new Dictionary<string, string>();

        /// <summary>a map from a prefix to the associated namespace URI.</summary>
        private readonly Dictionary<string, string> _prefixToNamespaceMap = new Dictionary<string, string>();

        /// <summary>A map of all registered aliases, from qname to IXmpAliasInfo.</summary>
        private readonly Dictionary<string, IXmpAliasInfo> _aliasMap = new Dictionary<string, IXmpAliasInfo>();

        /// <summary>The pattern that must not be contained in simple properties</summary>
        private static RegexOptions _defaultRegexOptions =
#if PORTABLE
                                        RegexOptions.None;
#else
                                        RegexOptions.Compiled;
#endif
        private readonly Regex _p = new Regex("[/*?\\[\\]]", _defaultRegexOptions);

        private readonly object _lock = new object();

        /// <summary>
        /// Performs the initialisation of the registry with the default namespaces, aliases and global
        /// options.
        /// </summary>
        public XmpSchemaRegistry()
        {
            try
            {
                RegisterStandardNamespaces();
                RegisterStandardAliases();
            }
            catch (XmpException e)
            {
                throw new Exception("The XMPSchemaRegistry cannot be initialized!", e);
            }
        }

        #region Namespaces

        public string RegisterNamespace(string namespaceUri, string suggestedPrefix)
        {
            lock (_lock)
            {
                ParameterAsserts.AssertSchemaNs(namespaceUri);
                ParameterAsserts.AssertPrefix(suggestedPrefix);

                if (suggestedPrefix[suggestedPrefix.Length - 1] != ':')
                    suggestedPrefix += ':';

                if (!Utils.IsXmlNameNs(suggestedPrefix.Substring(0, suggestedPrefix.Length - 1 - 0)))
                    throw new XmpException("The prefix is a bad XML name", XmpErrorCode.BadXml);

                string registeredPrefix;
                if (_namespaceToPrefixMap.TryGetValue(namespaceUri, out registeredPrefix))
                {
                    // Return the actual prefix
                    return registeredPrefix;
                }

                if (_prefixToNamespaceMap.ContainsKey(suggestedPrefix))
                {
                    // the namespace is new, but the prefix is already engaged,
                    // we generate a new prefix out of the suggested
                    var generatedPrefix = suggestedPrefix;
                    for (var i = 1; _prefixToNamespaceMap.ContainsKey(generatedPrefix); i++)
                        generatedPrefix = suggestedPrefix.Substring(0, suggestedPrefix.Length - 1 - 0) + "_" + i + "_:";
                    suggestedPrefix = generatedPrefix;
                }

                _prefixToNamespaceMap[suggestedPrefix] = namespaceUri;
                _namespaceToPrefixMap[namespaceUri] = suggestedPrefix;

                // Return the suggested prefix
                return suggestedPrefix;
            }
        }

        public void DeleteNamespace(string namespaceUri)
        {
            lock (_lock)
            {
                var prefixToDelete = GetNamespacePrefix(namespaceUri);
                if (prefixToDelete != null)
                {
                    _namespaceToPrefixMap.Remove(namespaceUri);
                    _prefixToNamespaceMap.Remove(prefixToDelete);
                }
            }
        }

        public string GetNamespacePrefix(string namespaceUri)
        {
            lock (_lock)
            {
                string value;
                return _namespaceToPrefixMap.TryGetValue(namespaceUri, out value) ? value : null;
            }
        }

        public string GetNamespaceUri(string namespacePrefix)
        {
            lock (_lock)
            {
                if (namespacePrefix != null && !namespacePrefix.EndsWith(":"))
                    namespacePrefix += ":";
                string value;
                return _prefixToNamespaceMap.TryGetValue(namespacePrefix, out value) ? value : null;
            }
        }

        public IDictionary<string, string> Namespaces
        {
            get
            {
                lock (_lock)
                {
                    return new Dictionary<string, string>(_namespaceToPrefixMap);
                }
            }
        }

        public IDictionary<string, string> Prefixes
        {
            get
            {
                lock (_lock)
                {
                    return new Dictionary<string, string>(_prefixToNamespaceMap);
                }
            }
        }

        /// <summary>
        /// Register the standard namespaces of schemas and types that are included in the XMP
        /// Specification and some other Adobe private namespaces.
        /// </summary>
        /// <remarks>
        /// Register the standard namespaces of schemas and types that are included in the XMP
        /// Specification and some other Adobe private namespaces.
        /// Note: This method is not lock because only called by the constructor.
        /// </remarks>
        /// <exception cref="XmpException">Forwards processing exceptions</exception>
        private void RegisterStandardNamespaces()
        {
            // register standard namespaces
            RegisterNamespace(XmpConstants.NsXml, "xml");
            RegisterNamespace(XmpConstants.NsRdf, "rdf");
            RegisterNamespace(XmpConstants.NsDC, "dc");
            RegisterNamespace(XmpConstants.NsIptccore, "Iptc4xmpCore");
            RegisterNamespace(XmpConstants.NsIptcext, "Iptc4xmpExt");
            RegisterNamespace(XmpConstants.NsDicom, "DICOM");
            RegisterNamespace(XmpConstants.NsPlus, "plus");

            // register Adobe standard namespaces
            RegisterNamespace(XmpConstants.NsX, "x");
            RegisterNamespace(XmpConstants.NsIx, "iX");
            RegisterNamespace(XmpConstants.NsXmp, "xmp");
            RegisterNamespace(XmpConstants.NsXmpRights, "xmpRights");
            RegisterNamespace(XmpConstants.NsXmpMm, "xmpMM");
            RegisterNamespace(XmpConstants.NsXmpBj, "xmpBJ");
            RegisterNamespace(XmpConstants.NsXmpNote, "xmpNote");
            RegisterNamespace(XmpConstants.NsPdf, "pdf");
            RegisterNamespace(XmpConstants.NsPdfx, "pdfx");
            RegisterNamespace(XmpConstants.NsPdfxId, "pdfxid");
            RegisterNamespace(XmpConstants.NsPdfaSchema, "pdfaSchema");
            RegisterNamespace(XmpConstants.NsPdfaProperty, "pdfaProperty");
            RegisterNamespace(XmpConstants.NsPdfaType, "pdfaType");
            RegisterNamespace(XmpConstants.NsPdfaField, "pdfaField");
            RegisterNamespace(XmpConstants.NsPdfaId, "pdfaid");
            RegisterNamespace(XmpConstants.NsPdfaExtension, "pdfaExtension");
            RegisterNamespace(XmpConstants.NsPhotoshop, "photoshop");
            RegisterNamespace(XmpConstants.NsPsalbum, "album");
            RegisterNamespace(XmpConstants.NsExif, "exif");
            RegisterNamespace(XmpConstants.NsExifx, "exifEX");
            RegisterNamespace(XmpConstants.NsExifAux, "aux");
            RegisterNamespace(XmpConstants.NsTiff, "tiff");
            RegisterNamespace(XmpConstants.NsPng, "png");
            RegisterNamespace(XmpConstants.NsJpeg, "jpeg");
            RegisterNamespace(XmpConstants.NsJp2K, "jp2k");
            RegisterNamespace(XmpConstants.NsCameraraw, "crs");
            RegisterNamespace(XmpConstants.NsAdobestockphoto, "bmsp");
            RegisterNamespace(XmpConstants.NsCreatorAtom, "creatorAtom");
            RegisterNamespace(XmpConstants.NsAsf, "asf");
            RegisterNamespace(XmpConstants.NsWav, "wav");
            RegisterNamespace(XmpConstants.NsBwf, "bext");
            RegisterNamespace(XmpConstants.NsRiffinfo, "riffinfo");
            RegisterNamespace(XmpConstants.NsScript, "xmpScript");
            RegisterNamespace(XmpConstants.NsTxmp, "txmp");
            RegisterNamespace(XmpConstants.NsSwf, "swf");

            // register Adobe private namespaces
            RegisterNamespace(XmpConstants.NsDm, "xmpDM");
            RegisterNamespace(XmpConstants.NsTransient, "xmpx");

            // register Adobe standard type namespaces
            RegisterNamespace(XmpConstants.TypeText, "xmpT");
            RegisterNamespace(XmpConstants.TypePagedfile, "xmpTPg");
            RegisterNamespace(XmpConstants.TypeGraphics, "xmpG");
            RegisterNamespace(XmpConstants.TypeImage, "xmpGImg");
            RegisterNamespace(XmpConstants.TypeFont, "stFnt");
            RegisterNamespace(XmpConstants.TypeDimensions, "stDim");
            RegisterNamespace(XmpConstants.TypeResourceevent, "stEvt");
            RegisterNamespace(XmpConstants.TypeResourceref, "stRef");
            RegisterNamespace(XmpConstants.TypeStVersion, "stVer");
            RegisterNamespace(XmpConstants.TypeStJob, "stJob");
            RegisterNamespace(XmpConstants.TypeManifestitem, "stMfs");
            RegisterNamespace(XmpConstants.TypeIdentifierqual, "xmpidq");
        }

        #endregion

        #region Aliases

        public IXmpAliasInfo ResolveAlias(string aliasNs, string aliasProp)
        {
            lock (_lock)
            {
                var aliasPrefix = GetNamespacePrefix(aliasNs);
                if (aliasPrefix == null)
                    return null;
                IXmpAliasInfo info;
                return _aliasMap.TryGetValue(aliasPrefix + aliasProp, out info) ? info : null;
            }
        }

        public IXmpAliasInfo FindAlias(string qname)
        {
            lock (_lock)
            {
                IXmpAliasInfo info;
                return _aliasMap.TryGetValue(qname, out info) ? info : null;
            }
        }

        public IEnumerable<IXmpAliasInfo> FindAliases(string aliasNs)
        {
            lock (_lock)
            {
                var prefix = GetNamespacePrefix(aliasNs);
                var result = new List<IXmpAliasInfo>();
                if (prefix != null)
                {
                    for (var it = _aliasMap.Keys.Iterator(); it.HasNext();)
                    {
                        var qname = it.Next();
                        if (qname.StartsWith(prefix))
                        {
                            result.Add(FindAlias(qname));
                        }
                    }
                }
                return result;
            }
        }

        /// <summary>Associates an alias name with an actual name.</summary>
        /// <remarks>
        /// Associates an alias name with an actual name.
        /// <para />
        /// Define a alias mapping from one namespace/property to another. Both
        /// property names must be simple names. An alias can be a direct mapping,
        /// where the alias and actual have the same data type. It is also possible
        /// to map a simple alias to an item in an array. This can either be to the
        /// first item in the array, or to the 'x-default' item in an alt-text array.
        /// Multiple alias names may map to the same actual, as long as the forms
        /// match. It is a no-op to reregister an alias in an identical fashion.
        /// Note: This method is not locking because only called by registerStandardAliases
        /// which is only called by the constructor.
        /// Note2: The method is only package-private so that it can be tested with unittests
        /// </remarks>
        /// <param name="aliasNs">The namespace URI for the alias. Must not be null or the empty string.</param>
        /// <param name="aliasProp">The name of the alias. Must be a simple name, not null or the empty string and not a general path expression.</param>
        /// <param name="actualNs">The namespace URI for the actual. Must not be null or the empty string.</param>
        /// <param name="actualProp">The name of the actual. Must be a simple name, not null or the empty string and not a general path expression.</param>
        /// <param name="aliasForm">Provides options for aliases for simple aliases to array items. This is needed to know what kind of array to create if
        /// set for the first time via the simple alias. Pass <c>XMP_NoOptions</c>, the default value, for all direct aliases regardless of whether the actual
        /// data type is an array or not (see <see cref="AliasOptions"/>).</param>
        /// <exception cref="XmpException">for inconsistant aliases.</exception>
        private void RegisterAlias(string aliasNs, string aliasProp, string actualNs, string actualProp, AliasOptions aliasForm)
        {
            lock (_lock)
            {
                ParameterAsserts.AssertSchemaNs(aliasNs);
                ParameterAsserts.AssertPropName(aliasProp);
                ParameterAsserts.AssertSchemaNs(actualNs);
                ParameterAsserts.AssertPropName(actualProp);

                // Fix the alias options
                var aliasOpts = aliasForm != null ? new AliasOptions(XmpNodeUtils.VerifySetOptions(aliasForm.ToPropertyOptions(), null).GetOptions()) : new AliasOptions();
                if (_p.IsMatch(aliasProp) || _p.IsMatch(actualProp))
                    throw new XmpException("Alias and actual property names must be simple", XmpErrorCode.BadXPath);

                // check if both namespaces are registered
                var aliasPrefix = GetNamespacePrefix(aliasNs);
                var actualPrefix = GetNamespacePrefix(actualNs);

                if (aliasPrefix == null)
                    throw new XmpException("Alias namespace is not registered", XmpErrorCode.BadSchema);
                if (actualPrefix == null)
                    throw new XmpException("Actual namespace is not registered", XmpErrorCode.BadSchema);

                var key = aliasPrefix + aliasProp;

                // check if alias is already existing
                if (_aliasMap.ContainsKey(key))
                    throw new XmpException("Alias is already existing", XmpErrorCode.BadParam);
                if (_aliasMap.ContainsKey(actualPrefix + actualProp))
                    throw new XmpException("Actual property is already an alias, use the base property", XmpErrorCode.BadParam);

                _aliasMap[key] = new XmpAliasInfo(actualNs, actualPrefix, actualProp, aliasOpts);
            }
        }

        private sealed class XmpAliasInfo : IXmpAliasInfo
        {
            public XmpAliasInfo(string actualNs, string actualPrefix, string actualProp, AliasOptions aliasOpts)
            {
                Namespace = actualNs;
                Prefix = actualPrefix;
                PropName = actualProp;
                AliasForm = aliasOpts;
            }

            public string Namespace { get; }
            public string Prefix { get; }
            public string PropName { get; }
            public AliasOptions AliasForm { get; }

            public override string ToString()
            {
                return $"{Prefix}{PropName} NS({Namespace}), FORM ({AliasForm})";
            }
        }

        public IDictionary<string, IXmpAliasInfo> Aliases
        {
            get
            {
                lock (_lock)
                {
                    return new Dictionary<string, IXmpAliasInfo>(_aliasMap);
                }
            }
        }

        private void RegisterStandardAliases()
        {
            var aliasToArrayOrdered = new AliasOptions { IsArrayOrdered = true };
            var aliasToArrayAltText = new AliasOptions { IsArrayAltText = true };

            // Aliases from XMP to DC.
            RegisterAlias(XmpConstants.NsXmp, "Author", XmpConstants.NsDC, "creator", aliasToArrayOrdered);
            RegisterAlias(XmpConstants.NsXmp, "Authors", XmpConstants.NsDC, "creator", null);
            RegisterAlias(XmpConstants.NsXmp, "Description", XmpConstants.NsDC, "description", null);
            RegisterAlias(XmpConstants.NsXmp, "Format", XmpConstants.NsDC, "format", null);
            RegisterAlias(XmpConstants.NsXmp, "Keywords", XmpConstants.NsDC, "subject", null);
            RegisterAlias(XmpConstants.NsXmp, "Locale", XmpConstants.NsDC, "language", null);
            RegisterAlias(XmpConstants.NsXmp, "Title", XmpConstants.NsDC, "title", null);
            RegisterAlias(XmpConstants.NsXmpRights, "Copyright", XmpConstants.NsDC, "rights", null);

            // Aliases from PDF to DC and XMP.
            RegisterAlias(XmpConstants.NsPdf, "Author", XmpConstants.NsDC, "creator", aliasToArrayOrdered);
            RegisterAlias(XmpConstants.NsPdf, "BaseURL", XmpConstants.NsXmp, "BaseURL", null);
            RegisterAlias(XmpConstants.NsPdf, "CreationDate", XmpConstants.NsXmp, "CreateDate", null);
            RegisterAlias(XmpConstants.NsPdf, "Creator", XmpConstants.NsXmp, "CreatorTool", null);
            RegisterAlias(XmpConstants.NsPdf, "ModDate", XmpConstants.NsXmp, "ModifyDate", null);
            RegisterAlias(XmpConstants.NsPdf, "Subject", XmpConstants.NsDC, "description", aliasToArrayAltText);
            RegisterAlias(XmpConstants.NsPdf, "Title", XmpConstants.NsDC, "title", aliasToArrayAltText);

            // Aliases from PHOTOSHOP to DC and XMP.
            RegisterAlias(XmpConstants.NsPhotoshop, "Author", XmpConstants.NsDC, "creator", aliasToArrayOrdered);
            RegisterAlias(XmpConstants.NsPhotoshop, "Caption", XmpConstants.NsDC, "description", aliasToArrayAltText);
            RegisterAlias(XmpConstants.NsPhotoshop, "Copyright", XmpConstants.NsDC, "rights", aliasToArrayAltText);
            RegisterAlias(XmpConstants.NsPhotoshop, "Keywords", XmpConstants.NsDC, "subject", null);
            RegisterAlias(XmpConstants.NsPhotoshop, "Marked", XmpConstants.NsXmpRights, "Marked", null);
            RegisterAlias(XmpConstants.NsPhotoshop, "Title", XmpConstants.NsDC, "title", aliasToArrayAltText);
            RegisterAlias(XmpConstants.NsPhotoshop, "WebStatement", XmpConstants.NsXmpRights, "WebStatement", null);

            // Aliases from TIFF and EXIF to DC and XMP.
            RegisterAlias(XmpConstants.NsTiff, "Artist", XmpConstants.NsDC, "creator", aliasToArrayOrdered);
            RegisterAlias(XmpConstants.NsTiff, "Copyright", XmpConstants.NsDC, "rights", null);
            RegisterAlias(XmpConstants.NsTiff, "DateTime", XmpConstants.NsXmp, "ModifyDate", null);
            RegisterAlias(XmpConstants.NsTiff, "ImageDescription", XmpConstants.NsDC, "description", null);
            RegisterAlias(XmpConstants.NsTiff, "Software", XmpConstants.NsXmp, "CreatorTool", null);

            // Aliases from PNG (Acrobat ImageCapture) to DC and XMP.
            RegisterAlias(XmpConstants.NsPng, "Author", XmpConstants.NsDC, "creator", aliasToArrayOrdered);
            RegisterAlias(XmpConstants.NsPng, "Copyright", XmpConstants.NsDC, "rights", aliasToArrayAltText);
            RegisterAlias(XmpConstants.NsPng, "CreationTime", XmpConstants.NsXmp, "CreateDate", null);
            RegisterAlias(XmpConstants.NsPng, "Description", XmpConstants.NsDC, "description", aliasToArrayAltText);
            RegisterAlias(XmpConstants.NsPng, "ModificationTime", XmpConstants.NsXmp, "ModifyDate", null);
            RegisterAlias(XmpConstants.NsPng, "Software", XmpConstants.NsXmp, "CreatorTool", null);
            RegisterAlias(XmpConstants.NsPng, "Title", XmpConstants.NsDC, "title", aliasToArrayAltText);
        }

        #endregion
    }
}
