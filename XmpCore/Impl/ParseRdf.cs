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
using System.Xml;
using System.Xml.Linq;
using XmpCore.Options;

namespace XmpCore.Impl
{
    public enum RdfTerm
    {
        Other = 0,

        /// <summary>Start of coreSyntaxTerms.</summary>
        Rdf = 1,

        Id = 2,

        About = 3,

        ParseType = 4,

        Resource = 5,

        NodeId = 6,

        /// <summary>End of coreSyntaxTerms</summary>
        Datatype = 7,

        /// <summary>Start of additions for syntax Terms.</summary>
        Description = 8,

        /// <summary>End of of additions for syntaxTerms.</summary>
        Li = 9,

        /// <summary>Start of oldTerms.</summary>
        AboutEach = 10,

        AboutEachPrefix = 11,

        /// <summary>End of oldTerms.</summary>
        BagId = 12,

        FirstCore = Rdf,

        LastCore = Datatype,

        /// <summary>! Yes, the syntax terms include the core terms.</summary>
        FirstSyntax = FirstCore,

        LastSyntax = Li,

        FirstOld = AboutEach,

        LastOld = BagId
    }

    /// <summary>Parser for "normal" XML serialisation of RDF.</summary>
    /// <since>14.07.2006</since>
    public static class ParseRdf
    {
        /// <summary>this prefix is used for default namespaces</summary>
        public const string DefaultPrefix = "_dflt";

        /// <summary>The main parsing method.</summary>
        /// <remarks>
        /// The main parsing method. The XML tree is walked through from the root node and and XMP tree
        /// is created. This is a raw parse, the normalisation of the XMP tree happens outside.
        /// </remarks>
        /// <param name="xmlRoot">the XML root node</param>
        /// <returns>Returns an XMP metadata object (not normalized)</returns>
        /// <exception cref="XmpException">Occurs if the parsing fails for any reason.</exception>
        internal static XmpMeta Parse(XElement xmlRoot)
        {
            var xmp = new XmpMeta();
            Rdf_RDF(xmp, xmlRoot);
            return xmp;
        }

        /// <summary>
        /// Each of these parsing methods is responsible for recognizing an RDF
        /// syntax production and adding the appropriate structure to the XMP tree.
        /// </summary>
        /// <remarks>
        /// Each of these parsing methods is responsible for recognizing an RDF
        /// syntax production and adding the appropriate structure to the XMP tree.
        /// They simply return for success, failures will throw an exception.
        /// </remarks>
        /// <param name="xmp">the xmp metadata object that is generated</param>
        /// <param name="rdfRdfNode">the top-level xml node</param>
        /// <exception cref="XmpException">thrown on parsing errors</exception>
        internal static void Rdf_RDF(XmpMeta xmp, XElement rdfRdfNode)
        {
            if (rdfRdfNode.Attributes().Count() > 0)
            {
                Rdf_NodeElementList(xmp, xmp.GetRoot(), rdfRdfNode);
            }
            else
            {
                throw new XmpException("Invalid attributes of rdf:RDF element", XmpErrorCode.BadRdf);
            }
        }

        /// <summary>
        /// 7.2.10 nodeElementList<br />
        /// ws* ( nodeElement ws* )
        /// Note: this method is only called from the rdf:RDF-node (top level)
        /// </summary>
        /// <param name="xmp">the xmp metadata object that is generated</param>
        /// <param name="xmpParent">the parent xmp node</param>
        /// <param name="rdfRdfNode">the top-level xml node</param>
        /// <exception cref="XmpException">thrown on parsing errors</exception>
        private static void Rdf_NodeElementList(XmpMeta xmp, XmpNode xmpParent, XElement rdfRdfNode)
        {
            foreach (var child in rdfRdfNode.Nodes())
            {
                // filter whitespaces (and all text nodes)
                if (!IsWhitespaceNode(child))
                {
                    Rdf_NodeElement(xmp, xmpParent, (XElement)child, true);
                }
            }
        }

        /// <summary>
        /// 7.2.5 nodeElementURIs
        /// anyURI - ( coreSyntaxTerms | rdf:li | oldTerms )
        /// 7.2.11 nodeElement
        /// start-element ( URI == nodeElementURIs,
        /// attributes == set ( ( idAttr | nodeIdAttr | aboutAttr )?, propertyAttr* ) )
        /// propertyEltList
        /// end-element()
        /// A node element URI is rdf:Description or anything else that is not an RDF
        /// term.
        /// </summary>
        /// <param name="xmp">the xmp metadata object that is generated</param>
        /// <param name="xmpParent">the parent xmp node</param>
        /// <param name="xmlNode">the currently processed XML node</param>
        /// <param name="isTopLevel">Flag if the node is a top-level node</param>
        /// <exception cref="XmpException">thrown on parsing errors</exception>
        private static void Rdf_NodeElement(XmpMeta xmp, XmpNode xmpParent, XElement xmlNode, bool isTopLevel)
        {
            var nodeTerm = GetRdfTermKind(xmlNode);
            if (nodeTerm != RdfTerm.Description && nodeTerm != RdfTerm.Other)
            {
                throw new XmpException("Node element must be rdf:Description or typed node", XmpErrorCode.BadRdf);
            }
            if (isTopLevel && nodeTerm == RdfTerm.Other)
            {
                throw new XmpException("Top level typed node not allowed", XmpErrorCode.BadXmp);
            }
            Rdf_NodeElementAttrs(xmp, xmpParent, xmlNode, isTopLevel);
            Rdf_PropertyElementList(xmp, xmpParent, xmlNode, isTopLevel);
        }

