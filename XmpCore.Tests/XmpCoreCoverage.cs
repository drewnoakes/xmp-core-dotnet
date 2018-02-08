// =================================================================================================
// ADOBE SYSTEMS INCORPORATED
// Copyright 2006 Adobe Systems Incorporated
// All Rights Reserved
//
// NOTICE:  Adobe permits you to use, modify, and distribute this file in accordance with the terms
// of the Adobe license agreement accompanying it.
// =================================================================================================


using System;
using System.Text;
using Sharpen;
using XmpCore.Impl;
using XmpCore.Options;
using Xunit;
using Xunit.Abstractions;

namespace XmpCore.Tests
{
    // TODO add assertions rather than logging output

    public class XmpCoreCoverage
    {
        private readonly IXmpSchemaRegistry _registry = XmpMetaFactory.SchemaRegistry;
        private readonly ITestOutputHelper _log;

        public XmpCoreCoverage(ITestOutputHelper log) => _log = log;

        [Fact]
        public void DoCoreCoverage()
        {
            CoverNamespaceRegistry();

            CoverAliasRegistry();

            CoverCreatingXmp();

            var meta = CoverSetPropertyMethods();

            CoverGetPropertyMethods(meta);

            CoverExistingProperties(meta);

            CoverDeleteProperties(meta);

            CoverLocalisedProperties();

            CoverLiteralProperties();

            CoverParsing();

            CoverLinefeedValues();

            CoverSerialization();

            CoverIterator();

            CoverPathCreation();

            CoverDateTime();
        }

        /**
         * List predefined namespaces and aliases;
         * register new namespaces and aliases.
         * @throws XmpException Forward exceptions
         */
        private void CoverNamespaceRegistry()
        {
            WriteMajorLabel("Test of namespace registry");

            // Lists of predefined namespaces
            //TODO reinstate this code

            WriteMinorLabel("List predefined namespaces");
            var namespaces = _registry.Namespaces;
            foreach (var pair in namespaces)
                _log.WriteLine(pair.Key + "   --->   " + pair.Value);


            // Registry namespace functions
            WriteMinorLabel("Test namespace registry functions");

            var prefix = _registry.RegisterNamespace(TestData.NS1, "ns1");
            _log.WriteLine("registerNamespace ns1:   {0}   --->   {1}", prefix, _registry.GetNamespaceUri(prefix));

            prefix = _registry.RegisterNamespace(TestData.NS2, "ns2");
            _log.WriteLine("registerNamespace ns2:   {0}   --->   {1}", prefix, _registry.GetNamespaceUri(prefix));

            prefix = _registry.GetNamespacePrefix(TestData.NS1);
            _log.WriteLine("getNamespacePrefix ns1:   {0}", prefix);

            _log.WriteLine("getNamespaceURI ns1:   {0}", _registry.GetNamespaceUri("ns1"));

            prefix = _registry.GetNamespacePrefix("bogus");
            _log.WriteLine("getNamespacePrefix bogus:   {0}", prefix);

            _log.WriteLine("getNamespaceURI ns1:   {0}", _registry.GetNamespaceUri("bogus"));
        }

        /**
         * List predefined aliases, register new aliases and resolve aliases.
         * @throws XmpException Forward exceptions
         */
        private void CoverAliasRegistry()
        {
            WriteMajorLabel("Test alias registry and functions");
            DumpAliases();

            // Register new aliases
            WriteMinorLabel("Add ns2: to ns1: aliases");

            DumpAliases();


            // Resolve aliases
            WriteMinorLabel("Resolve ns2: to ns1: aliases");

            var aliasInfo = _registry.ResolveAlias(TestData.NS1, "SimpleActual");
            _log.WriteLine("ResolveAlias ns1:SimpleActual:   " + aliasInfo + "   (wrong way!)");

            aliasInfo = _registry.ResolveAlias(TestData.NS2, "SimpleAlias");
            _log.WriteLine("ResolveAlias ns2:SimpleAlias:   " + aliasInfo);


            aliasInfo = _registry.ResolveAlias(TestData.NS2, "BagAlias");
            _log.WriteLine("ResolveAlias ns2:BagAlias:   " + aliasInfo);

            aliasInfo = _registry.ResolveAlias(TestData.NS2, "SeqAlias");
            _log.WriteLine("ResolveAlias ns2:SeqAlias:   " + aliasInfo);

            aliasInfo = _registry.ResolveAlias(TestData.NS2, "AltAlias");
            _log.WriteLine("ResolveAlias ns2:AltAlias:   " + aliasInfo);

            aliasInfo = _registry.ResolveAlias(TestData.NS2, "AltTextAlias");
            _log.WriteLine("ResolveAlias ns2:AltTextAlias:   " + aliasInfo);


            aliasInfo = _registry.ResolveAlias(TestData.NS2, "BagItemAlias");
            _log.WriteLine("ResolveAlias ns2:BagItemAlias:   " + aliasInfo);

            aliasInfo = _registry.ResolveAlias(TestData.NS2, "SeqItemAlias");
            _log.WriteLine("ResolveAlias ns2:SeqItemAlias:   " + aliasInfo);

            aliasInfo = _registry.ResolveAlias(TestData.NS2, "AltItemAlias");
            _log.WriteLine("ResolveAlias ns2:AltItemAlias:   " + aliasInfo);

            aliasInfo = _registry.ResolveAlias(TestData.NS2, "AltTextItemAlias");
            _log.WriteLine("ResolveAlias ns2:AltTextItemAlias:   " + aliasInfo);


            // set alias properties
            WriteMinorLabel("Test SetProperty through ns2: simple aliases");

            var meta = XmpMetaFactory.Create();
            meta.SetProperty(TestData.NS2, "SimpleAlias", "Simple value");
            meta.SetProperty(TestData.NS2, "ns2:BagItemAlias", "BagItem value");
            meta.SetProperty(TestData.NS2, "SeqItemAlias", "SeqItem value");
            meta.SetProperty(TestData.NS2, "AltItemAlias", "AltItem value");
            meta.SetProperty(TestData.NS2, "AltTextItemAlias", "AltTextItem value");
            PrintXmpMeta(meta, "Check for aliases and bases");


            // delete aliases
            WriteMinorLabel("Delete some ns2: to ns1: aliases");

            DumpAliases();
        }

