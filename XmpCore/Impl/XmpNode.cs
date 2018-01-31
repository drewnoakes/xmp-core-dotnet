//=================================================================================================
// ADOBE SYSTEMS INCORPORATED
// Copyright 2006 Adobe Systems Incorporated
// All Rights Reserved
//
// NOTICE:  Adobe permits you to use, modify, and distribute this file in accordance with the terms
// of the Adobe license agreement accompanying it.
// =================================================================================================


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sharpen;
using XmpCore.Options;

namespace XmpCore.Impl
{
    /// <summary>
    /// A node in the internally XMP tree, which can be a schema node, a property node, an array node,
    /// an array item, a struct node or a qualifier node (without '?').
    /// </summary>
    /// <remarks>
    /// A node in the internally XMP tree, which can be a schema node, a property node, an array node,
    /// an array item, a struct node or a qualifier node (without '?').
    /// Possible improvements:
    /// 1. The kind Node of node might be better represented by a class-hierarchy of different nodes.
    /// 2. The array type should be an enum
    /// 3. isImplicitNode should be removed completely and replaced by return values of fi.
    /// 4. hasLanguage, hasType should be automatically maintained by XMPNode
    /// </remarks>
    /// <since>21.02.2006</since>
    public sealed class XmpNode : IComparable
    {
        /// <summary>list of child nodes, lazy initialized</summary>
        private List<XmpNode> _children;
        private Dictionary<string, XmpNode> _childrenLookup;

        /// <summary>list of qualifier of the node, lazy initialized</summary>
        private List<XmpNode> _qualifier;

        /// <summary>options describing the kind of the node</summary>
        private PropertyOptions _options;

        /// <summary>Creates an <c>XMPNode</c> with initial values.</summary>
        /// <param name="name">the name of the node</param>
        /// <param name="value">the value of the node</param>
        /// <param name="options">the options of the node</param>
        public XmpNode(string name, string value, PropertyOptions options)
        {
            // internal processing options
            Name = name;
            Value = value;
            _options = options;
        }

        /// <summary>Constructor for the node without value.</summary>
        /// <param name="name">the name of the node</param>
        /// <param name="options">the options of the node</param>
        public XmpNode(string name, PropertyOptions options)
            : this(name, null, options)
        {
        }

        /// <summary>Resets the node.</summary>
        public void Clear()
        {
            _options = null;
            Name = null;
            Value = null;
            _children = null;
            _childrenLookup = null;
            _qualifier = null;
        }

        /// <summary>
        /// Get the parent node.
        /// </summary>
        public XmpNode Parent { get; private set; }

        /// <param name="index">an index [1..size]</param>
        /// <returns>Returns the child with the requested index.</returns>
        public XmpNode GetChild(int index)
        {
            return GetChildren()[index - 1];
        }

        /// <summary>Adds a node as child to this node.</summary>
        /// <param name="node">an XMPNode</param>
        /// <exception cref="XmpException"></exception>
        public void AddChild(XmpNode node)
        {
            // check for duplicate properties
            AssertChildNotExisting(node.Name);
            node.Parent = this;
            GetChildren().Add(node);

            // Note: If this is called from ParseRdf.AddChildNode, any nodes added with the name "rdf:li" are renamed at
            // the end to XmpConstants.ArrayItemName. This implies 1-n "rdf:li" nodes are expected and we should ignore
            // them in the lookup table
            if (node.Name != "rdf:li" && node.Name != XmpConstants.ArrayItemName)
                GetChildrenLookup()[node.Name] = node;
        }

        /// <summary>Adds a node as child to this node.</summary>
        /// <param name="index">
        /// the index of the node <em>before</em> which the new one is inserted.
        /// <em>Note:</em> The node children are indexed from [1..size]!
        /// An index of size + 1 appends a node.
        /// </param>
        /// <param name="node">an XMPNode</param>
        /// <exception cref="XmpException"></exception>
        public void AddChild(int index, XmpNode node)
        {
            AssertChildNotExisting(node.Name);
            node.Parent = this;
            GetChildren().Insert(index - 1, node);

            // Note: If this is called from ParseRdf.AddChildNode, any nodes added with the name "rdf:li" are renamed at
            // the end to XmpConstants.ArrayItemName. This implies 1-n "rdf:li" nodes are expected and we should ignore
            // them in the lookup table
            if (node.Name != "rdf:li" && node.Name != XmpConstants.ArrayItemName)
                GetChildrenLookup()[node.Name] = node;
        }