        /// <summary>
        /// 7.2.7 propertyAttributeURIs
        /// anyURI - ( coreSyntaxTerms | rdf:Description | rdf:li | oldTerms )
        /// 7.2.11 nodeElement
        /// start-element ( URI == nodeElementURIs,
        /// attributes == set ( ( idAttr | nodeIdAttr | aboutAttr )?, propertyAttr* ) )
        /// propertyEltList
        /// end-element()
        /// Process the attribute list for an RDF node element.
        /// </summary>
        /// <remarks>
        /// 7.2.7 propertyAttributeURIs
        /// anyURI - ( coreSyntaxTerms | rdf:Description | rdf:li | oldTerms )
        /// 7.2.11 nodeElement
        /// start-element ( URI == nodeElementURIs,
        /// attributes == set ( ( idAttr | nodeIdAttr | aboutAttr )?, propertyAttr* ) )
        /// propertyEltList
        /// end-element()
        /// Process the attribute list for an RDF node element. A property attribute URI is
        /// anything other than an RDF term. The rdf:ID and rdf:nodeID attributes are simply ignored,
        /// as are rdf:about attributes on inner nodes.
        /// </remarks>
        /// <param name="xmp">the xmp metadata object that is generated</param>
        /// <param name="xmpParent">the parent xmp node</param>
        /// <param name="xmlNode">the currently processed XML node</param>
        /// <param name="isTopLevel">Flag if the node is a top-level node</param>
        /// <exception cref="XmpException">thrown on parsing errors</exception>
        private static void Rdf_NodeElementAttrs(XmpMeta xmp, XmpNode xmpParent, XElement xmlNode, bool isTopLevel)
        {
            // Used to detect attributes that are mutually exclusive.
            var exclusiveAttrs = 0;
            foreach (var attribute in xmlNode.Attributes())
            {
                // quick hack, ns declarations do not appear in C++
                // ignore "ID" without namespace
                var prefix = xmlNode.GetPrefixOfNamespace(attribute.Name.Namespace);
                if (prefix == "xmlns" || (prefix == null && attribute.Name == "xmlns"))
                {
                    continue;
                }
                var attrTerm = GetRdfTermKind(attribute);
                switch (attrTerm)
                {
                    case RdfTerm.Id:
                    case RdfTerm.NodeId:
                    case RdfTerm.About:
                    {
                        if (exclusiveAttrs > 0)
                        {
                            throw new XmpException("Mutally exclusive about, ID, nodeID attributes", XmpErrorCode.BadRdf);
                        }
                        exclusiveAttrs++;
                        if (isTopLevel && (attrTerm == RdfTerm.About))
                        {
                            // This is the rdf:about attribute on a top level node. Set
                            // the XMP tree name if
                            // it doesn't have a name yet. Make sure this name matches
                            // the XMP tree name.
                            if (!string.IsNullOrEmpty(xmpParent.Name))
                            {
                                if (attribute.Value != xmpParent.Name)
                                {
                                    throw new XmpException("Mismatched top level rdf:about values", XmpErrorCode.BadXmp);
                                }
                            }
                            else
                            {
                                xmpParent.Name = attribute.Value;
                            }
                        }
                        break;
                    }

                    case RdfTerm.Other:
                    {
                        AddChildNode(xmp, xmpParent, attribute, attribute.Value, isTopLevel);
                        break;
                    }

                    default:
                    {
                        throw new XmpException("Invalid nodeElement attribute", XmpErrorCode.BadRdf);
                    }
                }
            }
        }

        /// <summary>
        /// 7.2.13 propertyEltList
        /// ws* ( propertyElt ws* )
        /// </summary>
        /// <param name="xmp">the xmp metadata object that is generated</param>
        /// <param name="xmpParent">the parent xmp node</param>
        /// <param name="xmlParent">the currently processed XML node</param>
        /// <param name="isTopLevel">Flag if the node is a top-level node</param>
        /// <exception cref="XmpException">thrown on parsing errors</exception>
        private static void Rdf_PropertyElementList(XmpMeta xmp, XmpNode xmpParent, XElement xmlParent, bool isTopLevel)
        {
            foreach (var currChild in xmlParent.Nodes())
            {
                if (IsWhitespaceNode(currChild))
                {
                    continue;
                }
                if (currChild.NodeType == XmlNodeType.Comment)
                    continue;

                if (currChild.NodeType != XmlNodeType.Element)
                {
                    throw new XmpException("Expected property element node not found", XmpErrorCode.BadRdf);
                }
                Rdf_PropertyElement(xmp, xmpParent, (XElement)currChild, isTopLevel);
            }
        }