        /**
         * Test simple constructors and parsing, setting the instance ID
         * @throws XmpException Forwards exceptions
         */
        private void CoverCreatingXmp()
        {
            WriteMajorLabel("Test simple constructors and parsing, setting the instance ID");

            var meta1 = XmpMetaFactory.Create();
            PrintXmpMeta(meta1, "Empty XMP object");

            var meta2 = XmpMetaFactory.Create();
            meta2.SetObjectName("New object name");
            PrintXmpMeta(meta2, "XMP object with name");

            var meta3 = XmpMetaFactory.ParseFromString(TestData.RDF_COVERAGE);
            PrintXmpMeta(meta3, "Construct and parse from buffer");

            meta3.SetProperty(XmpConstants.NsXmpMm, "InstanceID", "meta2:Original");
            PrintXmpMeta(meta3, "Add instance ID");

            var meta4 = (XmpMeta)meta3.Clone();
            meta4.SetProperty(XmpConstants.NsXmpMm, "InstanceID", "meta2:Clone");
            PrintXmpMeta(meta3, "Clone and add instance ID");
        }

        /**
         * Cover some basid set calls (including arrays and structs).
         * @return Returns an <code>XmpMeta</code> object that is reused in the next examples.
         * @throws XmpException Forwards Exceptions
         */
        private IXmpMeta CoverSetPropertyMethods()
        {
            // Basic set/get methods
            WriteMajorLabel("Test SetProperty and related methods");

            var meta = XmpMetaFactory.Create();
            meta.SetProperty(TestData.NS1, "Prop", "Prop value");
            meta.SetProperty(TestData.NS1, "ns1:XMLProp", "<PropValue/>");
            meta.SetProperty(TestData.NS1, "ns1:URIProp", "URI:value/", new PropertyOptions {IsUri = true});

            meta.AppendArrayItem(TestData.NS1, "Bag", new PropertyOptions {IsArray = true}, "BagItem value", null);
            meta.AppendArrayItem(TestData.NS1, "ns1:Seq", new PropertyOptions {IsArrayOrdered = true}, "SeqItem value", null);
            meta.AppendArrayItem(TestData.NS1, "ns1:Alt", new PropertyOptions {IsArrayAlternate = true}, "AltItem value", null);

            meta.SetArrayItem(TestData.NS1, "Bag", 1, "BagItem 3");
            meta.InsertArrayItem(TestData.NS1, "ns1:Bag", 1, "BagItem 1");
            meta.InsertArrayItem(TestData.NS1, "ns1:Bag", 2, "BagItem 2");
            meta.AppendArrayItem(TestData.NS1, "Bag", "BagItem 4");

            meta.SetStructField(TestData.NS1, "Struct", TestData.NS2, "Field1", "Field1 value");
            meta.SetStructField(TestData.NS1, "ns1:Struct", TestData.NS2, "Field2", "Field2 value");
            meta.SetStructField(TestData.NS1, "ns1:Struct", TestData.NS2, "Field3", "Field3 value");

            PrintXmpMeta(meta, "A few basic set property calls");

            // -----------------------------------------------------------------------------------------

            // Add some properties with qualifier
            WriteMinorLabel("Add some properties with qualifier");
            _log.WriteLine("CountArrayItems Bag = " + meta.CountArrayItems(TestData.NS1, "Bag"));

            meta.SetProperty(TestData.NS1, "QualProp1", "Prop value");
            meta.SetQualifier(TestData.NS1, "QualProp1", TestData.NS2, "Qual1", "Qual1 value");
            meta.SetProperty(TestData.NS1, "QualProp1/?ns2:Qual3", "Qual3 value");
            meta.SetProperty(TestData.NS1, "QualProp1/?xml:lang", "x-qual");

            meta.SetProperty(TestData.NS1, "QualProp2", "Prop value");
            meta.SetQualifier(TestData.NS1, "QualProp2", XmpConstants.NsXml, "lang", "en-us");
            meta.SetProperty(TestData.NS1, "QualProp2/@xml:lang", "x-attr");

            meta.SetProperty(TestData.NS1, "QualProp3", "Prop value");
            meta.SetQualifier(TestData.NS1, "ns1:QualProp3", XmpConstants.NsXml, "xml:lang", "en-us");
            meta.SetQualifier(TestData.NS1, "ns1:QualProp3", TestData.NS2, "ns2:Qual", "Qual value");

            meta.SetProperty(TestData.NS1, "QualProp4", "Prop value");
            meta.SetQualifier(TestData.NS1, "QualProp4", TestData.NS2, "Qual", "Qual value");
            meta.SetQualifier(TestData.NS1, "QualProp4", XmpConstants.NsXml, "lang", "en-us");
            PrintXmpMeta(meta, "Add some qualifiers");

            meta.SetProperty(TestData.NS1, "QualProp1", "new value");
            meta.SetProperty(TestData.NS1, "QualProp2", "new value");
            meta.SetProperty(TestData.NS1, "QualProp3", "new value");
            meta.SetProperty(TestData.NS1, "QualProp4", "new value");
            PrintXmpMeta(meta, "Change values and keep qualifiers");

            return meta;
        }