        /// <summary>Replaces a node with another one.</summary>
        /// <param name="index">
        /// the index of the node that will be replaced.
        /// <em>Note:</em> The node children are indexed from [1..size]!
        /// </param>
        /// <param name="node">the replacement XMPNode</param>
        public void ReplaceChild(int index, XmpNode node)
        {
            node.Parent = this;
            GetChildren()[index - 1] = node;
            GetChildrenLookup()[node.Name] = node;
        }

        /// <summary>Removes a child at the requested index.</summary>
        /// <param name="itemIndex">the index to remove [1..size]</param>
        public void RemoveChild(int itemIndex)
        {
            GetChildrenLookup().Remove(GetChildren()[itemIndex - 1].Name);

            GetChildren().RemoveAt(itemIndex - 1);
            CleanupChildren();
        }

        /// <summary>Removes a child node.</summary>
        /// <remarks>
        /// Removes a child node.
        /// If its a schema node and doesn't have any children anymore, its deleted.
        /// </remarks>
        /// <param name="node">the child node to delete.</param>
        public void RemoveChild(XmpNode node)
        {
            GetChildrenLookup().Remove(node.Name);

            GetChildren().Remove(node);
            CleanupChildren();
        }

        /// <summary>
        /// Removes the children list if this node has no children anymore;
        /// checks if the provided node is a schema node and doesn't have any children anymore,
        /// its deleted.
        /// </summary>
        private void CleanupChildren()
        {
            if (_children.Count == 0)
            {
                _children = null;
                _childrenLookup = null;
            }
        }

        /// <summary>Removes all children from the node.</summary>
        public void RemoveChildren()
        {
            _children = null;
            _childrenLookup = null;
        }

        /// <returns>Returns the number of children without necessarily creating a list.</returns>
        public int GetChildrenLength() => _children?.Count ?? 0;

        /// <param name="expr">child node name to look for</param>
        /// <returns>Returns an <c>XMPNode</c> if node has been found, <c>null</c> otherwise.</returns>
        public XmpNode FindChildByName(string expr) => Find(GetChildrenLookup(), expr);

        /// <param name="index">an index [1..size]</param>
        /// <returns>Returns the qualifier with the requested index.</returns>
        public XmpNode GetQualifier(int index) => GetQualifier()[index - 1];

        /// <returns>Returns the number of qualifier without necessarily creating a list.</returns>
        public int GetQualifierLength() => _qualifier?.Count ?? 0;

        /// <summary>Appends a qualifier to the qualifier list and sets respective options.</summary>
        /// <param name="qualNode">a qualifier node.</param>
        /// <exception cref="XmpException"></exception>
        public void AddQualifier(XmpNode qualNode)
        {
            AssertQualifierNotExisting(qualNode.Name);
            qualNode.Parent = this;
            qualNode.Options.IsQualifier = true;
            Options.HasQualifiers = true;
            // constraints
            if (qualNode.IsLanguageNode)
            {
                // "xml:lang" is always first and the option "hasLanguage" is set
                _options.HasLanguage = true;
                GetQualifier().Insert(0, qualNode);
            }
            else
            {
                if (qualNode.IsTypeNode)
                {
                    // "rdf:type" must be first or second after "xml:lang" and the option "hasType" is set
                    _options.HasType = true;
                    GetQualifier().Insert(!_options.HasLanguage ? 0 : 1, qualNode);
                }
                else
                {
                    // other qualifiers are appended
                    GetQualifier().Add(qualNode);
                }
            }
        }