        /// <summary>
        /// 7.2.14 propertyElt
        /// resourcePropertyElt | literalPropertyElt | parseTypeLiteralPropertyElt |
        /// parseTypeResourcePropertyElt | parseTypeCollectionPropertyElt |
        /// parseTypeOtherPropertyElt | emptyPropertyElt
        /// 7.2.15 resourcePropertyElt
        /// start-element ( URI == propertyElementURIs, attributes == set ( idAttr? ) )
        /// ws* nodeElement ws
        /// end-element()
        /// 7.2.16 literalPropertyElt
        /// start-element (
        /// URI == propertyElementURIs, attributes == set ( idAttr?, datatypeAttr?) )
        /// text()
        /// end-element()
        /// 7.2.17 parseTypeLiteralPropertyElt
        /// start-element (
        /// URI == propertyElementURIs, attributes == set ( idAttr?, parseLiteral ) )
        /// literal
        /// end-element()
        /// 7.2.18 parseTypeResourcePropertyElt
        /// start-element (
        /// URI == propertyElementURIs, attributes == set ( idAttr?, parseResource ) )
        /// propertyEltList
        /// end-element()
        /// 7.2.19 parseTypeCollectionPropertyElt
        /// start-element (
        /// URI == propertyElementURIs, attributes == set ( idAttr?, parseCollection ) )
        /// nodeElementList
        /// end-element()
        /// 7.2.20 parseTypeOtherPropertyElt
        /// start-element ( URI == propertyElementURIs, attributes == set ( idAttr?, parseOther ) )
        /// propertyEltList
        /// end-element()
        /// 7.2.21 emptyPropertyElt
        /// start-element ( URI == propertyElementURIs,
        /// attributes == set ( idAttr?, ( resourceAttr | nodeIdAttr )?, propertyAttr* ) )
        /// end-element()
        /// The various property element forms are not distinguished by the XML element name,
        /// but by their attributes for the most part.
        /// </summary>
        /// <remarks>
        /// 7.2.14 propertyElt
        /// resourcePropertyElt | literalPropertyElt | parseTypeLiteralPropertyElt |
        /// parseTypeResourcePropertyElt | parseTypeCollectionPropertyElt |
        /// parseTypeOtherPropertyElt | emptyPropertyElt
        /// 7.2.15 resourcePropertyElt
        /// start-element ( URI == propertyElementURIs, attributes == set ( idAttr? ) )
        /// ws* nodeElement ws
        /// end-element()
        /// 7.2.16 literalPropertyElt
        /// start-element (
        /// URI == propertyElementURIs, attributes == set ( idAttr?, datatypeAttr?) )
        /// text()
        /// end-element()
        /// 7.2.17 parseTypeLiteralPropertyElt
        /// start-element (
        /// URI == propertyElementURIs, attributes == set ( idAttr?, parseLiteral ) )
        /// literal
        /// end-element()
        /// 7.2.18 parseTypeResourcePropertyElt
        /// start-element (
        /// URI == propertyElementURIs, attributes == set ( idAttr?, parseResource ) )
        /// propertyEltList
        /// end-element()
        /// 7.2.19 parseTypeCollectionPropertyElt
        /// start-element (
        /// URI == propertyElementURIs, attributes == set ( idAttr?, parseCollection ) )
        /// nodeElementList
        /// end-element()
        /// 7.2.20 parseTypeOtherPropertyElt
        /// start-element ( URI == propertyElementURIs, attributes == set ( idAttr?, parseOther ) )
        /// propertyEltList
        /// end-element()
        /// 7.2.21 emptyPropertyElt
        /// start-element ( URI == propertyElementURIs,
        /// attributes == set ( idAttr?, ( resourceAttr | nodeIdAttr )?, propertyAttr* ) )
        /// end-element()
        /// The various property element forms are not distinguished by the XML element name,
        /// but by their attributes for the most part. The exceptions are resourcePropertyElt and
        /// literalPropertyElt. They are distinguished by their XML element content.
        /// NOTE: The RDF syntax does not explicitly include the xml:lang attribute although it can
        /// appear in many of these. We have to allow for it in the attibute counts below.
        /// </remarks>
        /// <param name="xmp">the xmp metadata object that is generated</param>
        /// <param name="xmpParent">the parent xmp node</param>
        /// <param name="xmlNode">the currently processed XML node</param>
        /// <param name="isTopLevel">Flag if the node is a top-level node</param>
        /// <exception cref="XmpException">thrown on parsing errors</exception>
        private static void Rdf_PropertyElement(XmpMeta xmp, XmpNode xmpParent, XElement xmlNode, bool isTopLevel)
        {
            var nodeTerm = GetRdfTermKind(xmlNode);
            if (!IsPropertyElementName(nodeTerm))
            {
                throw new XmpException("Invalid property element name", XmpErrorCode.BadRdf);
            }

            var attributes = xmlNode.Attributes();

            // remove the namespace-definitions from the list (original Java)
            // (for C#, put them in an ignore list and don't count or process them)
            var ignoreNodes = new List<string>();
            foreach (var attribute in attributes)
            {
                var prefix = xmlNode.GetPrefixOfNamespace(attribute.Name.Namespace);
                if (prefix == "xmlns" || (prefix == null && attribute.Name == "xmlns"))
                {
                    ignoreNodes.Add(attribute.Name.ToString());
                }
            }

            if (attributes.Count() - ignoreNodes.Count > 3)
            {
                // Only an emptyPropertyElt can have more than 3 attributes.
                Rdf_EmptyPropertyElement(xmp, xmpParent, xmlNode, isTopLevel);
            }
            else
            {
                // Look through the attributes for one that isn't rdf:ID or xml:lang,
                // it will usually tell what we should be dealing with.
                // The called routines must verify their specific syntax!
                // (Also don't consider an ignored attribute as 'found')

                var attrLocal = "";
                var attrNs = "";
                var attrValue = "";

                XAttribute foundAttrib = null;
                foreach (var attribute in attributes)
                {
                    attrLocal = attribute.Name.LocalName;
                    attrNs = attribute.Name.NamespaceName;
                    attrValue = attribute.Value;

                    if ("xml:" + attrLocal != XmpConstants.XmlLang && !(attrLocal == "ID" && attrNs == XmpConstants.NsRdf)
                        && !ignoreNodes.Contains(attribute.Name.ToString()))
                    {
                        foundAttrib = attribute;
                        break;
                    }
                }
                if (foundAttrib != null) // meaning, contains one node other than xml:lang and rdf:ID
                {
                    attrLocal = foundAttrib.Name.LocalName;
                    attrNs = foundAttrib.Name.NamespaceName;
                    attrValue = foundAttrib.Value;

                    if (attrLocal == "datatype" && attrNs == XmpConstants.NsRdf)
                    {
                        Rdf_LiteralPropertyElement(xmp, xmpParent, xmlNode, isTopLevel);
                    }
                    else if (!(attrLocal == "parseType" && attrNs == XmpConstants.NsRdf))
                    {
                        Rdf_EmptyPropertyElement(xmp, xmpParent, xmlNode, isTopLevel);
                    }
                    else if (attrValue == "Literal")
                    {
                        Rdf_ParseTypeLiteralPropertyElement();
                    }
                    else if (attrValue == "Resource")
                    {
                        Rdf_ParseTypeResourcePropertyElement(xmp, xmpParent, xmlNode, isTopLevel);
                    }
                    else if (attrValue == "Collection")
                    {
                        Rdf_ParseTypeCollectionPropertyElement();
                    }
                    else
                    {
                        Rdf_ParseTypeOtherPropertyElement();
                    }
                }
                else
                {
                    // Only rdf:ID and xml:lang, could be a resourcePropertyElt, a literalPropertyElt,
                    // or an emptyPropertyElt. Look at the child XML nodes to decide which.
                    if (xmlNode.IsEmpty)
                    {
                        Rdf_EmptyPropertyElement(xmp, xmpParent, xmlNode, isTopLevel);
                    }
                    else
                    {
                        var nonTextNode = xmlNode.Nodes().FirstOrDefault(t => t.NodeType != XmlNodeType.Text);
                        if (nonTextNode == null)
                            Rdf_LiteralPropertyElement(xmp, xmpParent, xmlNode, isTopLevel);
                        else
                            Rdf_ResourcePropertyElement(xmp, xmpParent, xmlNode, isTopLevel);
                    }
                }
            }
        }

