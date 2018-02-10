// =================================================================================================
// ADOBE SYSTEMS INCORPORATED
// Copyright 2006 Adobe Systems Incorporated
// All Rights Reserved
//
// NOTICE:  Adobe permits you to use, modify, and distribute this file in accordance with the terms
// of the Adobe license agreement accompanying it.
// =================================================================================================


using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using XmpCore.Impl.XPath;
using XmpCore.Options;

namespace XmpCore.Impl
{
    /// <author>Stefan Makswit</author>
    /// <since>11.08.2006</since>
    public static class XmpUtils
    {
        private enum UnicodeKind
        {
            Normal = 0,
            Space = 1,
            Comma = 2,
            Semicolon = 3,
            Quote = 4,
            Control = 5
        }

        /// <param name="xmp">The XMP object containing the array to be catenated.</param>
        /// <param name="schemaNs">
        /// The schema namespace URI for the array. Must not be null or
        /// the empty string.
        /// </param>
        /// <param name="arrayName">
        /// The name of the array. May be a general path expression, must
        /// not be null or the empty string. Each item in the array must
        /// be a simple string value.
        /// </param>
        /// <param name="separator">
        /// The string to be used to separate the items in the catenated
        /// string. Defaults to &quot;; &quot;, ASCII semicolon and space
        /// (U+003B, U+0020).
        /// </param>
        /// <param name="quotes">
        /// The characters to be used as quotes around array items that
        /// contain a separator. Defaults to &apos;&quot;&apos;
        /// </param>
        /// <param name="allowCommas">Option flag to control the catenation.</param>
        /// <returns>Returns the string containing the catenated array items.</returns>
        /// <exception cref="XmpException">Forwards the Exceptions from the metadata processing</exception>
        public static string CatenateArrayItems(IXmpMeta xmp, string schemaNs, string arrayName, string separator, string quotes, bool allowCommas)
        {
            ParameterAsserts.AssertSchemaNs(schemaNs);
            ParameterAsserts.AssertArrayName(arrayName);
            ParameterAsserts.AssertImplementation(xmp);

            if (string.IsNullOrEmpty(separator))
                separator = "; ";
            if (string.IsNullOrEmpty(quotes))
                quotes = "\"";

            var xmpImpl = (XmpMeta)xmp;

            // Return an empty result if the array does not exist,
            // hurl if it isn't the right form.
            var arrayPath = XmpPathParser.ExpandXPath(schemaNs, arrayName);
            var arrayNode = XmpNodeUtils.FindNode(xmpImpl.GetRoot(), arrayPath, false, null);

            if (arrayNode == null)
                return string.Empty;
            if (!arrayNode.Options.IsArray || arrayNode.Options.IsArrayAlternate)
                throw new XmpException("Named property must be non-alternate array", XmpErrorCode.BadParam);

            // Make sure the separator is OK.
            CheckSeparator(separator);

            // Make sure the open and close quotes are a legitimate pair.
            var openQuote = quotes[0];
            var closeQuote = CheckQuotes(quotes, openQuote);

            // Build the result, quoting the array items, adding separators.
            // Hurl if any item isn't simple.
            var catenatedString = new StringBuilder();
            for (var it = arrayNode.IterateChildren(); it.HasNext();)
            {
                var currItem = (XmpNode)it.Next();

                if (currItem.Options.IsCompositeProperty)
                    throw new XmpException("Array items must be simple", XmpErrorCode.BadParam);

                var str = ApplyQuotes(currItem.Value, openQuote, closeQuote, allowCommas);
                catenatedString.Append(str);

                if (it.HasNext())
                    catenatedString.Append(separator);
            }
            return catenatedString.ToString();
        }

        /// <summary>
        /// See <see cref="XmpCore.XmpUtils.SeparateArrayItems(IXmpMeta, string, string, string, PropertyOptions, bool)"/>.
        /// </summary>
        /// <param name="xmp">The XMP object containing the array to be updated.</param>
        /// <param name="schemaNs">
        /// The schema namespace URI for the array. Must not be null or the empty string.
        /// </param>
        /// <param name="arrayName">
        /// The name of the array. May be a general path expression, must
        /// not be null or the empty string. Each item in the array must
        /// be a simple string value.
        /// </param>
        /// <param name="catedStr">The string to be separated into the array items.</param>
        /// <param name="arrayOptions">Option flags to control the separation.</param>
        /// <param name="preserveCommas">Flag if commas shall be preserved</param>
        /// <exception cref="XmpException">Forwards the Exceptions from the metadata processing</exception>
        public static void SeparateArrayItems(IXmpMeta xmp, string schemaNs, string arrayName, string catedStr, PropertyOptions arrayOptions, bool preserveCommas)
        {
            ParameterAsserts.AssertSchemaNs(schemaNs);
            ParameterAsserts.AssertArrayName(arrayName);

            if (catedStr == null)
                throw new XmpException("Parameter must not be null", XmpErrorCode.BadParam);

            ParameterAsserts.AssertImplementation(xmp);
            var xmpImpl = (XmpMeta)xmp;

            // Keep a zero value, has special meaning below.
            var arrayNode = SeparateFindCreateArray(schemaNs, arrayName, arrayOptions, xmpImpl);

            var arrayElementLimit = int.MaxValue;
            if (arrayNode != null && arrayOptions != null)
            {
                arrayElementLimit = arrayOptions.ArrayElementsLimit;
                if (arrayElementLimit == -1)
                    arrayElementLimit = int.MaxValue;
            }

            // Extract the item values one at a time, until the whole input string is done.
            var charKind = UnicodeKind.Normal;
            var ch = (char)0;
            var itemEnd = 0;
            var endPos = catedStr.Length;
            while (itemEnd < endPos)
            {
                // Skip any leading spaces and separation characters. Always skip commas here.
                // They can be kept when within a value, but not when alone between values.

                if (arrayNode.GetChildrenLength() >= arrayElementLimit)
                    break;

                int itemStart;
                for (itemStart = itemEnd; itemStart < endPos; itemStart++)
                {
                    ch = catedStr[itemStart];
                    charKind = ClassifyCharacter(ch);
                    if (charKind == UnicodeKind.Normal || charKind == UnicodeKind.Quote)
                        break;
                }

                if (itemStart >= endPos)
                    break;

                string itemValue;
                if (charKind != UnicodeKind.Quote)
                {
                    // This is not a quoted value. Scan for the end, create an array
                    // item from the substring.
                    for (itemEnd = itemStart; itemEnd < endPos; itemEnd++)
                    {
                        ch = catedStr[itemEnd];
                        charKind = ClassifyCharacter(ch);

                        if (charKind == UnicodeKind.Normal || charKind == UnicodeKind.Quote || (charKind == UnicodeKind.Comma && preserveCommas))
                            continue;

                        if (charKind != UnicodeKind.Space)
                            break;

                        if (itemEnd + 1 < endPos)
                        {
                            ch = catedStr[itemEnd + 1];
                            var nextKind = ClassifyCharacter(ch);
                            if (nextKind == UnicodeKind.Normal || nextKind == UnicodeKind.Quote || (nextKind == UnicodeKind.Comma && preserveCommas))
                                continue;
                        }
                        // Anything left?
                        break;
                    }
                    // Have multiple spaces, or a space followed by a
                    // separator.
                    itemValue = catedStr.Substring(itemStart, itemEnd - itemStart);
                }
                else
                {
                    // Accumulate quoted values into a local string, undoubling
                    // internal quotes that
                    // match the surrounding quotes. Do not undouble "unmatching"
                    // quotes.
                    var openQuote = ch;
                    var closeQuote = GetClosingQuote(openQuote);
                    itemStart++;
                    // Skip the opening quote;
                    var str = new StringBuilder();
                    for (itemEnd = itemStart; itemEnd < endPos; itemEnd++)
                    {
                        ch = catedStr[itemEnd];
                        charKind = ClassifyCharacter(ch);
                        if (charKind != UnicodeKind.Quote || !IsSurroundingQuote(ch, openQuote, closeQuote))
                        {
                            // This is not a matching quote, just append it to the
                            // item value.
                            str.Append(ch);
                        }
                        else
                        {
                            // This is a "matching" quote. Is it doubled, or the
                            // final closing quote?
                            // Tolerate various edge cases like undoubled opening
                            // (non-closing) quotes,
                            // or end of input.
                            char nextChar;
                            if (itemEnd + 1 < endPos)
                                nextChar = catedStr[itemEnd + 1];
                            else
                                nextChar = (char)0x3B;

                            if (ch == nextChar)
                            {
                                // This is doubled, copy it and skip the double.
                                str.Append(ch);
                                // Loop will add in charSize.
                                itemEnd++;
                            }
                            else if (!IsClosingQuote(ch, openQuote, closeQuote))
                            {
                                // This is an undoubled, non-closing quote, copy it.
                                str.Append(ch);
                            }
                            else
                            {
                                // This is an undoubled closing quote, skip it and
                                // exit the loop.
                                itemEnd++;
                                break;
                            }
                        }
                    }

                    itemValue = str.ToString();
                }

                // Add the separated item to the array.
                // Keep a matching old value in case it had separators.
                var foundIndex = -1;
                for (var oldChild = 1; oldChild <= arrayNode.GetChildrenLength(); oldChild++)
                {
                    if (arrayNode.GetChild(oldChild).Value == itemValue)
                    {
                        foundIndex = oldChild;
                        break;
                    }
                }

                if (foundIndex < 0)
                    arrayNode.AddChild(new XmpNode(XmpConstants.ArrayItemName, itemValue, null));
                // <#AdobePrivate>
                // else
                // {
                //     newItem = arrayNode.getChild(foundIndex);
                //     // Don't match again, let duplicates be seen.
                //     arrayNode.getChild(foundIndex).setValue(null);
                // }
                // </#AdobePrivate>

            }
        }