        /// <summary>Removes one qualifier node and fixes the options.</summary>
        /// <param name="qualNode">qualifier to remove</param>
        public void RemoveQualifier(XmpNode qualNode)
        {
            var opts = Options;
            if (qualNode.IsLanguageNode)
            {
                // if "xml:lang" is removed, remove hasLanguage-flag too
                opts.HasLanguage = false;
            }
            else
            {
                if (qualNode.IsTypeNode)
                {
                    // if "rdf:type" is removed, remove hasType-flag too
                    opts.HasType = false;
                }
            }
            GetQualifier().Remove(qualNode);
            if (_qualifier.Count == 0)
            {
                opts.HasQualifiers = false;
                _qualifier = null;
            }
        }

        /// <summary>Removes all qualifiers from the node and sets the options appropriate.</summary>
        public void RemoveQualifiers()
        {
            var opts = Options;
            // clear qualifier related options
            opts.HasQualifiers = false;
            opts.HasLanguage = false;
            opts.HasType = false;
            _qualifier = null;
        }

        /// <param name="expr">qualifier node name to look for</param>
        /// <returns>
        /// Returns a qualifier <c>XMPNode</c> if node has been found,
        /// <c>null</c> otherwise.
        /// </returns>
        public XmpNode FindQualifierByName(string expr) => Find(_qualifier, expr);

        /// <summary>
        /// Get whether the node has children.
        /// </summary>
        public bool HasChildren => _children != null && _children.Count > 0;

        /// <returns>
        /// Returns an iterator for the children.
        /// <em>Note:</em> take care to use it.remove(), as the flag are not adjusted in that case.
        /// </returns>
        public IIterator IterateChildren()
        {
            return _children != null
                ? (IIterator)GetChildren().Iterator()
                : Enumerable.Empty<object>().Iterator();
        }

        /// <summary>
        /// Returns whether the node has qualifier attached.
        /// </summary>
        public bool HasQualifier => _qualifier != null && _qualifier.Count > 0;

        /// <returns>
        /// Returns an iterator for the qualifier.
        /// <em>Note:</em> take care to use it.remove(), as the flag are not adjusted in that case.
        /// </returns>
        public IIterator IterateQualifier()
        {
            return _qualifier != null
                ? (IIterator)new Iterator391(GetQualifier().Iterator())
                : Enumerable.Empty<object>().Iterator();
        }

        /// <summary>
        /// Iterator that disallows removal.
        /// </summary>
        private sealed class Iterator391 : IIterator
        {
            public Iterator391(IIterator it) => _it = it;
            public bool HasNext() => _it.HasNext();
            public object Next() => _it.Next();
            public void Remove() => throw new NotSupportedException("remove() is not allowed due to the internal constraints");
            private readonly IIterator _it;
        }

        /// <summary>Performs a <b>deep clone</b> of the node and the complete subtree.</summary>
        public object Clone()
        {
            PropertyOptions newOptions;
            try
            {
                newOptions = new PropertyOptions(Options.GetOptions());
            }
            catch (XmpException)
            {
                // cannot happen
                newOptions = new PropertyOptions();
            }
            var newNode = new XmpNode(Name, Value, newOptions);
            CloneSubtree(newNode);
            return newNode;
        }

        /// <summary>
        /// Performs a <b>deep clone</b> of the complete subtree (children and
        /// qualifier )into and add it to the destination node.
        /// </summary>
        /// <param name="destination">the node to add the cloned subtree</param>
        public void CloneSubtree(XmpNode destination)
        {
            try
            {
                for (var it = IterateChildren(); it.HasNext();)
                {
                    var child = (XmpNode)it.Next();
                    destination.AddChild((XmpNode)child.Clone());
                }
                for (var it1 = IterateQualifier(); it1.HasNext();)
                {
                    var qualifier = (XmpNode)it1.Next();
                    destination.AddQualifier((XmpNode)qualifier.Clone());
                }
            }
            catch (XmpException)
            {
                // cannot happen (duplicate childs/quals do not exist in this node)
                Debug.Assert(false);
            }
        }

        /// <summary>Renders this node and the tree under this node in a human readable form.</summary>
        /// <param name="recursive">Flag is qualifier and child nodes shall be rendered too</param>
        /// <returns>Returns a multiline string containing the dump.</returns>
        public string DumpNode(bool recursive)
        {
            var result = new StringBuilder(512);
            DumpNode(result, recursive, 0, 0);
            return result.ToString();
        }