        /// <summary>
        /// 7.2.15 resourcePropertyElt
        /// start-element ( URI == propertyElementURIs, attributes == set ( idAttr? ) )
        /// ws* nodeElement ws
        /// end-element()
        /// This handles structs using an rdf:Description node,
        /// arrays using rdf:Bag/Seq/Alt, and typedNodes.
        /// </summary>
        /// <remarks>
        /// 7.2.15 resourcePropertyElt
        /// start-element ( URI == propertyElementURIs, attributes == set ( idAttr? ) )
        /// ws* nodeElement ws
        /// end-element()
        /// This handles structs using an rdf:Description node,
        /// arrays using rdf:Bag/Seq/Alt, and typedNodes. It also catches and cleans up qualified
        /// properties written with rdf:Description and rdf:value.
        /// </remarks>
        /// <param name="xmp">the xmp metadata object that is generated</param>
        /// <param name="xmpParent">the parent xmp node</param>
        /// <param name="xmlNode">the currently processed XML node</param>
        /// <param name="isTopLevel">Flag if the node is a top-level node</param>
        /// <exception cref="XmpException">thrown on parsing errors</exception>
        private static void Rdf_ResourcePropertyElement(XmpMeta xmp, XmpNode xmpParent, XElement xmlNode, bool isTopLevel)
        {
            if (isTopLevel && xmlNode.Name == "iX:changes")
            {
                // Strip old "punchcard" chaff which has on the prefix "iX:".
                return;
            }

            var newCompound = AddChildNode(xmp, xmpParent, xmlNode, string.Empty, isTopLevel);
            // walk through the attributes
            foreach (var attribute in xmlNode.Attributes())
            {
                var prefix = xmlNode.GetPrefixOfNamespace(attribute.Name.Namespace);

                if (prefix == "xmlns" || (prefix == null && attribute.Name == "xmlns"))
                    continue;

                var attrLocal = attribute.Name.LocalName;
                var attrNs = attribute.Name.NamespaceName;
                if ("xml:" + attribute.Name.LocalName == XmpConstants.XmlLang)
                {
                    AddQualifierNode(newCompound, XmpConstants.XmlLang, attribute.Value);
                }
                else
                {
                    if (attrLocal == "ID" && attrNs == XmpConstants.NsRdf)
                        continue;

                    // Ignore all rdf:ID attributes.
                    throw new XmpException("Invalid attribute for resource property element", XmpErrorCode.BadRdf);
                }
            }

            // walk through the children
            var found = false;
            foreach (var currChild in xmlNode.Nodes())
            {
                if (!IsWhitespaceNode(currChild))
                {
                    if (currChild.NodeType == XmlNodeType.Element && !found)
                    {
                        var currChildElem = (XElement)currChild;
                        var isRdf = currChildElem.Name.NamespaceName == XmpConstants.NsRdf;
                        var childLocal = currChildElem.Name.LocalName;
                        if (isRdf && childLocal == "Bag")
                        {
                            newCompound.Options.IsArray = true;
                        }
                        else if (isRdf && childLocal == "Seq")
                        {
                            newCompound.Options.IsArray = true;
                            newCompound.Options.IsArrayOrdered = true;
                        }
                        else if (isRdf && childLocal == "Alt")
                        {
                            newCompound.Options.IsArray = true;
                            newCompound.Options.IsArrayOrdered = true;
                            newCompound.Options.IsArrayAlternate = true;
                        }
                        else
                        {
                            newCompound.Options.IsStruct = true;
                            if (!isRdf && childLocal != "Description")
                            {
                                var typeName = currChildElem.Name.NamespaceName;
                                if (typeName == null)
                                {
                                    throw new XmpException("All XML elements must be in a namespace", XmpErrorCode.BadXmp);
                                }
                                typeName += ':' + childLocal;
                                AddQualifierNode(newCompound, "rdf:type", typeName);
                            }
                        }
                        Rdf_NodeElement(xmp, newCompound, currChildElem, false);
                        if (newCompound.HasValueChild)
                            FixupQualifiedNode(newCompound);
                        else if (newCompound.Options.IsArrayAlternate)
                            XmpNodeUtils.DetectAltText(newCompound);
                        found = true;
                    }
                    else
                    {
                        if (found)
                        {
                            // found second child element
                            throw new XmpException("Invalid child of resource property element", XmpErrorCode.BadRdf);
                        }
                        throw new XmpException("Children of resource property element must be XML elements", XmpErrorCode.BadRdf);
                    }
                }
            }
            if (!found)
            {
                // didn't found any child elements
                throw new XmpException("Missing child of resource property element", XmpErrorCode.BadRdf);
            }
        }

        /// <summary>
        /// 7.2.16 literalPropertyElt
        /// start-element ( URI == propertyElementURIs,
        /// attributes == set ( idAttr?, datatypeAttr?) )
        /// text()
        /// end-element()
        /// Add a leaf node with the text value and qualifiers for the attributes.
        /// </summary>
        /// <param name="xmp">the xmp metadata object that is generated</param>
        /// <param name="xmpParent">the parent xmp node</param>
        /// <param name="xmlNode">the currently processed XML node</param>
        /// <param name="isTopLevel">Flag if the node is a top-level node</param>
        /// <exception cref="XmpException">thrown on parsing errors</exception>
        private static void Rdf_LiteralPropertyElement(XmpMeta xmp, XmpNode xmpParent, XElement xmlNode, bool isTopLevel)
        {
            var newChild = AddChildNode(xmp, xmpParent, xmlNode, null, isTopLevel);
            foreach (var attribute in xmlNode.Attributes())
            {
                var prefix = xmlNode.GetPrefixOfNamespace(attribute.Name.Namespace);

                if (prefix == "xmlns" || (prefix == null && attribute.Name == "xmlns"))
                    continue;

                var attrNs = attribute.Name.NamespaceName;
                var attrLocal = attribute.Name.LocalName;
                if ("xml:" + attribute.Name.LocalName == XmpConstants.XmlLang)
                {
                    AddQualifierNode(newChild, XmpConstants.XmlLang, attribute.Value);
                }
                else
                {
                    if (attrNs == XmpConstants.NsRdf && (attrLocal == "ID" || attrLocal == "datatype"))
                        continue;

                    // Ignore all rdf:ID and rdf:datatype attributes.
                    throw new XmpException("Invalid attribute for literal property element", XmpErrorCode.BadRdf);
                }
            }
            var textValue = string.Empty;
            foreach (var child in xmlNode.Nodes())
            {
                if (child.NodeType == XmlNodeType.Text)
                {
                    textValue += ((XText)child).Value;
                }
                else
                {
                    throw new XmpException("Invalid child of literal property element", XmpErrorCode.BadRdf);
                }
            }
            newChild.Value = textValue;
        }

