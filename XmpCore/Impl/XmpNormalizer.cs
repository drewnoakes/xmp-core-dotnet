// =================================================================================================
// ADOBE SYSTEMS INCORPORATED
// Copyright 2006 Adobe Systems Incorporated
// All Rights Reserved
//
// NOTICE:  Adobe permits you to use, modify, and distribute this file in accordance with the terms
// of the Adobe license agreement accompanying it.
// =================================================================================================


using System.Collections;
#if PORTABLE
using System.Collections.Generic;
#endif
using Sharpen;
using XmpCore.Impl.XPath;
using XmpCore.Options;

namespace XmpCore.Impl
{
    /// <since>Aug 18, 2006</since>
    public static class XmpNormalizer
    {
        /// <summary>caches the correct dc-property array forms</summary>
        private static IDictionary _dcArrayForms;

        static XmpNormalizer()
        {
            InitDcArrays();
        }

        /// <summary>Normalizes a raw parsed XMPMeta-Object</summary>
        /// <param name="xmp">the raw metadata object</param>
        /// <param name="options">the parsing options</param>
        /// <returns>Returns the normalized metadata object</returns>
        /// <exception cref="XmpException">Collects all severe processing errors.</exception>
        internal static IXmpMeta Process(XmpMeta xmp, ParseOptions options)
        {
            var tree = xmp.GetRoot();
            TouchUpDataModel(xmp);
            MoveExplicitAliases(tree, options);
            TweakOldXmp(tree);
            DeleteEmptySchemas(tree);
            return xmp;
        }

        /// <summary>
        /// Tweak old XMP: Move an instance ID from rdf:about to the
        /// <em>xmpMM:InstanceID</em> property.
        /// </summary>
        /// <remarks>
        /// Tweak old XMP: Move an instance ID from rdf:about to the
        /// <em>xmpMM:InstanceID</em> property. An old instance ID usually looks
        /// like &quot;uuid:bac965c4-9d87-11d9-9a30-000d936b79c4&quot;, plus InDesign
        /// 3.0 wrote them like &quot;bac965c4-9d87-11d9-9a30-000d936b79c4&quot;. If
        /// the name looks like a UUID simply move it to <em>xmpMM:InstanceID</em>,
        /// don't worry about any existing <em>xmpMM:InstanceID</em>. Both will
        /// only be present when a newer file with the <em>xmpMM:InstanceID</em>
        /// property is updated by an old app that uses <em>rdf:about</em>.
        /// </remarks>
        /// <param name="tree">the root of the metadata tree</param>
        /// <exception cref="XmpException">Thrown if tweaking fails.</exception>
        private static void TweakOldXmp(XmpNode tree)
        {
            if (tree.Name != null && tree.Name.Length >= Utils.UuidLength)
            {
                var nameStr = tree.Name.ToLower();
                if (nameStr.StartsWith("uuid:"))
                {
                    nameStr = nameStr.Substring (5);
                }
                if (Utils.CheckUuidFormat(nameStr))
                {
                    // move UUID to xmpMM:InstanceID and remove it from the root node
                    var path = XmpPathParser.ExpandXPath(XmpConstants.NsXmpMm, "InstanceID");
                    var idNode = XmpNodeUtils.FindNode(tree, path, true, null);
                    if (idNode != null)
                    {
                        idNode.Options = null;
                        // Clobber any existing xmpMM:InstanceID.
                        idNode.Value = "uuid:" + nameStr;
                        idNode.RemoveChildren();
                        idNode.RemoveQualifiers();
                        tree.Name = null;
                    }
                    else
                    {
                        throw new XmpException("Failure creating xmpMM:InstanceID", XmpErrorCode.InternalFailure);
                    }
                }
            }
        }