        public int CompareTo(object xmpNode)
        {
            return Options.IsSchemaNode
                ? string.CompareOrdinal(Value, ((XmpNode)xmpNode).Value)
                : string.CompareOrdinal(Name, ((XmpNode)xmpNode).Name);
        }

        public string Name { set; get; }

        public string Value { get; set; }

        public PropertyOptions Options
        {
            get => _options ?? (_options = new PropertyOptions());
            set => _options = value;
        }

        /// <summary>
        /// Get and set the implicit node flag.
        /// </summary>
        public bool IsImplicit { get; set; }

        /// <summary>
        /// Get and set whether the node contains aliases (applies only to schema nodes).
        /// </summary>
        public bool HasAliases { get; set; }

        /// <summary>
        /// Get and set whether this node is an alias (applies only to schema nodes).
        /// </summary>
        public bool IsAlias { get; set; }

        /// <summary>
        /// Get and set whether this node has an <c>rdf:value</c> child node.
        /// </summary>
        public bool HasValueChild { get; set; }

        /// <summary>
        /// Sorts the XMP node and its children, recursively.
        /// </summary>
        /// <remarks>
        /// Sorting occurs according to the following rules:
        /// <list type="bullet">
        /// <item>Nodes at one level are sorted by name, that is prefix + local name</item>
        /// <item>Starting at the root node the children and qualifier are sorted recursively,
        /// which the following exceptions.</item>
        /// <item>Sorting will not be used for arrays.</item>
        /// <item>Within qualifier "xml:lang" and/or "rdf:type" stay at the top in that order,
        /// all others are sorted.</item>
        /// </list>
        /// </remarks>
        public void Sort()
        {
            // sort qualifier
            if (HasQualifier)
                GetQualifier().Sort((a, b) => QualifierOrderComparer.Default.Compare(a.Name, b.Name));

            // sort children
            if (_children != null)
            {
                if (!Options.IsArray)
                    _children.Sort();

                foreach (var child in _children)
                    child.Sort();
            }
        }

        /// <summary>Dumps this node and its qualifier and children recursively.</summary>
        /// <remarks>
        /// Dumps this node and its qualifier and children recursively.
        /// <em>Note:</em> It creats empty options on every node.
        /// </remarks>
        /// <param name="result">the buffer to append the dump.</param>
        /// <param name="recursive">Flag is qualifier and child nodes shall be rendered too</param>
        /// <param name="indent">the current indent level.</param>
        /// <param name="index">the index within the parent node (important for arrays)</param>
        private void DumpNode(StringBuilder result, bool recursive, int indent, int index)
        {
            // write indent
            for (var i = 0; i < indent; i++)
                result.Append('\t');

            // render Node
            if (Parent != null)
            {
                if (Options.IsQualifier)
                {
                    result.Append('?');
                    result.Append(Name);
                }
                else if (Parent.Options.IsArray)
                {
                    result.Append('[');
                    result.Append(index);
                    result.Append(']');
                }
                else
                {
                    result.Append(Name);
                }
            }
            else
            {
                // applies only to the root node
                result.Append("ROOT NODE");
                if (!string.IsNullOrEmpty(Name))
                {
                    // the "about" attribute
                    result.Append(" (");
                    result.Append(Name);
                    result.Append(')');
                }
            }

            if (!string.IsNullOrEmpty(Value))
            {
                result.Append(" = \"");
                result.Append(Value);
                result.Append('"');
            }

            // render options if at least one is set
            if (Options.ContainsOneOf(unchecked((int)0xffffffff)))
            {
                result.Append("\t(");
                result.Append(Options);
                result.Append(" : ");
                result.Append(Options.GetOptionsString());
                result.Append(')');
            }
            result.Append('\n');

            // render qualifier
            if (recursive && HasQualifier)
            {
                var i = 0;
                foreach (var qual in GetQualifier().OrderBy(q => q.Name, QualifierOrderComparer.Default))
                    qual.DumpNode(result, recursive, indent + 2, ++i);
            }
            // render children
            if (recursive && HasChildren)
            {
                var i = 0;
                foreach (var child in GetChildren().OrderBy(c => c))
                    child.DumpNode(result, recursive, indent + 1, ++i);
            }
        }