        /// <summary>
        /// 7.2.17 parseTypeLiteralPropertyElt
        /// start-element ( URI == propertyElementURIs,
        /// attributes == set ( idAttr?, parseLiteral ) )
        /// literal
        /// end-element()
        /// </summary>
        /// <exception cref="XmpException">thrown on parsing errors</exception>
        private static void Rdf_ParseTypeLiteralPropertyElement()
        {
            throw new XmpException("ParseTypeLiteral property element not allowed", XmpErrorCode.BadXmp);
        }

        /// <summary>
        /// 7.2.18 parseTypeResourcePropertyElt
        /// start-element ( URI == propertyElementURIs,
        /// attributes == set ( idAttr?, parseResource ) )
        /// propertyEltList
        /// end-element()
        /// Add a new struct node with a qualifier for the possible rdf:ID attribute.
        /// </summary>
        /// <remarks>
        /// 7.2.18 parseTypeResourcePropertyElt
        /// start-element ( URI == propertyElementURIs,
        /// attributes == set ( idAttr?, parseResource ) )
        /// propertyEltList
        /// end-element()
        /// Add a new struct node with a qualifier for the possible rdf:ID attribute.
        /// Then process the XML child nodes to get the struct fields.
        /// </remarks>
        /// <param name="xmp">the xmp metadata object that is generated</param>
        /// <param name="xmpParent">the parent xmp node</param>
        /// <param name="xmlNode">the currently processed XML node</param>
        /// <param name="isTopLevel">Flag if the node is a top-level node</param>
        /// <exception cref="XmpException">thrown on parsing errors</exception>
        private static void Rdf_ParseTypeResourcePropertyElement(XmpMeta xmp, XmpNode xmpParent, XElement xmlNode, bool isTopLevel)
        {
            var newStruct = AddChildNode(xmp, xmpParent, xmlNode, string.Empty, isTopLevel);
            newStruct.Options.IsStruct = true;
            foreach (var attribute in xmlNode.Attributes())
            {
                var prefix = xmlNode.GetPrefixOfNamespace(attribute.Name.Namespace);
                if (prefix == "xmlns" || (prefix == null && attribute.Name == "xmlns"))
                    continue;

                var attrLocal = attribute.Name.LocalName;
                var attrNs = attribute.Name.NamespaceName;
                if ("xml:" + attribute.Name.LocalName == XmpConstants.XmlLang)
                {
                    AddQualifierNode(newStruct, XmpConstants.XmlLang, attribute.Value);
                }
                else
                {
                    if (attrNs == XmpConstants.NsRdf && (attrLocal == "ID" || attrLocal == "parseType"))
                        continue;

                    // The caller ensured the value is "Resource".
                    // Ignore all rdf:ID attributes.
                    throw new XmpException("Invalid attribute for ParseTypeResource property element", XmpErrorCode.BadRdf);
                }
            }

            Rdf_PropertyElementList(xmp, newStruct, xmlNode, false);

            if (newStruct.HasValueChild)
                FixupQualifiedNode(newStruct);
        }

        /// <summary>
        /// 7.2.19 parseTypeCollectionPropertyElt
        /// start-element ( URI == propertyElementURIs,
        /// attributes == set ( idAttr?, parseCollection ) )
        /// nodeElementList
        /// end-element()
        /// </summary>
        /// <exception cref="XmpException">thrown on parsing errors</exception>
        private static void Rdf_ParseTypeCollectionPropertyElement()
        {
            throw new XmpException("ParseTypeCollection property element not allowed", XmpErrorCode.BadXmp);
        }

        /// <summary>
        /// 7.2.20 parseTypeOtherPropertyElt
        /// start-element ( URI == propertyElementURIs, attributes == set ( idAttr?, parseOther ) )
        /// propertyEltList
        /// end-element()
        /// </summary>
        /// <exception cref="XmpException">thrown on parsing errors</exception>
        private static void Rdf_ParseTypeOtherPropertyElement()
        {
            throw new XmpException("ParseTypeOther property element not allowed", XmpErrorCode.BadXmp);
        }