        /**
         * Test getProperty, deleteProperty and related methods.
         * @param meta a predefined <code>XmpMeta</code> object.
         * @throws XmpException Forwards exceptions
         */
        private void CoverGetPropertyMethods(IXmpMeta meta)
        {
            WriteMajorLabel("Test getProperty, deleteProperty and related methods");

            meta.DeleteProperty(TestData.NS1, "QualProp1"); // ! Start with fresh qualifiers.
            meta.DeleteProperty(TestData.NS1, "ns1:QualProp2");
            meta.DeleteProperty(TestData.NS1, "ns1:QualProp3");
            meta.DeleteProperty(TestData.NS1, "QualProp4");


            WriteMinorLabel("Set properties with qualifier");

            meta.SetProperty(TestData.NS1, "QualProp1", "Prop value");
            meta.SetQualifier(TestData.NS1, "QualProp1", TestData.NS2, "Qual1", "Qual1 value");

            meta.SetProperty(TestData.NS1, "QualProp2", "Prop value");
            meta.SetQualifier(TestData.NS1, "QualProp2", XmpConstants.NsXml, "lang", "en-us");

            meta.SetProperty(TestData.NS1, "QualProp3", "Prop value");
            meta.SetQualifier(TestData.NS1, "QualProp3", XmpConstants.NsXml, "lang", "en-us");
            meta.SetQualifier(TestData.NS1, "QualProp3", TestData.NS2, "Qual", "Qual value");

            meta.SetProperty(TestData.NS1, "QualProp4", "Prop value");
            meta.SetQualifier(TestData.NS1, "QualProp4", TestData.NS2, "Qual", "Qual value");
            meta.SetQualifier(TestData.NS1, "QualProp4", XmpConstants.NsXml, "lang", "en-us");

            PrintXmpMeta(meta, "XMP object");


            WriteMinorLabel("Get simple properties");

            var property = meta.GetProperty(TestData.NS1, "Prop");
            _log.WriteLine("getProperty ns1:Prop =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            property = meta.GetProperty(TestData.NS1, "ns1:XMLProp");
            _log.WriteLine("getProperty ns1:XMLProp =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            property = meta.GetProperty(TestData.NS1, "ns1:URIProp");
            _log.WriteLine("getProperty ns1:URIProp =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            property = meta.GetArrayItem(TestData.NS1, "Bag", 2);
            _log.WriteLine("getArrayItem ns1:Bag[2] =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            try
            {
                meta.GetArrayItem(null, "ns1:Bag", 1);
            }
            catch (XmpException e)
            {
                _log.WriteLine("getArrayItem with no schema URI - threw XmpException #" + e.ErrorCode + " :   " + e.Message + ")");
            }


            WriteMinorLabel("Get array items and struct fields");

            property = meta.GetArrayItem(TestData.NS1, "ns1:Seq", 1);
            _log.WriteLine("getArrayItem ns1:Seq[1] =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            property = meta.GetArrayItem(TestData.NS1, "ns1:Alt", 1);
            _log.WriteLine("getArrayItem ns1:Alt[1] =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            property = meta.GetStructField(TestData.NS1, "Struct", TestData.NS2, "Field1");
            _log.WriteLine("getStructField ns1:Struct/ns2:Field1 =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            property = meta.GetStructField(TestData.NS1, "ns1:Struct", TestData.NS2, "Field2");
            _log.WriteLine("getStructField ns1:Struct/ns2:Field2 =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            property = meta.GetStructField(TestData.NS1, "ns1:Struct", TestData.NS2, "ns2:Field3");
            _log.WriteLine("getStructField ns1:Struct/ns2:Field3 =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            property = meta.GetStructField(TestData.NS1, "ns1:Struct", TestData.NS2, "ns2:Field3");
            _log.WriteLine("getStructField ns1:Struct/ns2:Field3 =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            property = meta.GetStructField(TestData.NS1, "ns1:Struct", TestData.NS2, "ns2:Field3");
            _log.WriteLine("getStructField ns1:Struct/ns2:Field3 =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");


            WriteMinorLabel("Get qualifier");

            property = meta.GetQualifier(TestData.NS1, "QualProp1", TestData.NS2, "Qual1");
            _log.WriteLine("getQualifier  ns1:QualProp1/?ns2:Qual1 =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            try
            {
                meta.GetQualifier(null, "ns1:QualProp1", TestData.NS2, "Qual1");
            }
            catch (XmpException e)
            {
                _log.WriteLine("getQualifier with no schema URI - threw XmpException #" + e.ErrorCode + " :   " + e.Message);
            }

            property = meta.GetQualifier(TestData.NS1, "QualProp3", XmpConstants.NsXml, "xml:lang");
            _log.WriteLine("getQualifier ns1:QualProp3/@xml-lang =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            property = meta.GetQualifier(TestData.NS1, "QualProp3", TestData.NS2, "ns2:Qual");
            _log.WriteLine("getQualifier ns1:QualProp3/?ns2:Qual =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");


            WriteMinorLabel("Get non-simple properties");

            property = meta.GetProperty(TestData.NS1, "Bag");
            _log.WriteLine("getProperty ns1:Bag =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            property = meta.GetProperty(TestData.NS1, "Seq");
            _log.WriteLine("getProperty ns1:Seq =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            property = meta.GetProperty(TestData.NS1, "Alt");
            _log.WriteLine("getProperty ns1:Alt =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            property = meta.GetProperty(TestData.NS1, "Struct");
            _log.WriteLine("getProperty ns1:Struct =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");


            WriteMinorLabel("Get not existing properties");

            try
            {
                meta.GetProperty("ns:bogus/", "Bogus");
            }
            catch (XmpException e)
            {
                _log.WriteLine("getProperty with bogus schema URI - threw XmpException #" + e.ErrorCode + " :   " + e.Message);
            }

            property = meta.GetProperty(TestData.NS1, "Bogus");
            _log.WriteLine("getProperty ns1:Bogus (not existing) =   " + property);

            property = meta.GetArrayItem(TestData.NS1, "Bag", 99);
            _log.WriteLine("ArrayItem ns1:Bag[99] (not existing) =   " + property);

            property = meta.GetStructField(TestData.NS1, "Struct", TestData.NS2, "Bogus");
            _log.WriteLine("getStructField ns1:Struct/ns2:Bogus (not existing) =   " + property);

            property = meta.GetQualifier(TestData.NS1, "Prop", TestData.NS2, "Bogus");
            _log.WriteLine("getQualifier ns1:Prop/?ns2:Bogus (not existing) =   " + property);
        }

        /**
         * Test doesPropertyExist, deleteProperty, and related methods.
         * @param meta a predefined <code>XmpMeta</code> object.
         */
        private void CoverExistingProperties(IXmpMeta meta)
        {
            WriteMajorLabel("Test doesPropertyExist, deleteProperty, and related methods");

            PrintXmpMeta(meta, "XMP object");

            _log.WriteLine("doesPropertyExist ns1:Prop =    " + meta.DoesPropertyExist(TestData.NS1, "Prop"));
            _log.WriteLine("doesPropertyExist ns1:Struct =    " + meta.DoesPropertyExist(TestData.NS1, "ns1:Struct"));
            _log.WriteLine("doesArrayItemExist ns1:Bag[2] =    " + meta.DoesArrayItemExist(TestData.NS1, "Bag", 2));
            _log.WriteLine("doesArrayItemExist ns1:Seq[last()] =    " + meta.DoesArrayItemExist(TestData.NS1, "ns1:Seq", XmpConstants.ArrayLastItem));
            _log.WriteLine("doesStructFieldExist ns1:Struct/ns2:Field1 =    " + meta.DoesStructFieldExist(TestData.NS1, "Struct", TestData.NS2, "Field1"));
            _log.WriteLine("doesQualifierExist ns1:QualProp1/?ns2:Qual1 =    " + meta.DoesQualifierExist(TestData.NS1, "QualProp1", TestData.NS2, "Qual1"));
            _log.WriteLine("doesQualifierExist ns1:QualProp2/?xml:lang =    " + meta.DoesQualifierExist(TestData.NS1, "QualProp2", XmpConstants.NsXml, "lang"));

            _log.WriteLine("doesPropertyExist (namespace is null) =    " + meta.DoesPropertyExist(null, "ns1:Bag"));
            _log.WriteLine("doesArrayItemExist (namespace is null) =    " + meta.DoesArrayItemExist(null, "ns1:Bag", XmpConstants.ArrayLastItem));
            _log.WriteLine("doesQualifierExist ns:Bogus (namespace not existing) =    " + meta.DoesPropertyExist("ns:bogus/", "Bogus"));
            _log.WriteLine("doesPropertyExist ns1:Bogus =    " + meta.DoesPropertyExist(TestData.NS1, "Bogus"));
            _log.WriteLine("doesArrayItemExist ns1:Bag[99] =    " + meta.DoesArrayItemExist(TestData.NS1, "Bag", 99));
            _log.WriteLine("doesStructFieldExist ns1:Struct/ns2:Bogus =    " + meta.DoesStructFieldExist(TestData.NS1, "Struct", TestData.NS2, "Bogus"));
            _log.WriteLine("doesQualifierExist ns1:Prop/?ns2:Bogus =    " + meta.DoesQualifierExist(TestData.NS1, "Prop", TestData.NS2, "Bogus"));
        }

        /**
         * Tests deletion of properties, array items, struct fields and qualifer.
         * @param meta a predefined <code>XmpMeta</code> object.
         */
        private void CoverDeleteProperties(IXmpMeta meta)
        {
            WriteMajorLabel("Test deleteProperty");

            meta.DeleteProperty(TestData.NS1, "Prop");
            meta.DeleteArrayItem(TestData.NS1, "Bag", 2);
            meta.DeleteStructField(TestData.NS1, "Struct", TestData.NS2, "Field1");

            PrintXmpMeta(meta, "Delete Prop, Bag[2], and Struct1/Field1");

            meta.DeleteQualifier(TestData.NS1, "QualProp1", TestData.NS2, "Qual1");
            meta.DeleteQualifier(TestData.NS1, "QualProp2", XmpConstants.NsXml, "lang");
            meta.DeleteQualifier(TestData.NS1, "QualProp3", TestData.NS2, "Qual");
            meta.DeleteQualifier(TestData.NS1, "QualProp4", XmpConstants.NsXml, "lang");

            PrintXmpMeta(meta,
                "Delete QualProp1/?ns2:Qual1, QualProp2/?xml:lang, " +
                "QualProp3:/ns2:Qual, and QualProp4/?xml:lang");

            meta.DeleteProperty(TestData.NS1, "Bag");
            meta.DeleteProperty(TestData.NS1, "Struct");

            PrintXmpMeta(meta, "Delete all of Bag and Struct");
        }

        /**
         * Localized text set/get methods.
         * @throws XmpException Forwards exceptions
         */
        private void CoverLocalisedProperties()
        {
            WriteMajorLabel("Test setLocalizedText and getLocalizedText");

            var meta = XmpMetaFactory.Create();
            meta.SetLocalizedText(TestData.NS1, "AltText", "", "x-default", "default value");
            PrintXmpMeta(meta, "Set x-default value");

            meta.SetLocalizedText(TestData.NS1, "AltText", "en", "en-us", "en-us value");
            PrintXmpMeta(meta, "Set en/en-us value");

            meta.SetLocalizedText(TestData.NS1, "AltText", "en", "en-uk", "en-uk value");
            PrintXmpMeta(meta, "Set en/en-uk value");

            var property = meta.GetLocalizedText(TestData.NS1, "AltText", "en", "en-ca");
            _log.WriteLine("getLocalizedText en/en-ca =   " + property.Value + " (lang: " + property.Language + ", opt: " + property.Options.GetOptionsString() + ")");

            property = meta.GetProperty(TestData.NS1, "AltText");
            _log.WriteLine("getProperty ns1:AltText =   " + property.Value + " (lang: " + property.Language + ", opt: " + property.Options.GetOptionsString() + ")");
        }

        /**
         * Literal value set/get methods
         * @throws XmpException
         */
        private void CoverLiteralProperties()
        {
            WriteMajorLabel("Test SetProperty... and getProperty... methods (set/get with literal values)");

            var meta = XmpMetaFactory.ParseFromString(TestData.DATETIME_RDF);
            var dateValue = XmpDateTimeFactory.Create(2000, 1, 2, 3, 4, 5, 0);

            meta.SetPropertyBoolean(TestData.NS1, "Bool0", false);
            meta.SetPropertyBoolean(TestData.NS1, "Bool1", true);
            meta.SetPropertyInteger(TestData.NS1, "Int", 42);
            meta.SetPropertyDouble(TestData.NS1, "Double", 4.2);

            meta.SetPropertyDate(TestData.NS1, "Date10", dateValue);
/*
            TODO reinstate this code

            int offset = (/* hour #1# 06 * 3600 * 1000 + /* minute #1# 07 * 60 * 1000) * /* sign #1# 1;
            dateValue.SetTimeZone(new SimpleTimeZone(offset, "test"));
            meta.SetPropertyDate (NS1, "Date11", dateValue);
            offset *= -1;
            dateValue.SetTimeZone(new SimpleTimeZone(offset, "test"));
            meta.SetPropertyDate (NS1, "Date12", dateValue);
            dateValue.SetNanosecond(9);
            meta.SetPropertyDate (NS1, "Date13", dateValue);
*/

            PrintXmpMeta(meta, "A few basic binary Set... calls");

            var b = meta.GetPropertyBoolean(TestData.NS1, "Bool0");
            _log.WriteLine("getPropertyBoolean ns1:Bool0 =   " + b);

            b = meta.GetPropertyBoolean(TestData.NS1, "Bool1");
            _log.WriteLine("getPropertyBoolean ns1:Bool1 =   " + b);

            var integer = meta.GetPropertyInteger(TestData.NS1, "Int");
            _log.WriteLine("getPropertyBoolean ns1:Int =   " + integer);

            var d = meta.GetPropertyDouble(TestData.NS1, "Double");
            _log.WriteLine("getPropertyBoolean ns1:Int =   " + d);

            for (var i = 1; i <= 13; i++)
            {
                var dateName = "Date" + i;
                var dt = meta.GetPropertyDate(TestData.NS1, dateName);
                _log.WriteLine("getPropertyDate (" + i + ") =   " + dt);
                meta.SetPropertyDate(TestData.NS2, dateName, dateValue);
            }

            PrintXmpMeta(meta, "Get and re-set the dates in NS2");
        }

        /**
         * Parse and serialize methods.
         * @throws XmpException Forwards exceptions
         */
        private void CoverParsing()
        {
            WriteMajorLabel("Test parsing with multiple buffers and various options");

            var meta = XmpMetaFactory.ParseFromString(TestData.SIMPLE_RDF);
            PrintXmpMeta(meta, "Parse from String");

            meta = XmpMetaFactory.ParseFromString(TestData.SIMPLE_RDF, new ParseOptions {RequireXmpMeta = true});
            PrintXmpMeta(meta, "Parse and require xmpmeta element, which is missing");

            meta = XmpMetaFactory.ParseFromString(TestData.NAMESPACE_RDF);
            PrintXmpMeta(meta, "Parse RDF with multiple nested namespaces");

            meta = XmpMetaFactory.ParseFromString(TestData.XMPMETA_RDF, new ParseOptions {RequireXmpMeta = true});
            PrintXmpMeta(meta, "Parse and require xmpmeta element, which is present");

            meta = XmpMetaFactory.ParseFromString(TestData.INCONSISTENT_RDF);
            PrintXmpMeta(meta, "Parse and reconcile inconsistent aliases");

            try
            {
                XmpMetaFactory.ParseFromString(TestData.INCONSISTENT_RDF, new ParseOptions {StrictAliasing = true});
            }
            catch (XmpException e)
            {
                _log.WriteLine("Parse and do not reconcile inconsistent aliases - threw XmpException #{0} :   {1}", e.ErrorCode, e.Message);
            }
        }

        /**
         * Test CR and LF in values.
         * @throws XmpException Forwards exceptions
         */
        private void CoverLinefeedValues()
        {
            WriteMajorLabel("Test CR and LF in values");

            const string valueWithCR = "ASCII \r CR";
            const string valueWithLF = "ASCII \n LF";
            const string valueWithCRLF = "ASCII \r\n CRLF";

            var meta = XmpMetaFactory.ParseFromString(TestData.NEWLINE_RDF);

            meta.SetProperty(TestData.NS2, "HasCR", valueWithCR);
            meta.SetProperty(TestData.NS2, "HasLF", valueWithLF);
            meta.SetProperty(TestData.NS2, "HasCRLF", valueWithCRLF);

            _log.WriteLine(XmpMetaFactory.SerializeToString(meta, new SerializeOptions {OmitPacketWrapper = true}));

            var hasCR = meta.GetPropertyString(TestData.NS1, "HasCR");
            var hasCR2 = meta.GetPropertyString(TestData.NS2, "HasCR");
            var hasLF = meta.GetPropertyString(TestData.NS1, "HasLF");
            var hasLF2 = meta.GetPropertyString(TestData.NS2, "HasLF");
            var hasCRLF = meta.GetPropertyString(TestData.NS1, "HasCRLF");
            var hasCRLF2 = meta.GetPropertyString(TestData.NS2, "HasCRLF");

            if (hasCR == valueWithCR && hasCR2 == valueWithCR &&
                hasLF == valueWithLF && hasLF2 == valueWithLF &&
                hasCRLF == valueWithCRLF && hasCRLF2 == valueWithCRLF)
            {
                _log.WriteLine("\n## HasCR and HasLF and HasCRLF correctly retrieved\n");
            }
        }

        /**
         * Covers the serialization of an <code>XmpMeta</code> object with different options.
         * @throws Exception Forwards exceptions
         */
        private void CoverSerialization()
        {
            WriteMajorLabel("Test serialization with various options");

            var meta = XmpMetaFactory.ParseFromString(TestData.SIMPLE_RDF);
            meta.SetProperty(TestData.NS2, "Another", "Something in another schema");
            meta.SetProperty(TestData.NS2, "Yet/pdf:More", "Yet more in another schema");

            PrintXmpMeta(meta, "Parse simple RDF, serialize with various options");

            WriteMinorLabel("Default serialize");
            _log.WriteLine(XmpMetaFactory.SerializeToString(meta, null));

            WriteMinorLabel("Compact RDF, no packet serialize");
            _log.WriteLine(XmpMetaFactory.SerializeToString(meta, new SerializeOptions {UseCompactFormat = true, OmitPacketWrapper = true}));

            WriteMinorLabel("Read-only serialize");
            _log.WriteLine(XmpMetaFactory.SerializeToString(meta, new SerializeOptions {ReadOnlyPacket = true}));

            WriteMinorLabel("Alternate newline serialize");
            _log.WriteLine(XmpMetaFactory.SerializeToString(meta, new SerializeOptions {Newline = "<--newline-->\n", OmitPacketWrapper = true}));

            WriteMinorLabel("Alternate indent serialize");
            _log.WriteLine(XmpMetaFactory.SerializeToString(meta, new SerializeOptions {Indent = "-->", BaseIndent = 5, OmitPacketWrapper = true}));

            WriteMinorLabel("Small padding serialize");
            _log.WriteLine(XmpMetaFactory.SerializeToString(meta, new SerializeOptions {Padding = 10}));

            WriteMinorLabel("Serialize with exact packet size");
            var s = XmpMetaFactory.SerializeToBuffer(meta, new SerializeOptions {ReadOnlyPacket = true}).Length;
            _log.WriteLine("Minimum packet size is " + s + " bytes\n");

            // with the flag "exact packet size" the padding becomes the overall length of the packet
            var buffer = XmpMetaFactory.SerializeToBuffer(meta, new SerializeOptions {ExactPacketLength = true, Padding = s});
            _log.WriteLine(Encoding.UTF8.GetString(buffer, 0, buffer.Length));

            try
            {
                XmpMetaFactory.ParseFromString(XmpMetaFactory.SerializeToString(meta, new SerializeOptions {ExactPacketLength = true, Padding = s - 1}));
            }
            catch (XmpException e)
            {
                _log.WriteLine("\nExact packet size smaller than minimal packet length - threw XmpException #{0} :   {1}", e.ErrorCode, e.Message);
            }
        }

        /**
         * Cover different use cases of the <code>XmpIterator</code>.
         * @throws XmpException Forwards exceptions
         */
        private void CoverIterator()
        {
            WriteMajorLabel("Test iteration methods");

            var meta = XmpMetaFactory.ParseFromString(TestData.RDF_COVERAGE);
            meta.SetProperty(TestData.NS2, "Prop", "Prop value");
            meta.AppendArrayItem(TestData.NS2, "Bag", new PropertyOptions {IsArray = true}, "BagItem 2", null);
            meta.AppendArrayItem(TestData.NS2, "Bag", "BagItem 1");
            meta.AppendArrayItem(TestData.NS2, "Bag", "BagItem 3");

            PrintXmpMeta(meta, "Parse \"coverage\" RDF, add Bag items out of order");
/*
            TODO reinstate this code

            writeMinorLabel ("Default iteration");
            for (XmpIterator it = meta.Iterator(); it.hasNext();)
            {
                XmpPropertyInfo prop = (XmpPropertyInfo) it.next();
                printPropertyInfo(prop);
            }

            writeMinorLabel ("Iterate omitting qualifiers");
            for (XmpIterator it = meta.iterator(new IteratorOptions().setOmitQualifiers(true)); it.hasNext();)
            {
                XmpPropertyInfo prop = (XmpPropertyInfo) it.next();
                printPropertyInfo(prop);
            }

            writeMinorLabel("Iterate with just leaf names");
            for (XmpIterator it = meta.iterator(new IteratorOptions().setJustLeafname(true)); it.hasNext();)
            {
                XmpPropertyInfo prop = (XmpPropertyInfo) it.next();
                printPropertyInfo(prop);
            }

            writeMinorLabel("Iterate with just leaf nodes");
            for (XmpIterator it = meta.iterator(new IteratorOptions().setJustLeafnodes(true)); it.hasNext();)
            {
                XmpPropertyInfo prop = (XmpPropertyInfo) it.next();
                printPropertyInfo(prop);
            }

            writeMinorLabel("Iterate just the schema nodes");
            for (XmpIterator it = meta.iterator(new IteratorOptions().setJustChildren(true)); it.hasNext();)
            {
                XmpPropertyInfo prop = (XmpPropertyInfo) it.next();
                printPropertyInfo(prop);
            }

            writeMinorLabel("Iterate the ns2: namespace");
            for (XmpIterator it = meta.iterator(NS2, null, null); it.hasNext();)
            {
                XmpPropertyInfo prop = (XmpPropertyInfo) it.next();
                printPropertyInfo(prop);
            }

            writeMinorLabel("Start at ns2:Bag");
            for (XmpIterator it = meta.iterator(NS2, "Bag", null); it.hasNext();)
            {
                XmpPropertyInfo prop = (XmpPropertyInfo) it.next();
                printPropertyInfo(prop);
            }

            writeMinorLabel("Start at ns2:NestedStructProp/ns1:Outer");
            for (XmpIterator it = meta.iterator(NS2, "NestedStructProp/ns1:Outer", null); it.hasNext();)
            {
                XmpPropertyInfo prop = (XmpPropertyInfo) it.next();
                printPropertyInfo(prop);
            }

            writeMinorLabel("Iterate an empty namespace");
            for (XmpIterator it = meta.iterator("ns:Empty", null, null); it.hasNext();)
            {
                XmpPropertyInfo prop = (XmpPropertyInfo) it.next();
                printPropertyInfo(prop);
            }

            writeMinorLabel("Iterate the top of the ns2: namespace with just leaf names");
            for (XmpIterator it = meta.iterator(NS2, null, new IteratorOptions().setJustChildren(true).setJustLeafname(true)); it.hasNext();)
            {
                XmpPropertyInfo prop = (XmpPropertyInfo) it.next();
                printPropertyInfo(prop);
            }

            writeMinorLabel("Iterate the top of the ns2: namespace with just leaf nodes");
            for (XmpIterator it = meta.iterator(NS2, null, new IteratorOptions().setJustChildren(true).setJustLeafnodes(true)); it.hasNext();)
            {
                XmpPropertyInfo prop = (XmpPropertyInfo) it.next();
                printPropertyInfo(prop);
            }
*/
        }

        /**
         * XPath composition utilities using the <code>XmpPathFactory</code>.
         * @throws XmpException Forwards exceptions
         */
        private void CoverPathCreation()
        {
            WriteMajorLabel("XPath composition utilities");

            var meta = XmpMetaFactory.Create();

            meta.AppendArrayItem(TestData.NS1, "ArrayProp", new PropertyOptions {IsArray = true}, "Item 1", null);

            var path = XmpPathFactory.ComposeArrayItemPath("ArrayProp", 2);
            _log.WriteLine("composeArrayItemPath ArrayProp[2] =   " + path);
            meta.SetProperty(TestData.NS1, path, "new ns1:ArrayProp[2] value");

            path = "StructProperty";
            path += XmpPathFactory.ComposeStructFieldPath(TestData.NS2, "Field3");
            _log.WriteLine("composeStructFieldPath StructProperty/ns2:Field3 =   " + path);
            meta.SetProperty(TestData.NS1, path, "new ns1:StructProp/ns2:Field3 value");

            path = "QualProp";
            path += XmpPathFactory.ComposeQualifierPath(TestData.NS2, "Qual");
            _log.WriteLine("composeStructFieldPath QualProp/?ns2:Qual =   " + path);
            meta.SetProperty(TestData.NS1, path, "new ns1:QualProp/?ns2:Qual value");

            meta.SetLocalizedText(TestData.NS1, "AltTextProp", null, "en-US", "initival value");
            path = "AltTextProp";
            path += XmpPathFactory.ComposeQualifierPath(XmpConstants.NsXml, "lang");
            _log.WriteLine("composeQualifierPath ns1:AltTextProp/?xml:lang =   " + path);
            meta.SetProperty(TestData.NS1, path, "new ns1:AltTextProp/?xml:lang value");

            PrintXmpMeta(meta, "Modified simple RDF");
        }

        /**
         * Date/Time utilities
         */
        private void CoverDateTime()
        {
            WriteMajorLabel("Test date/time utilities and special values");

#if !PORTABLE
            var date1 = XmpDateTimeFactory.Create(2000, 1, 31, 12, 34, 56, -1);
            date1.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

            var date2 = XmpDateTimeFactory.Create(0, 0, 0, 0, 0, 0, 0);

            var cal = new GregorianCalendar(2007, 1, 28);
            var date3 = XmpDateTimeFactory.CreateFromCalendar(cal);

            var currentDateTime = XmpDateTimeFactory.GetCurrentDateTime();

            _log.WriteLine("Print date 2000 Jan 31 12:34:56 PST =   {0}", date1);
            _log.WriteLine("Print zero date =   {0}", date2);
            _log.WriteLine("Print date created by a calendar =   {0}", date3);
            _log.WriteLine("Print current date =   {0}", currentDateTime);
#endif
        }

        #region Utilities

        /**
         * Print the content of an XmpMeta object a headline and its name.
         * @param meta an <code>XmpMeta</code> object
         * @param title the headline
         */
        private void PrintXmpMeta(IXmpMeta meta, string title)
        {
            var name = meta.GetObjectName();
            if (!string.IsNullOrEmpty(name))
            {
                _log.WriteLine("{0} (Name: '{1}'):", title, name);
            }
            else
            {
                _log.WriteLine("{0}:", title);
            }
            _log.WriteLine(meta.DumpObject());
        }

        /**
         * @param prop an <code>XmpPropertyInfo</code> from the <code>XmpIterator</code>.
         */
        private void PrintPropertyInfo(IXmpPropertyInfo prop)
        {
            _log.WriteLine("NS ({0})   PATH ({1})   VALUE ({2})  OPTIONS ({3})",
                prop.Namespace, prop.Path, prop.Value, prop.Options.GetOptionsString());
        }

        /**
         * Dump the alias list to the output.
         */
        private void DumpAliases()
        {
            var aliases = _registry.Aliases;
            foreach (var qname in aliases.Keys)
                _log.WriteLine("{0}   --->   {1}", qname, aliases[qname]);
        }

        /**
         * Writes a major headline to the output.
         * @param title the headline
         */
        private void WriteMajorLabel(string title)
        {
            _log.WriteLine("// =============================================================================");
            _log.WriteLine("// {0}", title);
            _log.WriteLine("// =============================================================================");
        }

        /**
         * Writes a minor headline to the output.
         * @param title the headline
         */
        private void WriteMinorLabel(string title)
        {
            _log.WriteLine("// -----------------------------------------------------------------------------".Substring(0, title.Length + 3));
            _log.WriteLine("// {0}", title);
        }

        #endregion
    }
}