        /// <summary>Visit all schemas to do general fixes and handle special cases.</summary>
        /// <param name="xmp">the metadata object implementation</param>
        /// <exception cref="XmpException">Thrown if the normalisation fails.</exception>
        private static void TouchUpDataModel(XmpMeta xmp)
        {
            // make sure the DC schema is existing, because it might be needed within the normalization
            // if not touched it will be removed by removeEmptySchemas
            XmpNodeUtils.FindSchemaNode(xmp.GetRoot(), XmpConstants.NsDC, true);
            // Do the special case fixes within each schema.
            for (var it = xmp.GetRoot().IterateChildren(); it.HasNext(); )
            {
                var currSchema = (XmpNode)it.Next();
                if (XmpConstants.NsDC.Equals(currSchema.Name))
                {
                    NormalizeDcArrays(currSchema);
                }
                else
                {
                    if (XmpConstants.NsExif.Equals(currSchema.Name))
                    {
                        // Do a special case fix for exif:GPSTimeStamp.
                        FixGpsTimeStamp(currSchema);
                        var arrayNode = XmpNodeUtils.FindChildNode(currSchema, "exif:UserComment", false);
                        if (arrayNode != null)
                        {
                            RepairAltText(arrayNode);
                        }
                    }
                    else
                    {
                        if (XmpConstants.NsDm.Equals(currSchema.Name))
                        {
                            // Do a special case migration of xmpDM:copyright to
                            // dc:rights['x-default'].
                            var dmCopyright = XmpNodeUtils.FindChildNode(currSchema, "xmpDM:copyright", false);
                            if (dmCopyright != null)
                            {
                                MigrateAudioCopyright(xmp, dmCopyright);
                            }
                        }
                        else
                        {
                            if (XmpConstants.NsXmpRights.Equals(currSchema.Name))
                            {
                                var arrayNode = XmpNodeUtils.FindChildNode(currSchema, "xmpRights:UsageTerms", false);
                                if (arrayNode != null)
                                {
                                    RepairAltText(arrayNode);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Undo the denormalization performed by the XMP used in Acrobat 5.<br />
        /// If a Dublin Core array had only one item, it was serialized as a simple
        /// property.
        /// </summary>
        /// <remarks>
        /// Undo the denormalization performed by the XMP used in Acrobat 5.<br />
        /// If a Dublin Core array had only one item, it was serialized as a simple
        /// property. <br />
        /// The <c>xml:lang</c> attribute was dropped from an
        /// <c>alt-text</c> item if the language was <c>x-default</c>.
        /// </remarks>
        /// <param name="dcSchema">the DC schema node</param>
        /// <exception cref="XmpException">Thrown if normalization fails</exception>
        private static void NormalizeDcArrays(XmpNode dcSchema)
        {
            for (var i = 1; i <= dcSchema.GetChildrenLength(); i++)
            {
                var currProp = dcSchema.GetChild(i);
                var arrayForm = (PropertyOptions)_dcArrayForms[currProp.Name];
                if (arrayForm == null)
                {
                    continue;
                }
                if (currProp.Options.IsSimple)
                {
                    // create a new array and add the current property as child,
                    // if it was formerly simple
                    var newArray = new XmpNode(currProp.Name, arrayForm);
                    currProp.Name = XmpConstants.ArrayItemName;
                    newArray.AddChild(currProp);
                    dcSchema.ReplaceChild(i, newArray);
                    // fix language alternatives
                    if (arrayForm.IsArrayAltText && !currProp.Options.HasLanguage)
                    {
                        var newLang = new XmpNode(XmpConstants.XmlLang, XmpConstants.XDefault, null);
                        currProp.AddQualifier(newLang);
                    }
                }
                else
                {
                    // clear array options and add corrected array form if it has been an array before
                    currProp.Options.SetOption(PropertyOptions.ArrayFlag | PropertyOptions.ArrayOrderedFlag | PropertyOptions.ArrayAlternateFlag | PropertyOptions.ArrayAltTextFlag, false);
                    currProp.Options.MergeWith(arrayForm);
                    if (arrayForm.IsArrayAltText)
                    {
                        // applying for "dc:description", "dc:rights", "dc:title"
                        RepairAltText(currProp);
                    }
                }
            }
        }

        /// <summary>Make sure that the array is well-formed AltText.</summary>
        /// <remarks>
        /// Make sure that the array is well-formed AltText. Each item must be simple
        /// and have an "xml:lang" qualifier. If repairs are needed, keep simple
        /// non-empty items by adding the "xml:lang" with value "x-repair".
        /// </remarks>
        /// <param name="arrayNode">the property node of the array to repair.</param>
        /// <exception cref="XmpException">Forwards unexpected exceptions.</exception>
        private static void RepairAltText(XmpNode arrayNode)
        {
            if (arrayNode == null || !arrayNode.Options.IsArray)
            {
                // Already OK or not even an array.
                return;
            }
            // fix options
            arrayNode.Options.IsArrayOrdered = true;
            arrayNode.Options.IsArrayAlternate = true;
            arrayNode.Options.IsArrayAltText = true;
            for (var it = arrayNode.IterateChildren(); it.HasNext(); )
            {
                var currChild = (XmpNode)it.Next();
                if (currChild.Options.IsCompositeProperty)
                {
                    // Delete non-simple children.
                    it.Remove();
                }
                else if (!currChild.Options.HasLanguage)
                {
                    if (string.IsNullOrEmpty(currChild.Value))
                    {
                        // Delete empty valued children that have no xml:lang.
                        it.Remove();
                    }
                    else
                    {
                        // Add an xml:lang qualifier with the value "x-repair".
                        var repairLang = new XmpNode(XmpConstants.XmlLang, "x-repair", null);
                        currChild.AddQualifier(repairLang);
                    }
                }
            }
        }

        /// <summary>Visit all of the top level nodes looking for aliases.</summary>
        /// <remarks>
        /// Visit all of the top level nodes looking for aliases. If there is
        /// no base, transplant the alias subtree. If there is a base and strict
        /// aliasing is on, make sure the alias and base subtrees match.
        /// </remarks>
        /// <param name="tree">the root of the metadata tree</param>
        /// <param name="options">th parsing options</param>
        /// <exception cref="XmpException">Forwards XMP errors</exception>
        private static void MoveExplicitAliases(XmpNode tree, ParseOptions options)
        {
            if (!tree.HasAliases)
            {
                return;
            }
            tree.HasAliases = false;
            var strictAliasing = options.StrictAliasing;
            for (var schemaIt = tree.GetUnmodifiableChildren().Iterator(); schemaIt.HasNext(); )
            {
                var currSchema = (XmpNode)schemaIt.Next();
                if (!currSchema.HasAliases)
                {
                    continue;
                }
                for (var propertyIt = currSchema.IterateChildren(); propertyIt.HasNext(); )
                {
                    var currProp = (XmpNode)propertyIt.Next();
                    if (!currProp.IsAlias)
                    {
                        continue;
                    }
                    currProp.IsAlias = false;
                    // Find the base path, look for the base schema and root node.
                    var info = XmpMetaFactory.SchemaRegistry.FindAlias(currProp.Name);
                    if (info != null)
                    {
                        // find or create schema
                        var baseSchema = XmpNodeUtils.FindSchemaNode(tree, info.Namespace, null, true);
                        baseSchema.IsImplicit = false;
                        var baseNode = XmpNodeUtils.FindChildNode(baseSchema, info.Prefix + info.PropName, false);
                        if (baseNode == null)
                        {
                            if (info.AliasForm.IsSimple())
                            {
                                // A top-to-top alias, transplant the property.
                                // change the alias property name to the base name
                                var qname = info.Prefix + info.PropName;
                                currProp.Name = qname;
                                baseSchema.AddChild(currProp);
                                // remove the alias property
                                propertyIt.Remove();
                            }
                            else
                            {
                                // An alias to an array item,
                                // create the array and transplant the property.
                                baseNode = new XmpNode(info.Prefix + info.PropName, info.AliasForm.ToPropertyOptions());
                                baseSchema.AddChild(baseNode);
                                TransplantArrayItemAlias(propertyIt, currProp, baseNode);
                            }
                        }
                        else if (info.AliasForm.IsSimple())
                        {
                            // The base node does exist and this is a top-to-top alias.
                            // Check for conflicts if strict aliasing is on.
                            // Remove and delete the alias subtree.
                            if (strictAliasing)
                            {
                                CompareAliasedSubtrees(currProp, baseNode, true);
                            }
                            propertyIt.Remove();
                        }
                        else
                        {
                            // This is an alias to an array item and the array exists.
                            // Look for the aliased item.
                            // Then transplant or check & delete as appropriate.
                            XmpNode itemNode = null;
                            if (info.AliasForm.IsArrayAltText)
                            {
                                var xdIndex = XmpNodeUtils.LookupLanguageItem(baseNode, XmpConstants.XDefault);
                                if (xdIndex != -1)
                                {
                                    itemNode = baseNode.GetChild(xdIndex);
                                }
                            }
                            else if (baseNode.HasChildren)
                            {
                                itemNode = baseNode.GetChild(1);
                            }

                            if (itemNode == null)
                            {
                                TransplantArrayItemAlias(propertyIt, currProp, baseNode);
                            }
                            else if (strictAliasing)
                            {
                                CompareAliasedSubtrees(currProp, itemNode, true);
                            }
                            propertyIt.Remove();
                        }
                    }
                }
                currSchema.HasAliases = false;
            }
        }

        /// <summary>Moves an alias node of array form to another schema into an array</summary>
        /// <param name="propertyIt">the property iterator of the old schema (used to delete the property)</param>
        /// <param name="childNode">the node to be moved</param>
        /// <param name="baseArray">the base array for the array item</param>
        /// <exception cref="XmpException">Forwards XMP errors</exception>
        private static void TransplantArrayItemAlias(IIterator propertyIt, XmpNode childNode, XmpNode baseArray)
        {
            if (baseArray.Options.IsArrayAltText)
            {
                if (childNode.Options.HasLanguage)
                {
                    throw new XmpException("Alias to x-default already has a language qualifier", XmpErrorCode.BadXmp);
                }
                var langQual = new XmpNode(XmpConstants.XmlLang, XmpConstants.XDefault, null);
                childNode.AddQualifier(langQual);
            }
            propertyIt.Remove();
            childNode.Name = XmpConstants.ArrayItemName;
            baseArray.AddChild(childNode);
        }

        /// <summary>Fixes the GPS Timestamp in EXIF.</summary>
        /// <param name="exifSchema">the EXIF schema node</param>
        /// <exception cref="XmpException">Thrown if the date conversion fails.</exception>
        private static void FixGpsTimeStamp(XmpNode exifSchema)
        {
            // Note: if dates are not found the convert-methods throws an exceptions,
            //          and this methods returns.
            var gpsDateTime = XmpNodeUtils.FindChildNode(exifSchema, "exif:GPSTimeStamp", false);

            if (gpsDateTime == null)
                return;

            try
            {
                var binGpsStamp = XmpCore.XmpUtils.ConvertToDate(gpsDateTime.Value);
                if (binGpsStamp.Year != 0 || binGpsStamp.Month != 0 || binGpsStamp.Day != 0)
                    return;

                var otherDate = XmpNodeUtils.FindChildNode(exifSchema, "exif:DateTimeOriginal", false)
                    ?? XmpNodeUtils.FindChildNode(exifSchema, "exif:DateTimeDigitized", false);

                var binOtherDate = XmpCore.XmpUtils.ConvertToDate(otherDate.Value);
                var cal = binGpsStamp.Calendar;
                cal.Set(CalendarEnum.Year, binOtherDate.Year);
                cal.Set(CalendarEnum.Month, binOtherDate.Month);
                cal.Set(CalendarEnum.DayOfMonth, binOtherDate.Day);
                binGpsStamp = new XmpDateTime(cal);
                gpsDateTime.Value = XmpCore.XmpUtils.ConvertFromDate(binGpsStamp);
            }
            catch (XmpException)
            {
                // Don't let a missing or bad date stop other things.
            }
        }

        /// <summary>Remove all empty schemas from the metadata tree that were generated during the rdf parsing.</summary>
        /// <param name="tree">the root of the metadata tree</param>
        private static void DeleteEmptySchemas(XmpNode tree)
        {
            // Delete empty schema nodes. Do this last, other cleanup can make empty
            // schema.
            for (var it = tree.IterateChildren(); it.HasNext(); )
            {
                var schema = (XmpNode)it.Next();
                if (!schema.HasChildren)
                {
                    it.Remove();
                }
            }
        }

        /// <summary>The outermost call is special.</summary>
        /// <remarks>
        /// The outermost call is special. The names almost certainly differ. The
        /// qualifiers (and hence options) will differ for an alias to the x-default
        /// item of a langAlt array.
        /// </remarks>
        /// <param name="aliasNode">the alias node</param>
        /// <param name="baseNode">the base node of the alias</param>
        /// <param name="outerCall">marks the outer call of the recursion</param>
        /// <exception cref="XmpException">Forwards XMP errors</exception>
        private static void CompareAliasedSubtrees(XmpNode aliasNode, XmpNode baseNode, bool outerCall)
        {
            if (!aliasNode.Value.Equals(baseNode.Value) || aliasNode.GetChildrenLength() != baseNode.GetChildrenLength())
            {
                throw new XmpException("Mismatch between alias and base nodes", XmpErrorCode.BadXmp);
            }
            if (!outerCall && (!aliasNode.Name.Equals(baseNode.Name) || !aliasNode.Options.Equals(baseNode.Options) || aliasNode.GetQualifierLength() != baseNode.GetQualifierLength()))
            {
                throw new XmpException("Mismatch between alias and base nodes", XmpErrorCode.BadXmp);
            }
            for (IIterator an = aliasNode.IterateChildren(), bn = baseNode.IterateChildren(); an.HasNext() && bn.HasNext(); )
            {
                var aliasChild = (XmpNode)an.Next();
                var baseChild = (XmpNode)bn.Next();
                CompareAliasedSubtrees(aliasChild, baseChild, false);
            }
            for (IIterator an1 = aliasNode.IterateQualifier(), bn1 = baseNode.IterateQualifier(); an1.HasNext() && bn1.HasNext(); )
            {
                var aliasQual = (XmpNode)an1.Next();
                var baseQual = (XmpNode)bn1.Next();
                CompareAliasedSubtrees(aliasQual, baseQual, false);
            }
        }

        /// <summary>
        /// The initial support for WAV files mapped a legacy ID3 audio copyright
        /// into a new xmpDM:copyright property.
        /// </summary>
        /// <remarks>
        /// The initial support for WAV files mapped a legacy ID3 audio copyright
        /// into a new xmpDM:copyright property. This is special case code to migrate
        /// that into dc:rights['x-default']. The rules:
        /// <pre>
        /// 1. If there is no dc:rights array, or an empty array -
        /// Create one with dc:rights['x-default'] set from double linefeed and xmpDM:copyright.
        /// 2. If there is a dc:rights array but it has no x-default item -
        /// Create an x-default item as a copy of the first item then apply rule #3.
        /// 3. If there is a dc:rights array with an x-default item,
        /// Look for a double linefeed in the value.
        /// A. If no double linefeed, compare the x-default value to the xmpDM:copyright value.
        /// A1. If they match then leave the x-default value alone.
        /// A2. Otherwise, append a double linefeed and
        /// the xmpDM:copyright value to the x-default value.
        /// B. If there is a double linefeed, compare the trailing text to the xmpDM:copyright value.
        /// B1. If they match then leave the x-default value alone.
        /// B2. Otherwise, replace the trailing x-default text with the xmpDM:copyright value.
        /// 4. In all cases, delete the xmpDM:copyright property.
        /// </pre>
        /// </remarks>
        /// <param name="xmp">the metadata object</param>
        /// <param name="dmCopyright">the "dm:copyright"-property</param>
        private static void MigrateAudioCopyright(IXmpMeta xmp, XmpNode dmCopyright)
        {
            try
            {
                var dcSchema = XmpNodeUtils.FindSchemaNode(((XmpMeta)xmp).GetRoot(), XmpConstants.NsDC, true);
                var dmValue = dmCopyright.Value;
                var doubleLf = "\n\n";
                var dcRightsArray = XmpNodeUtils.FindChildNode(dcSchema, "dc:rights", false);
                if (dcRightsArray == null || !dcRightsArray.HasChildren)
                {
                    // 1. No dc:rights array, create from double linefeed and xmpDM:copyright.
                    dmValue = doubleLf + dmValue;
                    xmp.SetLocalizedText(XmpConstants.NsDC, "rights", string.Empty, XmpConstants.XDefault, dmValue, null);
                }
                else
                {
                    var xdIndex = XmpNodeUtils.LookupLanguageItem(dcRightsArray, XmpConstants.XDefault);
                    if (xdIndex < 0)
                    {
                        // 2. No x-default item, create from the first item.
                        var firstValue = dcRightsArray.GetChild(1).Value;
                        xmp.SetLocalizedText(XmpConstants.NsDC, "rights", string.Empty, XmpConstants.XDefault, firstValue, null);
                        xdIndex = XmpNodeUtils.LookupLanguageItem(dcRightsArray, XmpConstants.XDefault);
                    }
                    // 3. Look for a double linefeed in the x-default value.
                    var defaultNode = dcRightsArray.GetChild(xdIndex);
                    var defaultValue = defaultNode.Value;
                    var lfPos = defaultValue.IndexOf(doubleLf);
                    if (lfPos < 0)
                    {
                        // 3A. No double LF, compare whole values.
                        if (!dmValue.Equals(defaultValue))
                        {
                            // 3A2. Append the xmpDM:copyright to the x-default
                            // item.
                            defaultNode.Value = defaultValue + doubleLf + dmValue;
                        }
                    }
                    else
                    {
                        // 3B. Has double LF, compare the tail.
                        if (!defaultValue.Substring (lfPos + 2).Equals(dmValue))
                        {
                            // 3B2. Replace the x-default tail.
                            defaultNode.Value = defaultValue.Substring (0, lfPos + 2 - 0) + dmValue;
                        }
                    }
                }
                // 4. Get rid of the xmpDM:copyright.
                dmCopyright.Parent.RemoveChild(dmCopyright);
            }
            catch (XmpException)
            {
            }
        }

        // Don't let failures (like a bad dc:rights form) stop other
        // cleanup.
        /// <summary>
        /// Initializes the map that contains the known arrays, that are fixed by <see cref="NormalizeDcArrays"/>.
        /// </summary>
        private static void InitDcArrays()
        {
            _dcArrayForms = new
#if !PORTABLE
                Hashtable();
#else
                Dictionary<string, PropertyOptions>();
#endif
            // Properties supposed to be a "Bag".
            var bagForm = new PropertyOptions { IsArray = true };
            _dcArrayForms["dc:contributor"] = bagForm;
            _dcArrayForms["dc:language"] = bagForm;
            _dcArrayForms["dc:publisher"] = bagForm;
            _dcArrayForms["dc:relation"] = bagForm;
            _dcArrayForms["dc:subject"] = bagForm;
            _dcArrayForms["dc:type"] = bagForm;
            // Properties supposed to be a "Seq".
            var seqForm = new PropertyOptions { IsArray = true, IsArrayOrdered = true };
            _dcArrayForms["dc:creator"] = seqForm;
            _dcArrayForms["dc:date"] = seqForm;
            // Properties supposed to be an "Alt" in alternative-text form.
            var altTextForm = new PropertyOptions { IsArray = true, IsArrayOrdered = true, IsArrayAlternate = true, IsArrayAltText = true };
            _dcArrayForms["dc:description"] = altTextForm;
            _dcArrayForms["dc:rights"] = altTextForm;
            _dcArrayForms["dc:title"] = altTextForm;
        }
    }
}