        /// <summary>
        /// 7.2.21 emptyPropertyElt
        /// start-element ( URI == propertyElementURIs,
        /// attributes == set (
        /// idAttr?, ( resourceAttr | nodeIdAttr )?, propertyAttr* ) )
        /// end-element()
        /// <ns:Prop1/>  <!-- a simple property with an empty value -->
        /// <ns:Prop2 rdf:resource="http: *www.adobe.com/"/> <!-- a URI value -->
        /// <ns:Prop3 rdf:value="..." ns:Qual="..."/> <!-- a simple qualified property -->
        /// <ns:Prop4 ns:Field1="..." ns:Field2="..."/> <!-- a struct with simple fields -->
        /// An emptyPropertyElt is an element with no contained content, just a possibly empty set of
        /// attributes.
        /// </summary>
        /// <remarks>
        /// 7.2.21 emptyPropertyElt
        /// start-element ( URI == propertyElementURIs,
        /// attributes == set (
        /// idAttr?, ( resourceAttr | nodeIdAttr )?, propertyAttr* ) )
        /// end-element()
        /// <ns:Prop1/>  <!-- a simple property with an empty value -->
        /// <ns:Prop2 rdf:resource="http: *www.adobe.com/"/> <!-- a URI value -->
        /// <ns:Prop3 rdf:value="..." ns:Qual="..."/> <!-- a simple qualified property -->
        /// <ns:Prop4 ns:Field1="..." ns:Field2="..."/> <!-- a struct with simple fields -->
        /// An emptyPropertyElt is an element with no contained content, just a possibly empty set of
        /// attributes. An emptyPropertyElt can represent three special cases of simple XMP properties: a
        /// simple property with an empty value (ns:Prop1), a simple property whose value is a URI
        /// (ns:Prop2), or a simple property with simple qualifiers (ns:Prop3).
        /// An emptyPropertyElt can also represent an XMP struct whose fields are all simple and
        /// unqualified (ns:Prop4).
        /// It is an error to use both rdf:value and rdf:resource - that can lead to invalid  RDF in the
        /// verbose form written using a literalPropertyElt.
        /// The XMP mapping for an emptyPropertyElt is a bit different from generic RDF, partly for
        /// design reasons and partly for historical reasons. The XMP mapping rules are:
        /// <list type="bullet">
        /// <item> If there is an rdf:value attribute then this is a simple property
        /// with a text value.
        /// All other attributes are qualifiers.</item>
        /// <item> If there is an rdf:resource attribute then this is a simple property
        /// with a URI value.
        /// All other attributes are qualifiers.</item>
        /// <item> If there are no attributes other than xml:lang, rdf:ID, or rdf:nodeID
        /// then this is a simple
        /// property with an empty value.</item>
        /// <item> Otherwise this is a struct, the attributes other than xml:lang, rdf:ID,
        /// or rdf:nodeID are fields.</item>
        /// </list>
        /// </remarks>
        /// <param name="xmp">the xmp metadata object that is generated</param>
        /// <param name="xmpParent">the parent xmp node</param>
        /// <param name="xmlNode">the currently processed XML node</param>
        /// <param name="isTopLevel">Flag if the node is a top-level node</param>
        /// <exception cref="XmpException">thrown on parsing errors</exception>
        private static void Rdf_EmptyPropertyElement(XmpMeta xmp, XmpNode xmpParent, XElement xmlNode, bool isTopLevel)
        {
            var hasPropertyAttrs = false;
            var hasResourceAttr = false;
            var hasNodeIdAttr = false;
            var hasValueAttr = false;
            XAttribute valueNode = null;
            // ! Can come from rdf:value or rdf:resource.
            if (!(xmlNode.FirstNode == null))
            {
                throw new XmpException("Nested content not allowed with rdf:resource or property attributes", XmpErrorCode.BadRdf);
            }
            // First figure out what XMP this maps to and remember the XML node for a simple value.
            foreach (var attribute in xmlNode.Attributes())
            {
                var prefix = xmlNode.GetPrefixOfNamespace(attribute.Name.Namespace);
                if (prefix == "xmlns" || (prefix == null && attribute.Name == "xmlns"))
                    continue;

                switch (GetRdfTermKind(attribute))
                {
                    case RdfTerm.Id:
                    {
                        // Nothing to do.
                        break;
                    }

                    case RdfTerm.Resource:
                    {
                        if (hasNodeIdAttr)
                        {
                            throw new XmpException("Empty property element can't have both rdf:resource and rdf:nodeID", XmpErrorCode.BadRdf);
                        }
                        if (hasValueAttr)
                        {
                            throw new XmpException("Empty property element can't have both rdf:value and rdf:resource", XmpErrorCode.BadXmp);
                        }
                        hasResourceAttr = true;
                        if (!hasValueAttr)
                        {
                            valueNode = attribute;
                        }
                        break;
                    }

                    case RdfTerm.NodeId:
                    {
                        if (hasResourceAttr)
                        {
                            throw new XmpException("Empty property element can't have both rdf:resource and rdf:nodeID", XmpErrorCode.BadRdf);
                        }
                        hasNodeIdAttr = true;
                        break;
                    }

                    case RdfTerm.Other:
                    {
                        if (attribute.Name.LocalName == "value" && attribute.Name.NamespaceName == XmpConstants.NsRdf)
                        {
                            if (hasResourceAttr)
                            {
                                throw new XmpException("Empty property element can't have both rdf:value and rdf:resource", XmpErrorCode.BadXmp);
                            }
                            hasValueAttr = true;
                            valueNode = attribute;
                        }
                        else
                        {
                            if ("xml:" + attribute.Name.LocalName != XmpConstants.XmlLang)
                            {
                                hasPropertyAttrs = true;
                            }
                        }
                        break;
                    }

                    default:
                    {
                        throw new XmpException("Unrecognized attribute of empty property element", XmpErrorCode.BadRdf);
                    }
                }
            }
            // Create the right kind of child node and visit the attributes again
            // to add the fields or qualifiers.
            // ! Because of implementation vagaries,
            //   the xmpParent is the tree root for top level properties.
            // ! The schema is found, created if necessary, by addChildNode.
            var childNode = AddChildNode(xmp, xmpParent, xmlNode, string.Empty, isTopLevel);
            var childIsStruct = false;
            if (hasValueAttr || hasResourceAttr)
            {
                childNode.Value = valueNode != null ? valueNode.Value : string.Empty;
                if (!hasValueAttr)
                {
                    // ! Might have both rdf:value and rdf:resource.
                    childNode.Options.IsUri = true;
                }
            }
            else
            {
                if (hasPropertyAttrs)
                {
                    childNode.Options.IsStruct = true;
                    childIsStruct = true;
                }
            }

            foreach (var attribute in xmlNode.Attributes())
            {
                var prefix = xmlNode.GetPrefixOfNamespace(attribute.Name.Namespace);
                if (attribute == valueNode || prefix == "xmlns" || (prefix == null && attribute.Name == "xmlns"))
                    continue;

                // Skip the rdf:value or rdf:resource attribute holding the value.
                switch (GetRdfTermKind(attribute))
                {
                    case RdfTerm.Id:
                    case RdfTerm.NodeId:
                    {
                        break;
                    }

                    case RdfTerm.Resource:
                    {
                        // Ignore all rdf:ID and rdf:nodeID attributes.
                        AddQualifierNode(childNode, "rdf:resource", attribute.Value);
                        break;
                    }

                    case RdfTerm.Other:
                    {
                        if (!childIsStruct)
                        {
                            AddQualifierNode(childNode, attribute.Name.LocalName, attribute.Value);
                        }
                        else
                        {
                            if ("xml:" + attribute.Name.LocalName == XmpConstants.XmlLang)
                            {
                                AddQualifierNode(childNode, XmpConstants.XmlLang, attribute.Value);
                            }
                            else
                            {
                                AddChildNode(xmp, childNode, attribute, attribute.Value, false);
                            }
                        }
                        break;
                    }

                    default:
                    {
                        throw new XmpException("Unrecognized attribute of empty property element", XmpErrorCode.BadRdf);
                    }
                }
            }
        }