        /// <summary>
        /// Get whether this node is a language qualifier.
        /// </summary>
        private bool IsLanguageNode => Name == XmpConstants.XmlLang;

        /// <summary>
        /// Get whether this node is a type qualifier.
        /// </summary>
        private bool IsTypeNode => Name == "rdf:type";

        /// <summary>
        /// <em>Note:</em> This method should always be called when accessing 'children' to be sure
        /// that its initialized.
        /// </summary>
        /// <returns>Returns list of children that is lazy initialized.</returns>
        private List<XmpNode> GetChildren() => _children ?? (_children = new List<XmpNode>(0));

        /// <summary>
        /// <em>Note:</em> This method should always be called when accessing 'children' lookup to be sure
        /// that its initialized. This is used in the Find function to make searching large lists of children more efficient.
        /// </summary>
        /// <returns>Returns dictionary of children lookup that is lazy initialized.</returns>
        private Dictionary<string, XmpNode> GetChildrenLookup() => _childrenLookup ?? (_childrenLookup = new Dictionary<string, XmpNode>(0));

        /// <returns>Returns a read-only copy of child nodes list.</returns>
        public IEnumerable<object> GetUnmodifiableChildren() => GetChildren().Cast<object>().ToList();

        /// <returns>Returns list of qualifier that is lazy initialized.</returns>
        private List<XmpNode> GetQualifier() => _qualifier ?? (_qualifier = new List<XmpNode>(0));

        /// <summary>Internal find.</summary>
        /// <param name="list">the list to search in</param>
        /// <param name="expr">the search expression</param>
        /// <returns>Returns the found node or <c>nulls</c>.</returns>
        private static XmpNode Find(IEnumerable<XmpNode> list, string expr) => list?.FirstOrDefault(node => node.Name == expr);

        /// <summary>Internal find from a lookup Dictionary.</summary>
        /// <param name="lookup">the lookup Dictionary to search in</param>
        /// <param name="expr">the search expression</param>
        /// <returns>Returns the found node or <c>nulls</c>.</returns>
        private static XmpNode Find(Dictionary<string, XmpNode> lookup, string expr)
        {
            XmpNode ret = null;
            lookup.TryGetValue(expr, out ret);
            return ret;
        }

        /// <summary>Checks that a node name is not existing on the same level, except for array items.</summary>
        /// <param name="childName">the node name to check</param>
        /// <exception cref="XmpException">Thrown if a node with the same name is existing.</exception>
        private void AssertChildNotExisting(string childName)
        {
            if (childName != XmpConstants.ArrayItemName && FindChildByName(childName) != null)
                throw new XmpException("Duplicate property or field node '" + childName + "'", XmpErrorCode.BadXmp);
        }

        /// <summary>Checks that a qualifier name is not existing on the same level.</summary>
        /// <param name="qualifierName">the new qualifier name</param>
        /// <exception cref="XmpException">Thrown if a node with the same name is existing.</exception>
        private void AssertQualifierNotExisting(string qualifierName)
        {
            if (qualifierName != XmpConstants.ArrayItemName && FindQualifierByName(qualifierName) != null)
                throw new XmpException("Duplicate '" + qualifierName + "' qualifier", XmpErrorCode.BadXmp);
        }

        private sealed class QualifierOrderComparer : IComparer<string>
        {
            public static readonly QualifierOrderComparer Default = new QualifierOrderComparer();

            public int Compare(string x, string y)
            {
                if (string.Equals(x, y))
                    return 0;

                const string xml = XmpConstants.XmlLang;
                const string rdf = "rdf:type"; // TODO extract to a constant too

                switch (x)
                {
                    case xml:
                        return -1;
                    case rdf:
                        return y == xml ? 1 : -1;
                    default:
                        return 0;
                }
            }
        }
    }
}