        /// <summary>Utility to find or create the array used by <c>separateArrayItems()</c>.</summary>
        /// <param name="schemaNs">a the namespace fo the array</param>
        /// <param name="arrayName">the name of the array</param>
        /// <param name="arrayOptions">the options for the array if newly created</param>
        /// <param name="xmp">the xmp object</param>
        /// <returns>Returns the array node.</returns>
        /// <exception cref="XmpException">Forwards exceptions</exception>
        private static XmpNode SeparateFindCreateArray(string schemaNs, string arrayName, PropertyOptions arrayOptions, XmpMeta xmp)
        {
            arrayOptions = XmpNodeUtils.VerifySetOptions(arrayOptions, null);

            if (!arrayOptions.IsOnlyArrayOptions)
                throw new XmpException("Options can only provide array form", XmpErrorCode.BadOptions);

            // Find the array node, make sure it is OK. Move the current children
            // aside, to be readded later if kept.
            var arrayPath = XmpPathParser.ExpandXPath(schemaNs, arrayName);
            var arrayNode = XmpNodeUtils.FindNode(xmp.GetRoot(), arrayPath, false, null);
            if (arrayNode != null)
            {
                // The array exists, make sure the form is compatible. Zero
                // arrayForm means take what exists.
                var arrayForm = arrayNode.Options;

                if (!arrayForm.IsArray || arrayForm.IsArrayAlternate)
                    throw new XmpException("Named property must be non-alternate array", XmpErrorCode.BadXPath);
                if (arrayOptions.EqualArrayTypes(arrayForm))
                    throw new XmpException("Mismatch of specified and existing array form", XmpErrorCode.BadXPath);
            }
            else
            {
                // *** Right error?
                // The array does not exist, try to create it.
                // don't modify the options handed into the method
                arrayOptions.IsArray = true;
                arrayNode = XmpNodeUtils.FindNode(xmp.GetRoot(), arrayPath, true, arrayOptions);
                if (arrayNode == null)
                    throw new XmpException("Failed to create named array", XmpErrorCode.BadXPath);
            }
            return arrayNode;
        }