        /// <summary>Adds a child node.</summary>
        /// <param name="xmp">the xmp metadata object that is generated</param>
        /// <param name="xmpParent">the parent xmp node</param>
        /// <param name="xmlNode">the currently processed XML node</param>
        /// <param name="value">Node value</param>
        /// <param name="isTopLevel">Flag if the node is a top-level node</param>
        /// <returns>Returns the newly created child node.</returns>
        /// <exception cref="XmpException">thrown on parsing errors</exception>
        private static XmpNode AddChildNode(XmpMeta xmp, XmpNode xmpParent, XElement xmlNode, string value, bool isTopLevel)
        {
            return AddChildNode(xmp, xmpParent, xmlNode.Name, xmlNode.GetPrefixOfNamespace(xmlNode.Name.Namespace), value, isTopLevel);
        }

        private static XmpNode AddChildNode(XmpMeta xmp, XmpNode xmpParent, XAttribute xmlNode, string value, bool isTopLevel)
        {
            return AddChildNode(xmp, xmpParent, xmlNode.Name, xmlNode.Parent.GetPrefixOfNamespace(xmlNode.Name.Namespace), value, isTopLevel);
        }

        private static XmpNode AddChildNode(XmpMeta xmp, XmpNode xmpParent, XName nodeName, string nodeNamespacePrefix, string value, bool isTopLevel)
        {
            var registry = XmpMetaFactory.SchemaRegistry;
            var ns = nodeName.NamespaceName;
            string childName;
            if (ns != string.Empty)
            {
                if (ns == XmpConstants.NsDcDeprecated)
                {
                    // Fix a legacy DC namespace
                    ns = XmpConstants.NsDC;
                }

                var prefix = registry.GetNamespacePrefix(ns);

                if (prefix == null)
                {
                    prefix = nodeNamespacePrefix ?? DefaultPrefix;
                    prefix = registry.RegisterNamespace(ns, prefix);
                }

                childName = prefix + nodeName.LocalName;
            }
            else
            {
                throw new XmpException("XML namespace required for all elements and attributes", XmpErrorCode.BadRdf);
            }
            // create schema node if not already there
            var childOptions = new PropertyOptions();
            var isAlias = false;
            if (isTopLevel)
            {
                // Lookup the schema node, adjust the XMP parent pointer.
                // Incoming parent must be the tree root.
                var schemaNode = XmpNodeUtils.FindSchemaNode(xmp.GetRoot(), ns, DefaultPrefix, true);
                schemaNode.IsImplicit = false;
                // Clear the implicit node bit.
                // need runtime check for proper 32 bit code.
                xmpParent = schemaNode;
                // If this is an alias set the alias flag in the node
                // and the hasAliases flag in the tree.
                if (registry.FindAlias(childName) != null)
                {
                    isAlias = true;
                    xmp.GetRoot().HasAliases = true;
                    schemaNode.HasAliases = true;
                }
            }
            // Make sure that this is not a duplicate of a named node.
            var isArrayItem = childName == "rdf:li";
            var isValueNode = childName == "rdf:value";
            // Create XMP node and so some checks
            var newChild = new XmpNode(childName, value, childOptions) {IsAlias = isAlias};
            // Add the new child to the XMP parent node, a value node first.
            if (!isValueNode)
            {
                xmpParent.AddChild(newChild);
            }
            else
            {
                xmpParent.AddChild(1, newChild);
            }
            if (isValueNode)
            {
                if (isTopLevel || !xmpParent.Options.IsStruct)
                {
                    throw new XmpException("Misplaced rdf:value element", XmpErrorCode.BadRdf);
                }
                xmpParent.HasValueChild = true;
            }
            if (isArrayItem)
            {
                if (!xmpParent.Options.IsArray)
                {
                    throw new XmpException("Misplaced rdf:li element", XmpErrorCode.BadRdf);
                }
                newChild.Name = XmpConstants.ArrayItemName;
            }
            return newChild;
        }

        /// <summary>Adds a qualifier node.</summary>
        /// <param name="xmpParent">the parent xmp node</param>
        /// <param name="name">
        /// the name of the qualifier which has to be
        /// QName including the <b>default prefix</b>
        /// </param>
        /// <param name="value">the value of the qualifier</param>
        /// <returns>Returns the newly created child node.</returns>
        /// <exception cref="XmpException">thrown on parsing errors</exception>
        private static void AddQualifierNode(XmpNode xmpParent, string name, string value)
        {
            // normalize value of language qualifiers
            if (name == XmpConstants.XmlLang)
                value = Utils.NormalizeLangValue(value);

            xmpParent.AddQualifier(new XmpNode(name, value, null));
        }

        /// <summary>The parent is an RDF pseudo-struct containing an rdf:value field.</summary>
        /// <remarks>
        /// The parent is an RDF pseudo-struct containing an rdf:value field. Fix the
        /// XMP data model. The rdf:value node must be the first child, the other
        /// children are qualifiers. The form, value, and children of the rdf:value
        /// node are the real ones. The rdf:value node's qualifiers must be added to
        /// the others.
        /// </remarks>
        /// <param name="xmpParent">the parent xmp node</param>
        /// <exception cref="XmpException">thrown on parsing errors</exception>
        private static void FixupQualifiedNode(XmpNode xmpParent)
        {
            Debug.Assert(xmpParent.Options.IsStruct && xmpParent.HasChildren);
            var valueNode = xmpParent.GetChild(1);
            Debug.Assert(valueNode.Name == "rdf:value");
            // Move the qualifiers on the value node to the parent.
            // Make sure an xml:lang qualifier stays at the front.
            // Check for duplicate names between the value node's qualifiers and the parent's children.
            // The parent's children are about to become qualifiers. Check here, between the groups.
            // Intra-group duplicates are caught by XMPNode#addChild(...).
            if (valueNode.Options.HasLanguage)
            {
                if (xmpParent.Options.HasLanguage)
                {
                    throw new XmpException("Redundant xml:lang for rdf:value element", XmpErrorCode.BadXmp);
                }
                var langQual = valueNode.GetQualifier(1);
                valueNode.RemoveQualifier(langQual);
                xmpParent.AddQualifier(langQual);
            }
            // Start the remaining copy after the xml:lang qualifier.
            for (var i = 1; i <= valueNode.GetQualifierLength(); i++)
            {
                var qualifier = valueNode.GetQualifier(i);
                xmpParent.AddQualifier(qualifier);
            }
            // Change the parent's other children into qualifiers.
            // This loop starts at 1, child 0 is the rdf:value node.
            for (var i1 = 2; i1 <= xmpParent.GetChildrenLength(); i1++)
            {
                var qualifier = xmpParent.GetChild(i1);
                xmpParent.AddQualifier(qualifier);
            }
            // Move the options and value last, other checks need the parent's original options.
            // Move the value node's children to be the parent's children.
            Debug.Assert(xmpParent.Options.IsStruct || xmpParent.HasValueChild);
            xmpParent.HasValueChild = false;
            xmpParent.Options.IsStruct = false;
            xmpParent.Options.MergeWith(valueNode.Options);
            xmpParent.Value = valueNode.Value;
            xmpParent.RemoveChildren();
            for (var it = valueNode.IterateChildren(); it.HasNext();)
            {
                var child = (XmpNode)it.Next();
                xmpParent.AddChild(child);
            }
        }

        /// <summary>Checks if the node is a white space.</summary>
        /// <param name="node">an XML-node</param>
        /// <returns>
        /// Returns whether the node is a whitespace node,
        /// i.e. a text node that contains only whitespaces.
        /// </returns>
        private static bool IsWhitespaceNode(XNode node)
        {
            return node.NodeType == XmlNodeType.Text && ((XText)node).Value
#if PORTABLE
                                                                                    .ToCharArray()
                                                                            #endif
                       .All(char.IsWhiteSpace);
        }

        /// <summary>
        /// 7.2.6 propertyElementURIs
        /// anyURI - ( coreSyntaxTerms | rdf:Description | oldTerms )
        /// </summary>
        /// <param name="term">the term id</param>
        /// <returns>Return true if the term is a property element name.</returns>
        private static bool IsPropertyElementName(RdfTerm term)
        {
            if (term == RdfTerm.Description || IsOldTerm(term))
            {
                return false;
            }
            return (!IsCoreSyntaxTerm(term));
        }

        /// <summary>
        /// 7.2.4 oldTerms<br />
        /// rdf:aboutEach | rdf:aboutEachPrefix | rdf:bagID
        /// </summary>
        /// <param name="term">the term id</param>
        /// <returns>Returns true if the term is an old term.</returns>
        private static bool IsOldTerm(RdfTerm term)
        {
            return RdfTerm.FirstOld <= term && term <= RdfTerm.LastOld;
        }

        /// <summary>
        /// 7.2.2 coreSyntaxTerms<br />
        /// rdf:RDF | rdf:ID | rdf:about | rdf:parseType | rdf:resource | rdf:nodeID |
        /// rdf:datatype
        /// </summary>
        /// <param name="term">the term id</param>
        /// <returns>Return true if the term is a core syntax term</returns>
        private static bool IsCoreSyntaxTerm(RdfTerm term)
        {
            return RdfTerm.FirstCore <= term && term <= RdfTerm.LastCore;
        }

        /// <summary>Determines the ID for a certain RDF Term.</summary>
        /// <remarks>
        /// Determines the ID for a certain RDF Term.
        /// Arranged to hopefully minimize the parse time for large XMP.
        /// </remarks>
        /// <param name="node">an XML node</param>
        /// <returns>Returns the term ID.</returns>
        private static RdfTerm GetRdfTermKind(XElement node)
        {
            return GetRdfTermKind(node.Name, node.NodeType);
        }

        private static RdfTerm GetRdfTermKind(XAttribute node)
        {
            return GetRdfTermKind(node.Name, node.NodeType, node.Parent.Name);
        }

        private static RdfTerm GetRdfTermKind(XName name, XmlNodeType nodeType, XName parentName = null)
        {
            var localName = name.LocalName;
            var ns = name.NamespaceName;
            var parentNamespaceName = (parentName != null ? parentName.NamespaceName : string.Empty);

            if (ns == string.Empty && (localName == "about" || localName == "ID") && (nodeType == XmlNodeType.Attribute) && parentNamespaceName == XmpConstants.NsRdf)
                ns = XmpConstants.NsRdf;

            if (ns == XmpConstants.NsRdf)
            {
                switch (localName)
                {
                    case "li":
                        return RdfTerm.Li;
                    case "parseType":
                        return RdfTerm.ParseType;
                    case "Description":
                        return RdfTerm.Description;
                    case "about":
                        return RdfTerm.About;
                    case "resource":
                        return RdfTerm.Resource;
                    case "RDF":
                        return RdfTerm.Rdf;
                    case "ID":
                        return RdfTerm.Id;
                    case "nodeID":
                        return RdfTerm.NodeId;
                    case "datatype":
                        return RdfTerm.Datatype;
                    case "aboutEach":
                        return RdfTerm.AboutEach;
                    case "aboutEachPrefix":
                        return RdfTerm.AboutEachPrefix;
                    case "bagID":
                        return RdfTerm.BagId;
                }
            }

            return RdfTerm.Other;
        }
    }
}