        /// <param name="xmp">The XMP object containing the properties to be removed.</param>
        /// <param name="schemaNs">
        /// Optional schema namespace URI for the properties to be
        /// removed.
        /// </param>
        /// <param name="propName">Optional path expression for the property to be removed.</param>
        /// <param name="doAllProperties">
        /// Option flag to control the deletion: do internal properties in
        /// addition to external properties.
        /// </param>
        /// <param name="includeAliases">
        /// Option flag to control the deletion: Include aliases in the
        /// "named schema" case above.
        /// </param>
        /// <exception cref="XmpException">If metadata processing fails</exception>
        public static void RemoveProperties(IXmpMeta xmp, string schemaNs, string propName, bool doAllProperties, bool includeAliases)
        {
            ParameterAsserts.AssertImplementation(xmp);

            var xmpImpl = (XmpMeta)xmp;
            if (!string.IsNullOrEmpty(propName))
            {
                // Remove just the one indicated property. This might be an alias,
                // the named schema might not actually exist. So don't lookup the
                // schema node.
                if (string.IsNullOrEmpty(schemaNs))
                    throw new XmpException("Property name requires schema namespace", XmpErrorCode.BadParam);

                var expPath = XmpPathParser.ExpandXPath(schemaNs, propName);
                var propNode = XmpNodeUtils.FindNode(xmpImpl.GetRoot(), expPath, false, null);
                if (propNode != null)
                {
                    if (doAllProperties || !Utils.IsInternalProperty(expPath.GetSegment(XmpPath.StepSchema).Name, expPath.GetSegment(XmpPath.StepRootProp).Name))
                    {
                        var parent = propNode.Parent;
                        parent.RemoveChild(propNode);
                        if (parent.Options.IsSchemaNode && !parent.HasChildren)
                        {
                            // remove empty schema node
                            parent.Parent.RemoveChild(parent);
                        }
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(schemaNs))
                {
                    // Remove all properties from the named schema. Optionally include
                    // aliases, in which case
                    // there might not be an actual schema node.
                    // XMP_NodePtrPos schemaPos;
                    var schemaNode = XmpNodeUtils.FindSchemaNode(xmpImpl.GetRoot(), schemaNs, false);

                    if (schemaNode != null && RemoveSchemaChildren(schemaNode, doAllProperties))
                        xmpImpl.GetRoot().RemoveChild(schemaNode);

                    if (includeAliases)
                    {
                        // We're removing the aliases also. Look them up by their namespace prefix.
                        // But that takes more code and the extra speed isn't worth it.
                        // Lookup the XMP node from the alias, to make sure the actual exists.
                        foreach (var info in XmpMetaFactory.SchemaRegistry.FindAliases(schemaNs))
                        {
                            var path = XmpPathParser.ExpandXPath(info.Namespace, info.PropName);
                            var actualProp = XmpNodeUtils.FindNode(xmpImpl.GetRoot(), path, false, null);
                            actualProp?.Parent.RemoveChild(actualProp);
                        }
                    }
                }
                else
                {
                    // Remove all appropriate properties from all schema. In this case
                    // we don't have to be
                    // concerned with aliases, they are handled implicitly from the
                    // actual properties.
                    for (var it = xmpImpl.GetRoot().IterateChildren(); it.HasNext();)
                    {
                        var schema = (XmpNode)it.Next();
                        if (RemoveSchemaChildren(schema, doAllProperties))
                            it.Remove();
                    }
                }
            }
        }

        /// <param name="source">The source XMP object.</param>
        /// <param name="destination">The destination XMP object.</param>
        /// <param name="doAllProperties">Do internal properties in addition to external properties.</param>
        /// <param name="replaceOldValues">Replace the values of existing properties.</param>
        /// <param name="deleteEmptyValues">Delete destination values if source property is empty.</param>
        /// <exception cref="XmpException">Forwards the Exceptions from the metadata processing</exception>
        public static void AppendProperties(IXmpMeta source, IXmpMeta destination, bool doAllProperties, bool replaceOldValues, bool deleteEmptyValues)
        {
            ParameterAsserts.AssertImplementation(source);
            ParameterAsserts.AssertImplementation(destination);

            var src = (XmpMeta)source;
            var dest = (XmpMeta)destination;
            for (var it = src.GetRoot().IterateChildren(); it.HasNext();)
            {
                var sourceSchema = (XmpNode)it.Next();

                // Make sure we have a destination schema node
                var destSchema = XmpNodeUtils.FindSchemaNode(dest.GetRoot(), sourceSchema.Name, false);
                var createdSchema = false;

                if (destSchema == null)
                {
                    destSchema = new XmpNode(sourceSchema.Name, sourceSchema.Value, new PropertyOptions { IsSchemaNode = true });
                    dest.GetRoot().AddChild(destSchema);
                    createdSchema = true;
                }

                // Process the source schema's children.
                for (var ic = sourceSchema.IterateChildren(); ic.HasNext();)
                {
                    var sourceProp = (XmpNode)ic.Next();
                    if (doAllProperties || !Utils.IsInternalProperty(sourceSchema.Name, sourceProp.Name))
                        AppendSubtree(dest, sourceProp, destSchema, false, replaceOldValues, deleteEmptyValues);
                }

                if (!destSchema.HasChildren && (createdSchema || deleteEmptyValues))
                {
                    // Don't create an empty schema / remove empty schema.
                    dest.GetRoot().RemoveChild(destSchema);
                }
            }
        }

        /// <summary>Remove all schema children according to the flag <c>doAllProperties</c>.</summary>
        /// <remarks>Empty schemas are automatically remove by <c>XMPNode</c>.</remarks>
        /// <param name="schemaNode">a schema node</param>
        /// <param name="doAllProperties">flag if all properties or only externals shall be removed.</param>
        /// <returns>Returns true if the schema is empty after the operation.</returns>
        private static bool RemoveSchemaChildren(XmpNode schemaNode, bool doAllProperties)
        {
            for (var it = schemaNode.IterateChildren(); it.HasNext();)
            {
                var currProp = (XmpNode)it.Next();
                if (doAllProperties || !Utils.IsInternalProperty(schemaNode.Name, currProp.Name))
                {
                    it.Remove();
                }
            }
            return !schemaNode.HasChildren;
        }

        /// <param name="destXmp">The destination XMP object.</param>
        /// <param name="sourceNode">the source node</param>
        /// <param name="destParent">the parent of the destination node</param>
        /// <param name="mergeCompound"></param>
        /// <param name="replaceOldValues">Replace the values of existing properties.</param>
        /// <param name="deleteEmptyValues">flag if properties with empty values should be deleted in the destination object.</param>
        /// <exception cref="XmpException" />
        private static void AppendSubtree(XmpMeta destXmp, XmpNode sourceNode, XmpNode destParent, bool mergeCompound, bool replaceOldValues, bool deleteEmptyValues)
        {
            var destNode = XmpNodeUtils.FindChildNode(destParent, sourceNode.Name, false);
            var valueIsEmpty = false;

            //if (deleteEmptyValues)
            valueIsEmpty = sourceNode.Options.IsSimple ? string.IsNullOrEmpty(sourceNode.Value) : !sourceNode.HasChildren;

            if (valueIsEmpty)
            {
                if (deleteEmptyValues && destNode != null)
                    destParent.RemoveChild(destNode);
                return; // ! Done, empty values are either ignored or cause deletions.
            }

            //else
            //{
            if (destNode == null)
            {
                // The one easy case, the destination does not exist.
                var tempNode = (XmpNode)sourceNode.Clone(true);
                if (tempNode != null)
                    destParent.AddChild(tempNode);
                return;
            }

            var sourceForm = sourceNode.Options;
            var replaceThis = replaceOldValues; // ! Don't modify replaceOld, it gets passed to inner calls.
            if (mergeCompound && (!sourceForm.IsSimple))
                replaceThis = false;

            //else
            //{
            if (replaceThis)
            {
                // The destination exists and should be replaced.
                //XmpMeta.SetNode(destNode, sourceNode.Value, sourceNode.Options, true);
                destParent.RemoveChild(destNode);
                //destNode = (XmpNode)sourceNode.Clone();
                var tempNode = (XmpNode)sourceNode.Clone(true);
                if (tempNode != null)
                    destParent.AddChild(tempNode);
                return;
            }

            //else
            //{
            // The destination exists and is not totally replaced. Structs and arrays are merged.
            var destForm = destNode.Options;
            //if (!Equals(sourceForm, destForm))
            if (sourceForm.GetOptions() != destForm.GetOptions() || sourceForm.IsSimple)
            {
                return;
            }

            if (sourceForm.IsStruct)
            {
                // To merge a struct process the fields recursively. E.g. add simple missing fields.
                // The recursive call to AppendSubtree will handle deletion for fields with empty
                // values.
                for (var it = sourceNode.IterateChildren(); it.HasNext();)
                {
                    var sourceField = (XmpNode)it.Next();
                    AppendSubtree(destXmp, sourceField, destNode, mergeCompound, replaceOldValues, deleteEmptyValues);
                    if (deleteEmptyValues && !destNode.HasChildren)
                        destParent.RemoveChild(destNode);
                }
            }
            else if (sourceForm.IsArrayAltText)
            {
                // Merge AltText arrays by the "xml:lang" qualifiers. Make sure x-default is first.
                // Make a special check for deletion of empty values. Meaningful in AltText arrays
                // because the "xml:lang" qualifier provides unambiguous source/dest correspondence.
                for (var it = sourceNode.IterateChildren(); it.HasNext();)
                {
                    var sourceItem = (XmpNode)it.Next();

                    if (!sourceItem.HasQualifier || sourceItem.GetQualifier(1).Name != XmpConstants.XmlLang)
                        continue;

                    var destIndex = XmpNodeUtils.LookupLanguageItem(destNode, sourceItem.GetQualifier(1).Value);
                    //if (deleteEmptyValues && string.IsNullOrEmpty(sourceItem.Value))
                    if (string.IsNullOrEmpty(sourceItem.Value))
                    {
                        if (deleteEmptyValues && destIndex != -1)
                        {
                            destNode.RemoveChild(destIndex);
                            if (!destNode.HasChildren)
                                destParent.RemoveChild(destNode);
                        }
                    }
                    else
                    {
                        if (destIndex == -1)
                        {
                            // Not replacing, keep the existing item.
                            if (sourceItem.GetQualifier(1).Value != XmpConstants.XDefault || !destNode.HasChildren)
                            {
                                XmpNode tempNode = (XmpNode)sourceItem.Clone(true);
                                if (tempNode != null)
                                    destNode.AddChild(tempNode);
                                //sourceItem.CloneSubtree(destNode);
                            }
                            else
                            {
                                var destItem = new XmpNode(sourceItem.Name, sourceItem.Value, sourceItem.Options);
                                sourceItem.CloneSubtree(destItem, true);
                                destNode.AddChild(1, destItem);
                            }
                        }
                        else
                        {
                            if (replaceOldValues)
                                destNode.GetChild(destIndex).Value = sourceItem.Value;
                        }

                    }
                }
            }
            else if (sourceForm.IsArray)
            {
                // Merge other arrays by item values. Don't worry about order or duplicates. Source
                // items with empty values do not cause deletion, that conflicts horribly with
                // merging.
                for (var children = sourceNode.IterateChildren(); children.HasNext();)
                {
                    var sourceItem = (XmpNode)children.Next();

                    var match = false;
                    for (var id = destNode.IterateChildren(); id.HasNext();)
                    {
                        var destItem = (XmpNode)id.Next();
                        if (ItemValuesMatch(sourceItem, destItem))
                        {
                            match = true;
                            break;
                        }
                    }
                    if (!match)
                    {
                        XmpNode tempNode = (XmpNode)sourceItem.Clone(true);
                        if (tempNode != null)
                            destNode.AddChild(tempNode);
                    }
                }
            }
            //}
            //}
            //}
        }

        /// <summary>Compares two nodes including its children and qualifier.</summary>
        /// <param name="leftNode">an <c>XMPNode</c></param>
        /// <param name="rightNode">an <c>XMPNode</c></param>
        /// <returns>Returns true if the nodes are equal, false otherwise.</returns>
        /// <exception cref="XmpException">Forwards exceptions to the calling method.</exception>
        private static bool ItemValuesMatch(XmpNode leftNode, XmpNode rightNode)
        {
            var leftForm = leftNode.Options;
            var rightForm = rightNode.Options;

            if (!leftForm.Equals(rightForm))
                return false;

            if (leftForm.IsSimple)
            {
                // Simple nodes, check the values and xml:lang qualifiers.
                if (leftNode.Value != rightNode.Value)
                    return false;
                if (leftNode.Options.HasLanguage != rightNode.Options.HasLanguage)
                    return false;
                if (leftNode.Options.HasLanguage && leftNode.GetQualifier(1).Value != rightNode.GetQualifier(1).Value)
                    return false;
            }
            else
            {
                if (leftForm.IsStruct)
                {
                    // Struct nodes, see if all fields match, ignoring order.
                    if (leftNode.GetChildrenLength() != rightNode.GetChildrenLength())
                        return false;

                    for (var it = leftNode.IterateChildren(); it.HasNext();)
                    {
                        var leftField = (XmpNode)it.Next();
                        var rightField = XmpNodeUtils.FindChildNode(rightNode, leftField.Name, false);
                        if (rightField == null || !ItemValuesMatch(leftField, rightField))
                            return false;
                    }
                }
                else
                {
                    // Array nodes, see if the "leftNode" values are present in the
                    // "rightNode", ignoring order, duplicates,
                    // and extra values in the rightNode-> The rightNode is the
                    // destination for AppendProperties.
                    Debug.Assert(leftForm.IsArray);
                    for (var il = leftNode.IterateChildren(); il.HasNext();)
                    {
                        var leftItem = (XmpNode)il.Next();
                        var match = false;

                        for (var ir = rightNode.IterateChildren(); ir.HasNext();)
                        {
                            var rightItem = (XmpNode)ir.Next();
                            if (ItemValuesMatch(leftItem, rightItem))
                            {
                                match = true;
                                break;
                            }
                        }

                        if (!match)
                            return false;
                    }
                }
            }
            return true; // All of the checks passed
        }

        public static void DuplicateSubtree(IXmpMeta source, IXmpMeta dest, string sourceNS, string sourceRoot, string destNS, string destRoot, PropertyOptions options)
        {
            var fullSourceTree = false;
            var fullDestTree = false;
            XmpPath sourcePath;
            XmpPath destPath;
            XmpNode sourceNode;
            XmpNode destNode;

            ParameterAsserts.AssertNotNull(source);
            ParameterAsserts.AssertSchemaNs(sourceNS);
            ParameterAsserts.AssertSchemaNs(sourceRoot);
            ParameterAsserts.AssertNotNull(dest);
            ParameterAsserts.AssertNotNull(destNS);
            ParameterAsserts.AssertNotNull(destRoot);

            if (destNS.Length == 0)
                destNS = sourceNS;

            if (destRoot.Length == 0)
                destRoot = sourceRoot;

            if (sourceNS == "*")
                fullSourceTree = true;

            if (destNS == "*")
                fullDestTree = true;

            if (source == dest && (fullSourceTree || fullDestTree))
                throw new XmpException("Can't duplicate tree onto itself", XmpErrorCode.BadParam);

            if (fullSourceTree && fullDestTree)
                throw new XmpException("Use Clone for full tree to full tree", XmpErrorCode.BadParam);

            if (fullSourceTree)
            {
                destPath = XmpPathParser.ExpandXPath(destNS, destRoot);
                var destImpl = (XmpMeta)dest;
                destNode = XmpNodeUtils.FindNode(destImpl.GetRoot(), destPath, false, null);
                if (destNode == null || !destNode.Options.IsStruct)
                    throw new XmpException("Destination must be an existing struct", XmpErrorCode.BadXPath);

                if (destNode.HasChildren)
                {
                    if (options != null && ((options.GetOptions() & PropertyOptions.DeleteExisting) != 0))
                        destNode.RemoveChildren();
                    else
                        throw new XmpException("Destination must be an empty struct", XmpErrorCode.BadXPath);
                }

                var sourceImpl = (XmpMeta)source;
                for (int schemaNum = 1, schemaLim = sourceImpl.GetRoot().GetChildrenLength(); schemaNum <= schemaLim; ++schemaNum )
                {
                    var currSchema = sourceImpl.GetRoot().GetChild(schemaNum);

                    for (int propNum = 1, propLim = currSchema.GetChildrenLength(); propNum <= propLim; ++propNum )
                    {
                        sourceNode = currSchema.GetChild(propNum);
                        destNode.AddChild((XmpNode)sourceNode.Clone(false));

                        /*XMP_Node * copyNode = new XMP_Node ( destNode, sourceNode->name, sourceNode->value, sourceNode->options );
                        destNode->children.push_back ( copyNode );
                        CloneOffspring ( sourceNode, copyNode );*/  //implemented above
                    }
                }
            }
            else if (fullDestTree)
            {
                // The source node must be an existing struct, copy all of the fields to the dest top level.
                var srcImpl = (XmpMeta)source;
                var dstImpl = (XmpMeta)dest;
                sourcePath = XmpPathParser.ExpandXPath(sourceNS, sourceRoot);
                sourceNode = XmpNodeUtils.FindNode(srcImpl.GetRoot() , sourcePath , false, null);

                if (sourceNode == null || !sourceNode.Options.IsStruct)
                    throw new XmpException("Source must be an existing struct", XmpErrorCode.BadXPath);

                destNode = dstImpl.GetRoot();

                if (destNode.HasChildren )
                {
                    if (options != null && ((options.GetOptions() & PropertyOptions.DeleteExisting) != 0))
                        destNode.RemoveChildren();
                    else
                        throw new XmpException("Source must be an existing struct", XmpErrorCode.BadXPath);
                }

                for (int fieldNum = 1, fieldLim = sourceNode.GetChildrenLength(); fieldNum <= fieldLim; ++fieldNum )
                {
                    var currField = sourceNode.GetChild(fieldNum);

                    var colonPos = currField.Name.IndexOf(':');
                    if (colonPos == -1)
                        continue;

                    var nsPrefix = currField.Name.Substring(0, colonPos+1);

                    var nsRegister = XmpMetaFactory.SchemaRegistry;
                    var nsURI = nsRegister.GetNamespaceUri(nsPrefix);

                    if (nsURI == null)
                        throw new XmpException("Source field namespace is not global", XmpErrorCode.BadSchema);

                    var destSchema = XmpNodeUtils.FindSchemaNode(dstImpl.GetRoot(), nsURI, true);
                    if (destSchema == null)
                        throw new XmpException("Failed to find destination schema", XmpErrorCode.BadSchema);

                    destSchema.AddChild((XmpNode) currField.Clone(false));
                }
            }
            else
            {
                sourcePath = XmpPathParser.ExpandXPath(sourceNS, sourceRoot);
                destPath = XmpPathParser.ExpandXPath(destNS, destRoot);
                var sourceImpl = (XmpMeta)source;
                var destImpl = (XmpMeta)dest;
                sourceNode = XmpNodeUtils.FindNode(sourceImpl.GetRoot(), sourcePath, false, null);
                if (sourceNode == null)
                    throw new XmpException("Can't find source subtree", XmpErrorCode.BadXPath);
                destNode = XmpNodeUtils.FindNode(destImpl.GetRoot(), destPath, false, null);
                if (destNode != null)
                    throw new XmpException("Destination subtree must not exist", XmpErrorCode.BadXPath);
                destNode = XmpNodeUtils.FindNode(destImpl.GetRoot(), destPath, true, null);
                if (destNode == null)
                    throw new XmpException("Can't create destination root node", XmpErrorCode.BadXPath);
                if (source == dest)
                {
                    for (XmpNode testNode = destNode; testNode != null; testNode = testNode.Parent)
                    {
                        if (testNode == sourceNode)
                        {
                            // *** delete the just-created dest root node
                            throw new XmpException("Destination subtree is within the source subtree", XmpErrorCode.BadXPath);
                        }
                    }
                }
                destNode.Value = sourceNode.Value;
                destNode.Options = sourceNode.Options;
                sourceNode.CloneSubtree(destNode, false);
            }
        }

        /// <summary>Make sure the separator is OK.</summary>
        /// <remarks>
        /// Separators must be one semicolon surrounded by zero or more spaces. Any of the recognized semicolons or spaces are allowed.
        /// </remarks>
        /// <param name="separator" />
        /// <exception cref="XmpException" />
        private static void CheckSeparator(string separator)
        {
            var haveSemicolon = false;

            foreach (var t in separator)
            {
                var charKind = ClassifyCharacter(t);
                if (charKind == UnicodeKind.Semicolon)
                {
                    if (haveSemicolon)
                        throw new XmpException("Separator can have only one semicolon", XmpErrorCode.BadParam);
                    haveSemicolon = true;
                }
                else
                {
                    if (charKind != UnicodeKind.Space)
                        throw new XmpException("Separator can have only spaces and one semicolon", XmpErrorCode.BadParam);
                }
            }

            if (!haveSemicolon)
                throw new XmpException("Separator must have one semicolon", XmpErrorCode.BadParam);
        }

        /// <summary>
        /// Make sure the open and close quotes are a legitimate pair and return the
        /// correct closing quote or an exception.
        /// </summary>
        /// <param name="quotes">opened and closing quote in a string</param>
        /// <param name="openQuote">the open quote</param>
        /// <returns>Returns a corresponding closing quote.</returns>
        /// <exception cref="XmpException" />
        private static char CheckQuotes(string quotes, char openQuote)
        {
            char closeQuote;
            var charKind = ClassifyCharacter(openQuote);
            if (charKind != UnicodeKind.Quote)
                throw new XmpException("Invalid quoting character", XmpErrorCode.BadParam);

            if (quotes.Length == 1)
            {
                closeQuote = openQuote;
            }
            else
            {
                closeQuote = quotes[1];
                charKind = ClassifyCharacter(closeQuote);
                if (charKind != UnicodeKind.Quote)
                    throw new XmpException("Invalid quoting character", XmpErrorCode.BadParam);
            }

            if (closeQuote != GetClosingQuote(openQuote))
                throw new XmpException("Mismatched quote pair", XmpErrorCode.BadParam);

            return closeQuote;
        }

        /// <summary>
        /// Classifies the character into normal chars, spaces, semicola, quotes,
        /// control chars.
        /// </summary>
        /// <param name="ch">a char</param>
        /// <returns>Return the character kind.</returns>
        private static UnicodeKind ClassifyCharacter(char ch)
        {
            if (Spaces.IndexOf(ch) >= 0 || (0x2000 <= ch && ch <= 0x200B))
                return UnicodeKind.Space;
            if (Commas.IndexOf(ch) >= 0)
                return UnicodeKind.Comma;
            if (Semicola.IndexOf(ch) >= 0)
                return UnicodeKind.Semicolon;
            if (Quotes.IndexOf(ch) >= 0 || (0x3008 <= ch && ch <= 0x300F) || (0x2018 <= ch && ch <= 0x201F))
                return UnicodeKind.Quote;
            if (ch < 0x0020 || Controls.IndexOf(ch) >= 0)
                return UnicodeKind.Control;

            // Assume typical case.
            return UnicodeKind.Normal;
        }

        /// <param name="openQuote">the open quote char</param>
        /// <returns>Returns the matching closing quote for an open quote.</returns>
        private static char GetClosingQuote(char openQuote)
        {
            switch (openQuote)
            {
                // ! U+0022 is both opening and closing.
                case (char)0x0022:
                    return (char)0x0022;
                // Not interpreted as brackets anymore
//              case 0x005B:
//                  return 0x005D;
                case (char)0x00AB:
                    return (char)0x00BB;
                case (char)0x00BB:
                    // ! U+00AB and U+00BB are reversible.
                    return (char)0x00AB;
                case (char)0x2015:
                    return (char)0x2015;
                case (char)0x2018:
                    // ! U+2015 is both opening and closing.
                    return (char)0x2019;
                case (char)0x201A:
                    return (char)0x201B;
                case (char)0x201C:
                    return (char)0x201D;
                case (char)0x201E:
                    return (char)0x201F;
                case (char)0x2039:
                    return (char)0x203A;
                case (char)0x203A:
                    // ! U+2039 and U+203A are reversible.
                    return (char)0x2039;
                case (char)0x3008:
                    return (char)0x3009;
                case (char)0x300A:
                    return (char)0x300B;
                case (char)0x300C:
                    return (char)0x300D;
                case (char)0x300E:
                    return (char)0x300F;
                case (char)0x301D:
                    return (char)0x301F;
                default:
                    // ! U+301E also closes U+301D.
                    return (char)0;
            }
        }

        /// <summary>Add quotes to the item.</summary>
        /// <param name="item">the array item</param>
        /// <param name="openQuote">the open quote character</param>
        /// <param name="closeQuote">the closing quote character</param>
        /// <param name="allowCommas">flag if commas are allowed</param>
        /// <returns>Returns the value in quotes.</returns>
        private static string ApplyQuotes(string item, char openQuote, char closeQuote, bool allowCommas)
        {
            if (item == null)
                item = string.Empty;

            var prevSpace = false;

            // See if there are any separators in the value. Stop at the first occurrence. This is a bit
            // tricky in order to make typical typing work conveniently. The purpose of applying quotes
            // is to preserve the values when splitting them back apart. That is CatenateContainerItems
            // and SeparateContainerItems must round trip properly. For the most part we only look for
            // separators here. Internal quotes, as in -- Irving "Bud" Jones -- won't cause problems in
            // the separation. An initial quote will though, it will make the value look quoted.
            int i;
            for (i = 0; i < item.Length; i++)
            {
                var ch = item[i];
                var charKind = ClassifyCharacter(ch);
                if (i == 0 && charKind == UnicodeKind.Quote)
                    break;

                if (charKind == UnicodeKind.Space)
                {
                    // Multiple spaces are a separator.
                    if (prevSpace)
                        break;
                    prevSpace = true;
                }
                else
                {
                    prevSpace = false;
                    if (charKind == UnicodeKind.Semicolon || charKind == UnicodeKind.Control || (charKind == UnicodeKind.Comma && !allowCommas))
                        break;
                }
            }

            if (i < item.Length)
            {
                // Create a quoted copy, doubling any internal quotes that match the
                // outer ones. Internal quotes did not stop the "needs quoting"
                // search, but they do need
                // doubling. So we have to rescan the front of the string for
                // quotes. Handle the special
                // case of U+301D being closed by either U+301E or U+301F.
                var newItem = new StringBuilder(item.Length + 2);
                int splitPoint;
                for (splitPoint = 0; splitPoint <= i; splitPoint++)
                {
                    if (ClassifyCharacter(item[i]) == UnicodeKind.Quote)
                        break;
                }

                // Copy the leading "normal" portion.
                newItem.Append(openQuote).Append(item.Substring(0, splitPoint - 0));
                for (var charOffset = splitPoint; charOffset < item.Length; charOffset++)
                {
                    newItem.Append(item[charOffset]);
                    if (ClassifyCharacter(item[charOffset]) == UnicodeKind.Quote && IsSurroundingQuote(item[charOffset], openQuote, closeQuote))
                        newItem.Append(item[charOffset]);
                }

                newItem.Append(closeQuote);
                item = newItem.ToString();
            }

            return item;
        }

        /// <param name="ch">a character</param>
        /// <param name="openQuote">the opening quote char</param>
        /// <param name="closeQuote">the closing quote char</param>
        /// <returns>Return it the character is a surrounding quote.</returns>
        private static bool IsSurroundingQuote(char ch, char openQuote, char closeQuote)
        {
            return ch == openQuote || IsClosingQuote(ch, openQuote, closeQuote);
        }

        /// <param name="ch">a character</param>
        /// <param name="openQuote">the opening quote char</param>
        /// <param name="closeQuote">the closing quote char</param>
        /// <returns>Returns true if the character is a closing quote.</returns>
        private static bool IsClosingQuote(char ch, char openQuote, char closeQuote)
        {
            return ch == closeQuote || openQuote == 0x301D && ch == 0x301E || ch == 0x301F;
        }

        /// <summary>
        /// <list type="bullet">
        ///   <item>U+0022 ASCII space</item>
        ///   <item>U+3000, ideographic space</item>
        ///   <item>U+303F, ideographic half fill space</item>
        ///   <item>U+2000..U+200B, en quad through zero width space</item>
        /// </list>
        /// </summary>
        private const string Spaces = "\u0020\u3000\u303F";

        /// <summary>
        /// <list type="bullet">
        ///   <item>U+002C, ASCII comma</item>
        ///   <item>U+FF0C, full width comma</item>
        ///   <item>U+FF64, half width ideographic comma</item>
        ///   <item>U+FE50, small comma</item>
        ///   <item>U+FE51, small ideographic comma</item>
        ///   <item>U+3001, ideographic comma</item>
        ///   <item>U+060C, Arabic comma</item>
        ///   <item>U+055D, Armenian comma</item>
        /// </list>
        /// </summary>
        private const string Commas = "\u002C\uFF0C\uFF64\uFE50\uFE51\u3001\u060C\u055D";

        /// <summary>
        /// <list type="bullet">
        ///   <item>U+003B, ASCII semicolon</item>
        ///   <item>U+FF1B, full width semicolon</item>
        ///   <item>U+FE54, small semicolon</item>
        ///   <item>U+061B, Arabic semicolon</item>
        ///   <item>U+037E, Greek "semicolon" (really a question mark)</item>
        /// </list>
        /// </summary>
        private const string Semicola = "\u003B\uFF1B\uFE54\u061B\u037E";

        /// <summary>
        /// <list type="bullet">
        ///   <item>U+0022 ASCII quote</item>
        ///   <item>U+00AB and U+00BB, guillemet quotes</item>
        ///   <item>U+3008..U+300F, various quotes</item>
        ///   <item>U+301D..U+301F, double prime quotes</item>
        ///   <item>U+2015, dash quote</item>
        ///   <item>U+2018..U+201F, various quotes</item>
        ///   <item>U+2039 and U+203A, guillemet quotes</item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// The square brackets are not interpreted as quotes anymore (bug #2674672)
        /// (ASCII '[' (0x5B) and ']' (0x5D) are used as quotes in Chinese and
        /// Korean.)<br />
        /// </remarks>
        private const string Quotes = "\"\u00AB\u00BB\u301D\u301E\u301F\u2015\u2039\u203A";

        /// <summary>
        /// <list type="bullet">
        ///   <item>U+0000..U+001F ASCII controls</item>
        ///   <item>U+2028, line separator</item>
        ///   <item>U+2029, paragraph separator</item>
        /// </list>
        /// </summary>
        private const string Controls = "\u2028\u2029";
        // "\"\u005B\u005D\u00AB\u00BB\u301D\u301E\u301F\u2015\u2039\u203A";


        /// <summary>Moves the specified Property from one Meta to another.</summary>
        /// <param name="stdXMP">Meta Object from where the property needs to move</param>
        /// <param name="extXMP">Meta Object to where the property needs to move</param>
        /// <param name="schemaURI">Schema of the specified property</param>
        /// <param name="propName">Name of the property</param>
        /// <returns>true in case of success otherwise false.</returns>
        static bool MoveOneProperty(XmpMeta stdXMP, XmpMeta extXMP, string schemaURI, string propName)
        {
            XmpNode propNode = null;

            XmpNode stdSchema = XmpNodeUtils.FindSchemaNode(stdXMP.GetRoot(), schemaURI, false);
            if (stdSchema != null)
                propNode = XmpNodeUtils.FindChildNode(stdSchema, propName, false);

            if (propNode == null)
                return false;

            XmpNode extSchema = XmpNodeUtils.FindSchemaNode(extXMP.GetRoot(), schemaURI, true);

            propNode.Parent = extSchema;

            extSchema.IsImplicit = false;
            extSchema.AddChild(propNode);

            stdSchema.RemoveChild(propNode);

            if (stdSchema.HasChildren == false)
            {
                XmpNode xmpTree = stdSchema.Parent;
                xmpTree.RemoveChild(stdSchema);
            }

            return true;
        }

        /// <summary>estimates the size of an xmp node</summary>
        /// <param name="xmpNode">XMP Node Object</param>
        /// <returns>the estimated size of the node</returns>
        static int EstimateSizeForJPEG(XmpNode xmpNode)
        {
            int estSize = 0;
            int nameSize = xmpNode.Name.Length;

            bool includeName = (!xmpNode.Options.IsArray);

            if (xmpNode.Options.IsSimple)
            {
                if (includeName)
                    estSize += (nameSize + 3); // Assume attribute form.
                estSize += xmpNode.Value.Length;
            }
            else if (xmpNode.Options.IsArray)
            {
                // The form of the value portion is:
                // <rdf:Xyz><rdf:li>...</rdf:li>...</rdf:Xyx>
                if (includeName)
                    estSize += (2 * nameSize + 5);
                int arraySize = xmpNode.GetChildrenLength();
                estSize += 9 + 10; // The rdf:Xyz tags.
                estSize += arraySize * (8 + 9); // The rdf:li tags.
                for (int i = 1; i <= arraySize; ++i)
                {
                    estSize += EstimateSizeForJPEG(xmpNode.GetChild(i));
                }
            }
            else
            {
                // The form is: <headTag
                // rdf:parseType="Resource">...fields...</tailTag>
                if (includeName)
                    estSize += (2 * nameSize + 5);
                estSize += 25; // The rdf:parseType="Resource" attribute.
                int fieldCount = xmpNode.GetChildrenLength();
                for (int i = 1; i <= fieldCount; ++i)
                {
                    estSize += EstimateSizeForJPEG(xmpNode.GetChild(i));
                }
            }
            return estSize;
        }

        /// <summary>Utility function for placing objects in a Map. It behaves like a multi map.</summary>
        /// <param name="multiMap">A Map object which takes int as a key and list of list of string as value</param>
        /// <param name="key">A key for the map</param>
        /// <param name="stringPair">A value for the map</param>
        private static void PutObjectsInMultiMap(SortedDictionary<int, List<List<string>>> multiMap, int key, List<string> stringPair)
        {
            if (multiMap == null)
                return;
            List<List<string>> tempList; // multiMap[key];
            //if (tempList == null)
            if (!multiMap.TryGetValue(key, out tempList))
            {
                tempList = new List<List<string>>();
                multiMap[key] = tempList;
            }
            tempList.Add(stringPair);
        }

        /// <summary>Utility function for retrieving biggest entry in the multimap</summary>
        /// <remarks>see EstimateSizeForJPEG for size calculation</remarks>
        /// <param name="multiMap">A Map object which takes int as a key and list of list of string as value</param>
        /// <returns>the list with the maximum size.</returns>
        private static List<string> GetBiggestEntryInMultiMap(SortedDictionary<int, List<List<string>>> multiMap)
        {
            if (multiMap == null || multiMap.Count == 0)
                return null;

            //List<List<string>> myList = multiMap.get(((TreeMap)multiMap).lastKey());
            var myList = multiMap[multiMap.Keys.Last()];
            var myList1 = myList[0];
            myList.RemoveAt(0);
            if (myList.Count == 0)
            {
                //multiMap.remove(((TreeMap)multiMap).lastKey());
                multiMap.Remove(multiMap.Keys.Last());
            }
            return myList1;
        }

        /// <summary>Utility function for creating esimated size map for different properties of XMP Packet.</summary>
        /// <remarks>see PackageForJPEG</remarks>
        /// <param name="stdXMP">Meta Object whose property sizes needs to calculate.</param>
        /// <param name="propSizes">A treeMap Object which takes int as a key and list of list of string as values</param>
        private static void CreateEstimatedSizeMap(XmpMeta stdXMP, SortedDictionary<int, List<List<string>>> propSizes)
        {
            for (int s = stdXMP.GetRoot().GetChildrenLength(); s > 0; --s)
            {
                var stdSchema = stdXMP.GetRoot().GetChild(s);
                for (int p = stdSchema.GetChildrenLength(); p > 0; --p)
                {
                    XmpNode stdProp = stdSchema.GetChild(p);
                    if (stdSchema.Name.Equals(XmpConstants.NsXmpNote) && stdProp.Name.Equals("xmpNote:HasExtendedXMP"))
                        continue; // ! Don't move xmpNote:HasExtendedXMP.

                    int propSize = EstimateSizeForJPEG(stdProp);
                    var namePair = new List<string> {stdSchema.Name, stdProp.Name};
                    PutObjectsInMultiMap(propSizes, propSize, namePair);
                }
            }
        }

        /// <summary>Utility function for moving the largest property from One XMP Packet to another.</summary>
        /// <remarks>see MoveOneProperty and PackageForJPEG</remarks>
        /// <param name="stdXMP">Meta Object from where property moves.</param>
        /// <param name="extXMP">Meta Object to where property moves.</param>
        /// <param name="propSizes">A treeMap Object which holds the estimated sizes of the property of stdXMP as a key and their string representation as map values.</param>
        private static int MoveLargestProperty(XmpMeta stdXMP, XmpMeta extXMP, SortedDictionary<int, List<List<string>>> propSizes)
        {
            Debug.Assert(propSizes.Count != 0);

            //int propSize = (Integer)((TreeMap)propSizes).lastKey();
            var propSize = propSizes.Keys.Last();
            var tempList = GetBiggestEntryInMultiMap(propSizes);

            var moved = MoveOneProperty(stdXMP, extXMP, schemaURI: tempList[0], propName: tempList[1]);

            Debug.Assert(moved);
            return propSize;
        }

        /// <summary>creates XMP serializations appropriate for a JPEG file.</summary>
        /// <remarks>
        /// The standard XMP in a JPEG file is limited to 64K bytes. This function
        /// serializes the XMP metadata in an XMP object into a string of RDF.If
        /// the data does not fit into the 64K byte limit, it creates a second packet
        /// string with the extended data.
        /// </remarks>
        /// <param name="origXMPImpl">The XMP object containing the metadata.</param>
        /// <param name="stdStr">A string object in which to return the full standard XMP packet.</param>
        /// <param name="extStr">A string object in which to return the serialized extended XMP, empty if not needed.</param>
        /// <param name="digestStr">A string object in which to return an MD5 digest of the serialized extended XMP, empty if not needed.</param>
        public static void PackageForJPEG(IXmpMeta origXMPImpl,
                   StringBuilder stdStr,
                   StringBuilder extStr,
                   StringBuilder digestStr)
        {
            var origXMP = (XmpMeta)origXMPImpl;

            Debug.Assert(stdStr != null && extStr != null && digestStr != null);    // ! Enforced by wrapper.

            const int kStdXMPLimit = 65000;
            const string kPacketTrailer = "<?xpacket end=\"w\"?>";
            int kTrailerLen = kPacketTrailer.Length;

            var stdXMP = new XmpMeta();
            var extXMP = new XmpMeta();

            var keepItSmall = new SerializeOptions(SerializeOptions.UseCompactFormatFlag)
            {
                Padding = 0,
                Indent = "",
                BaseIndent = 0,
                Newline = " "
            };

            // Try to serialize everything. Note that we're making internal calls to SerializeToBuffer, so
            // we'll be getting back the pointer and length for its internal string.

            var tempStr = XmpMetaFactory.SerializeToString(origXMP, keepItSmall);

            if (tempStr.Length > kStdXMPLimit )
            {
                // Couldn't fit everything, make a copy of the input XMP and make sure there is no xmp:Thumbnails property.

                stdXMP.GetRoot().Options = origXMP.GetRoot().Options;
                stdXMP.GetRoot().Name = origXMP.GetRoot().Name;
                stdXMP.GetRoot().Value = origXMP.GetRoot().Value;

                origXMP.GetRoot().CloneSubtree(stdXMP.GetRoot(), false);

                if (stdXMP.DoesPropertyExist(XmpConstants.NsXmp, "Thumbnails"))
                {
                    stdXMP.DeleteProperty(XmpConstants.NsXmp, "Thumbnails" );
                    tempStr = XmpMetaFactory.SerializeToString(stdXMP, keepItSmall);
                }
            }

            if (tempStr.Length > kStdXMPLimit )
            {
                // Still doesn't fit, move all of the Camera Raw namespace. Add a dummy value for xmpNote:HasExtendedXMP.

                stdXMP.SetProperty(XmpConstants.NsXmpNote,
                    "HasExtendedXMP",
                    "123456789-123456789-123456789-12",
                    new PropertyOptions(PropertyOptions.NoOptionsFlag));

                var crSchema = XmpNodeUtils.FindSchemaNode(stdXMP.GetRoot(), XmpConstants.NsCameraraw, false);

                if (crSchema != null)
                {
                    crSchema.Parent = extXMP.GetRoot();
                    extXMP.GetRoot().AddChild(crSchema);
                    stdXMP.GetRoot().RemoveChild(crSchema);

                    tempStr = XmpMetaFactory.SerializeToString(stdXMP, keepItSmall);
                }
            }

            if (tempStr.Length > kStdXMPLimit)
            {
                // Still doesn't fit, move photoshop:History.

                bool moved = MoveOneProperty(stdXMP, extXMP, XmpConstants.NsPhotoshop, "photoshop:History");

                if (moved )
                    tempStr = XmpMetaFactory.SerializeToString(stdXMP, keepItSmall);
            }

            if (tempStr.Length > kStdXMPLimit)
            {
                // Still doesn't fit, move top level properties in order of estimated size. This is done by
                // creating a multi-map that maps the serialized size to the string pair for the schema URI
                // and top level property name. Since maps are inherently ordered, a reverse iteration of
                // the map can be done to move the largest things first. We use a double loop to keep going
                // until the serialization actually fits, in case the estimates are off.

                var propSizes = new SortedDictionary<int, List<List<string>>>();

                CreateEstimatedSizeMap(stdXMP, propSizes);

                // Outer loop to make sure enough is actually moved.

                while (tempStr.Length > kStdXMPLimit && propSizes.Count != 0)
                {
                    // Inner loop, move what seems to be enough according to the estimates.

                    int tempLen = tempStr.Length;
                    while (tempLen > kStdXMPLimit && propSizes.Count != 0)
                    {
                        int propSize = MoveLargestProperty(stdXMP, extXMP, propSizes);

                        Debug.Assert(propSize > 0);

                        if (propSize > tempLen)
                            propSize = tempLen;    // ! Don't go negative.

                        tempLen -= propSize;
                    }

                    // Reserialize the remaining standard XMP.

                    tempStr = XmpMetaFactory.SerializeToString(stdXMP, keepItSmall);
                }

            }

            if (tempStr.Length > kStdXMPLimit)
            {
                // Still doesn't fit, throw an exception and let the client decide what to do.
                // ! This should never happen with the policy of moving any and all top level properties.
                    throw new XmpException("Can't reduce XMP enough for JPEG file", XmpErrorCode.InternalFailure);
            }

            // Set the static output strings.
            if (extXMP.GetRoot().GetChildrenLength() == 0)
            {
                // Just have the standard XMP.
                stdStr.Append(tempStr);
            }
            else
            {
                // Have extended XMP. Serialize it, compute the digest, reset xmpNote:HasExtendedXMP, and
                // reserialize the standard XMP.

                tempStr = XmpMetaFactory.SerializeToString(extXMP,
                        new SerializeOptions(SerializeOptions.UseCompactFormatFlag | SerializeOptions.OmitPacketWrapperFlag));

                extStr.Append(tempStr);

                // TODO: If we move to >= netstandard20, this portion can be implemented
                /*
                MessageDigest md = MessageDigest.getInstance("MD5");
                md.update(tempStr.getBytes());

                byte[] byteData = md.digest();

                for (int i = 0; i < byteData.Length; i++)
                {
                    digestStr.Append(int.Parse((byteData[i] & 0xff) + 0x100, 16).substring(1));
                }
                */

                stdXMP.SetProperty(XmpConstants.NsXmpNote, "HasExtendedXMP", digestStr.ToString(),
                        new PropertyOptions(PropertyOptions.NoOptionsFlag));
                tempStr = XmpMetaFactory.SerializeToString(stdXMP, keepItSmall);
                stdStr.Append(tempStr);
            }

            // Adjust the standard XMP padding to be up to 2KB.
            Debug.Assert((stdStr.Length > kTrailerLen) && (stdStr.Length <= kStdXMPLimit) );

            int extraPadding = kStdXMPLimit - stdStr.Length;    // ! Do this before erasing the trailer.
            if (extraPadding > 2047)
                extraPadding = 2047;
            //stdStr.delete(stdStr.toString().indexOf(kPacketTrailer), stdStr.length());
            stdStr.Remove(stdStr.ToString().IndexOf(kPacketTrailer), stdStr.Length);

            stdStr.Append(' ', extraPadding);

            stdStr.Append(kPacketTrailer).ToString();
        }

        /// <summary>merges standard and extended XMP retrieved from a JPEG file.</summary>
        /// <remarks>
        /// When an extended partition stores properties that do not fit into the
        /// JPEG file limitation of 64K bytes, this function integrates those
        /// properties back into the same XMP object with those from the standard XMP
        /// packet.
        /// </remarks>
        /// <param name="fullXMP">An XMP object which the caller has initialized from the standard XMP packet in a JPEG file. The extended XMP is added to this object.</param>
        /// <param name="extendedXMP">An XMP object which the caller has initialized from the extended XMP packet in a JPEG file.</param>
        public static void MergeFromJPEG(IXmpMeta fullXMP, IXmpMeta extendedXMP)
        {
            var flags = new TemplateOptions(TemplateOptions.ReplaceExistingPropertiesFlag |TemplateOptions.IncludeInternalPropertiesFlag);

            ApplyTemplate((XmpMeta)fullXMP, (XmpMeta)extendedXMP, flags);
            fullXMP.DeleteProperty(XmpConstants.NsXmpNote, "HasExtendedXMP");
        }

        /// <summary>modifies a working XMP object according to a template object.</summary>
        /// <remarks>
        /// The XMP template can be used to add, replace or delete properties from
        /// the working XMP object. The actions that you specify determine how the
        /// template is applied.Each action can be applied individually or combined;
        /// if you do not specify any actions, the properties and values in the
        /// working XMP object do not change.
        /// </remarks>
        /// <param name="origXMP">The destination XMP object.</param>
        /// <param name="tempXMP">The template to apply to the destination XMP object.</param>
        /// <param name="actions">Option flags to control the copying. If none are specified,
        ///    the properties and values in the working XMP do not change. A logical OR of these bit-flag constants:
        ///    <ul>
        ///    <li><code> CLEAR_UNNAMED_PROPERTIES</code> Delete anything that is not in the template.</li>
        ///    <li><code> ADD_NEW_PROPERTIES</code> Add properties; see detailed description.</li>
        ///    <li><code> REPLACE_EXISTING_PROPERTIES</code> Replace the values of existing properties.</li>
        ///    <li><code> REPLACE_WITH_DELETE_EMPTY</code> Replace the values of existing properties and delete properties if the new value is empty.</li>
        ///    <li><code> INCLUDE_INTERNAL_PROPERTIES</code> Operate on internal properties as well as external properties.</li>
        ///    </ul>
        /// </param>
        public static void ApplyTemplate(IXmpMeta origXMP, IXmpMeta tempXMP, TemplateOptions actions)
        {
            var workingXMP = (XmpMeta)origXMP;
            var templateXMP = (XmpMeta)tempXMP;

            bool doClear = (actions.GetOptions() & TemplateOptions.ClearUnnamedPropertiesFlag) != 0;
            bool doAdd = (actions.GetOptions() & TemplateOptions.AddNewPropertiesFlag) != 0;
            bool doReplace = (actions.GetOptions() & TemplateOptions.ReplaceExistingPropertiesFlag) != 0;

            bool deleteEmpty = (actions.GetOptions() & TemplateOptions.ReplaceWithDeleteEmptyFlag) != 0;
            doReplace |= deleteEmpty; // Delete-empty implies Replace.
            deleteEmpty &= (!doClear); // Clear implies not delete-empty, but keep
                                       // the implicit Replace.

            bool doAll = (actions.GetOptions() & TemplateOptions.IncludeInternalPropertiesFlag) != 0;

            // ! In several places we do loops backwards so that deletions do not
            // perturb the remaining indices.
            // ! These loops use ordinals (size .. 1), we must use a zero based
            // index inside the loop.
            if (doClear)
            {
                // Visit the top level working properties, delete if not in the
                // template.
                for (int schemaOrdinal = workingXMP.GetRoot().GetChildrenLength(); schemaOrdinal > 0; --schemaOrdinal)
                {
                    XmpNode workingSchema = workingXMP.GetRoot().GetChild(schemaOrdinal);
                    XmpNode templateSchema = XmpNodeUtils.FindSchemaNode(templateXMP.GetRoot(), workingSchema.Name, false);

                    if (templateSchema == null)
                    {
                        // The schema is not in the template, delete all properties
                        // or just all external ones.
                        if (doAll)
                        {
                            workingSchema.RemoveChildren(); // Remove the properties here, delete the schema below.
                        }
                        else
                        {
                            for (int propOrdinal = workingSchema.GetChildrenLength(); propOrdinal > 0; --propOrdinal)
                            {
                                XmpNode workingProp = workingSchema.GetChild(propOrdinal);
                                if (!Utils.IsInternalProperty(workingSchema.Name, workingProp.Name))
                                    workingSchema.RemoveChild(propOrdinal);
                            }
                        }
                    }
                    else
                    {
                        // Check each of the working XMP's properties to see if it is in the template.
                        for (int propOrdinal = workingSchema.GetChildrenLength(); propOrdinal > 0; --propOrdinal)
                        {
                            XmpNode workingProp = workingSchema.GetChild(propOrdinal);
                            if ((doAll || !Utils.IsInternalProperty(workingSchema.Name, workingProp.Name))
                                    && (XmpNodeUtils.FindChildNode(templateSchema, workingProp.Name, false) == null))
                                workingSchema.RemoveChild(propOrdinal);
                        }
                    }

                    if (workingSchema.HasChildren == false)
                        workingXMP.GetRoot().RemoveChild(schemaOrdinal);
                }
            }

            if (doAdd || doReplace)
            {
                for (int schemaNum = 0, schemaLim = templateXMP.GetRoot().GetChildrenLength(); schemaNum<schemaLim; ++schemaNum)
                {
                    XmpNode templateSchema = templateXMP.GetRoot().GetChild(schemaNum + 1);

                    // Make sure we have an output schema node, then process the top level template properties.
                    XmpNode workingSchema = XmpNodeUtils.FindSchemaNode(workingXMP.GetRoot(), templateSchema.Name, false);
                    if (workingSchema == null)
                    {
                        workingSchema = new XmpNode(templateSchema.Name, templateSchema.Value, new PropertyOptions(PropertyOptions.SchemaNodeFlag));
                        workingXMP.GetRoot().AddChild(workingSchema);
                        workingSchema.Parent = workingXMP.GetRoot();
                    }

                    for (int propNum = 1, propLim = templateSchema.GetChildrenLength(); propNum <= propLim; ++propNum)
                    {
                        XmpNode templateProp = templateSchema.GetChild(propNum);
                        if (doAll || !Utils.IsInternalProperty(templateSchema.Name, templateProp.Name))
                            AppendSubtree(workingXMP, templateProp, workingSchema, doAdd, doReplace, deleteEmpty);
                    }

                    if (workingSchema.HasChildren == false)
                        workingXMP.GetRoot().RemoveChild(workingSchema);
                }
            }
        }
    }
}
