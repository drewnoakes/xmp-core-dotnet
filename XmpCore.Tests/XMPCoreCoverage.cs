// =================================================================================================
// ADOBE SYSTEMS INCORPORATED
// Copyright 2006 Adobe Systems Incorporated
// All Rights Reserved
//
// NOTICE:  Adobe permits you to use, modify, and distribute this file in accordance with the terms
// of the Adobe license agreement accompanying it.
// =================================================================================================


using System;
using System.IO;
using System.Text;
using Sharpen;
using XmpCore.Impl;
using XmpCore.Options;

namespace XmpCore.Tests
{
    public static class XmpCoreCoverage
    {
        private static TextWriter log;
        private static readonly IXmpSchemaRegistry registry = XmpMetaFactory.SchemaRegistry;

        public static void Main()
        {
            try
            {
                log = Console.Out;

                log.WriteLine("XmpCoreCoverage starting   " + DateTime.Now);
                log.WriteLine("XmpCore Version: " + XmpMetaFactory.VersionInfo);
                log.WriteLine();


                DoCoreCoverage();


                log.WriteLine(); log.WriteLine();
                log.WriteLine("XmpCoreCoverage ending   " + DateTime.Now);
            }
            catch (XmpException e)
            {
                log.WriteLine("Caught XmpException " + e.GetErrorCode() + " :   " +e.Message);
            }
            catch (Exception e)
            {
                log.WriteLine("Caught exception '" + e.Message  + "'");
            }

            if (log != null)
                log.Flush();
        }

        private static void DoCoreCoverage()
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
        private static void CoverNamespaceRegistry()
        {
            writeMajorLabel ("Test of namespace registry");

            // Lists of predefined namespaces
            //TODO reinstate this code

            writeMinorLabel ("List predefined namespaces");
            var namespaces = registry.Namespaces;
            foreach (var pair in namespaces)
                log.WriteLine(pair.Key + "   --->   " + pair.Value);

            log.WriteLine();

            // Registry namespace functions
            writeMinorLabel ("Test namespace registry functions");

            string prefix = registry.RegisterNamespace(TestData.NS1, "ns1");
            log.WriteLine ("registerNamespace ns1:   {0}   --->   {1}", prefix, registry.GetNamespaceUri(prefix));

            prefix = registry.RegisterNamespace(TestData.NS2, "ns2");
            log.WriteLine ("registerNamespace ns2:   {0}   --->   {1}", prefix, registry.GetNamespaceUri(prefix));

            prefix = registry.GetNamespacePrefix(TestData.NS1);
            log.WriteLine ("getNamespacePrefix ns1:   {0}", prefix);

            log.WriteLine("getNamespaceURI ns1:   {0}", registry.GetNamespaceUri("ns1"));

            prefix = registry.GetNamespacePrefix("bogus");
            log.WriteLine ("getNamespacePrefix bogus:   {0}", prefix);

            log.WriteLine("getNamespaceURI ns1:   {0}", registry.GetNamespaceUri("bogus"));
        }


        /**
         * List predefined aliases, register new aliases and resolve aliases.
         * @throws XmpException Forward exceptions
         */
        private static void CoverAliasRegistry()
        {
            writeMajorLabel ("Test alias registry and functions");
            dumpAliases();

            // Register new aliases
            writeMinorLabel ("Add ns2: to ns1: aliases");

            dumpAliases();


            // Resolve aliases
            writeMinorLabel ("Resolve ns2: to ns1: aliases");

            var aliasInfo = registry.ResolveAlias(TestData.NS1, "SimpleActual");
            log.WriteLine ("ResolveAlias ns1:SimpleActual:   " + aliasInfo + "   (wrong way!)");

            aliasInfo = registry.ResolveAlias(TestData.NS2, "SimpleAlias");
            log.WriteLine ("ResolveAlias ns2:SimpleAlias:   " + aliasInfo);
            log.WriteLine();


            aliasInfo = registry.ResolveAlias (TestData.NS2, "BagAlias");
            log.WriteLine ("ResolveAlias ns2:BagAlias:   " + aliasInfo);

            aliasInfo = registry.ResolveAlias (TestData.NS2, "SeqAlias");
            log.WriteLine ("ResolveAlias ns2:SeqAlias:   " + aliasInfo);

            aliasInfo = registry.ResolveAlias (TestData.NS2, "AltAlias");
            log.WriteLine ("ResolveAlias ns2:AltAlias:   " + aliasInfo);

            aliasInfo = registry.ResolveAlias (TestData.NS2, "AltTextAlias");
            log.WriteLine ("ResolveAlias ns2:AltTextAlias:   " + aliasInfo);
            log.WriteLine();


            aliasInfo = registry.ResolveAlias (TestData.NS2, "BagItemAlias");
            log.WriteLine ("ResolveAlias ns2:BagItemAlias:   " + aliasInfo);

            aliasInfo = registry.ResolveAlias (TestData.NS2, "SeqItemAlias");
            log.WriteLine ("ResolveAlias ns2:SeqItemAlias:   " + aliasInfo);

            aliasInfo = registry.ResolveAlias (TestData.NS2, "AltItemAlias");
            log.WriteLine ("ResolveAlias ns2:AltItemAlias:   " + aliasInfo);

            aliasInfo = registry.ResolveAlias (TestData.NS2, "AltTextItemAlias");
            log.WriteLine ("ResolveAlias ns2:AltTextItemAlias:   " + aliasInfo);
            log.WriteLine();


            // set alias properties
            writeMinorLabel ("Test SetProperty through ns2: simple aliases");

            var meta = XmpMetaFactory.Create();
            meta.SetProperty (TestData.NS2, "SimpleAlias", "Simple value");
            meta.SetProperty (TestData.NS2, "ns2:BagItemAlias", "BagItem value");
            meta.SetProperty (TestData.NS2, "SeqItemAlias", "SeqItem value");
            meta.SetProperty (TestData.NS2, "AltItemAlias", "AltItem value");
            meta.SetProperty (TestData.NS2, "AltTextItemAlias", "AltTextItem value");
            printXmpMeta(meta, "Check for aliases and bases");


            // delete aliases
            writeMinorLabel ("Delete some ns2: to ns1: aliases");

            dumpAliases();
        }


        /**
         * Test simple constructors and parsing, setting the instance ID
         * @throws XmpException Forwards exceptions
         */
        private static void CoverCreatingXmp()
        {
            writeMajorLabel ("Test simple constructors and parsing, setting the instance ID");

            var meta1 = XmpMetaFactory.Create();
            printXmpMeta(meta1, "Empty XMP object");

            var meta2 = XmpMetaFactory.Create();
            meta2.SetObjectName("New object name");
            printXmpMeta(meta2, "XMP object with name");

            var meta3 = XmpMetaFactory.ParseFromString(TestData.RDF_COVERAGE);
            printXmpMeta(meta3, "Construct and parse from buffer");

            meta3.SetProperty(XmpConstants.NsXmpMm, "InstanceID", "meta2:Original");
            printXmpMeta(meta3, "Add instance ID");

            XmpMeta meta4 = (XmpMeta) meta3.Clone();
            meta4.SetProperty (XmpConstants.NsXmpMm, "InstanceID", "meta2:Clone");
            printXmpMeta(meta3, "Clone and add instance ID");
        }


        /**
         * Cover some basid set calls (including arrays and structs).
         * @return Returns an <code>XmpMeta</code> object that is reused in the next examples.
         * @throws XmpException Forwards Exceptions
         */
        private static IXmpMeta CoverSetPropertyMethods()
        {
            // Basic set/get methods
            writeMajorLabel ("Test SetProperty and related methods");

            var meta = XmpMetaFactory.Create();
            meta.SetProperty (TestData.NS1, "Prop", "Prop value");
            meta.SetProperty (TestData.NS1, "ns1:XMLProp", "<PropValue/>");
            meta.SetProperty (TestData.NS1, "ns1:URIProp", "URI:value/", new PropertyOptions { IsUri = true });

            meta.AppendArrayItem(TestData.NS1, "Bag", new PropertyOptions { IsArray = true }, "BagItem value", null);
            meta.AppendArrayItem(TestData.NS1, "ns1:Seq", new PropertyOptions { IsArrayOrdered = true }, "SeqItem value", null);
            meta.AppendArrayItem(TestData.NS1, "ns1:Alt", new PropertyOptions { IsArrayAlternate = true }, "AltItem value", null);

            meta.SetArrayItem (TestData.NS1, "Bag", 1, "BagItem 3");
            meta.InsertArrayItem (TestData.NS1, "ns1:Bag", 1, "BagItem 1");
            meta.InsertArrayItem (TestData.NS1, "ns1:Bag", 2, "BagItem 2");
            meta.AppendArrayItem (TestData.NS1, "Bag", "BagItem 4");

            meta.SetStructField (TestData.NS1, "Struct", TestData.NS2, "Field1", "Field1 value");
            meta.SetStructField (TestData.NS1, "ns1:Struct", TestData.NS2, "Field2", "Field2 value");
            meta.SetStructField (TestData.NS1, "ns1:Struct", TestData.NS2, "Field3", "Field3 value");

            printXmpMeta(meta, "A few basic set property calls");

            // -----------------------------------------------------------------------------------------

            // Add some properties with qualifier
            writeMinorLabel ("Add some properties with qualifier");
            log.WriteLine ("CountArrayItems Bag = " + meta.CountArrayItems(TestData.NS1, "Bag"));

            meta.SetProperty (TestData.NS1, "QualProp1", "Prop value");
            meta.SetQualifier (TestData.NS1, "QualProp1", TestData.NS2, "Qual1", "Qual1 value");
            meta.SetProperty (TestData.NS1, "QualProp1/?ns2:Qual3", "Qual3 value");
            meta.SetProperty (TestData.NS1, "QualProp1/?xml:lang", "x-qual");

            meta.SetProperty (TestData.NS1, "QualProp2", "Prop value");
            meta.SetQualifier (TestData.NS1, "QualProp2", XmpConstants.NsXml, "lang", "en-us");
            meta.SetProperty (TestData.NS1, "QualProp2/@xml:lang", "x-attr");

            meta.SetProperty (TestData.NS1, "QualProp3", "Prop value");
            meta.SetQualifier (TestData.NS1, "ns1:QualProp3", XmpConstants.NsXml, "xml:lang", "en-us");
            meta.SetQualifier (TestData.NS1, "ns1:QualProp3", TestData.NS2, "ns2:Qual", "Qual value");

            meta.SetProperty (TestData.NS1, "QualProp4", "Prop value");
            meta.SetQualifier (TestData.NS1, "QualProp4", TestData.NS2, "Qual", "Qual value");
            meta.SetQualifier (TestData.NS1, "QualProp4", XmpConstants.NsXml, "lang", "en-us");
            printXmpMeta(meta, "Add some qualifiers");

            meta.SetProperty (TestData.NS1, "QualProp1", "new value");
            meta.SetProperty (TestData.NS1, "QualProp2", "new value");
            meta.SetProperty (TestData.NS1, "QualProp3", "new value");
            meta.SetProperty (TestData.NS1, "QualProp4", "new value");
            printXmpMeta (meta, "Change values and keep qualifiers");

            return meta;
        }


        /**
         * Test getProperty, deleteProperty and related methods.
         * @param meta a predefined <code>XmpMeta</code> object.
         * @throws XmpException Forwards exceptions
         */
        private static void CoverGetPropertyMethods(IXmpMeta meta)
        {
            writeMajorLabel ("Test getProperty, deleteProperty and related methods");

            meta.DeleteProperty (TestData.NS1, "QualProp1");    // ! Start with fresh qualifiers.
            meta.DeleteProperty (TestData.NS1, "ns1:QualProp2");
            meta.DeleteProperty (TestData.NS1, "ns1:QualProp3");
            meta.DeleteProperty (TestData.NS1, "QualProp4");


            writeMinorLabel("Set properties with qualifier");

            meta.SetProperty (TestData.NS1, "QualProp1", "Prop value");
            meta.SetQualifier (TestData.NS1, "QualProp1", TestData.NS2, "Qual1", "Qual1 value");

            meta.SetProperty (TestData.NS1, "QualProp2", "Prop value");
            meta.SetQualifier (TestData.NS1, "QualProp2", XmpConstants.NsXml, "lang", "en-us");

            meta.SetProperty (TestData.NS1, "QualProp3", "Prop value");
            meta.SetQualifier (TestData.NS1, "QualProp3", XmpConstants.NsXml, "lang", "en-us");
            meta.SetQualifier (TestData.NS1, "QualProp3", TestData.NS2, "Qual", "Qual value");

            meta.SetProperty (TestData.NS1, "QualProp4", "Prop value");
            meta.SetQualifier (TestData.NS1, "QualProp4", TestData.NS2, "Qual", "Qual value");
            meta.SetQualifier (TestData.NS1, "QualProp4", XmpConstants.NsXml, "lang", "en-us");

            printXmpMeta (meta, "XMP object");


            writeMinorLabel("Get simple properties");

            var property = meta.GetProperty(TestData.NS1, "Prop");
            log.WriteLine("getProperty ns1:Prop =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            property = meta.GetProperty(TestData.NS1, "ns1:XMLProp");
            log.WriteLine("getProperty ns1:XMLProp =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            property = meta.GetProperty(TestData.NS1, "ns1:URIProp");
            log.WriteLine("getProperty ns1:URIProp =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            property = meta.GetArrayItem(TestData.NS1, "Bag", 2);
            log.WriteLine("getArrayItem ns1:Bag[2] =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            try
            {
                meta.GetArrayItem(null, "ns1:Bag", 1);
            }
            catch (XmpException e)
            {
                log.WriteLine("getArrayItem with no schema URI - threw XmpException #" + e.GetErrorCode() +" :   " + e.Message + ")");
            }


            writeMinorLabel("Get array items and struct fields");

            property = meta.GetArrayItem(TestData.NS1, "ns1:Seq", 1);
            log.WriteLine("getArrayItem ns1:Seq[1] =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            property = meta.GetArrayItem(TestData.NS1, "ns1:Alt", 1);
            log.WriteLine("getArrayItem ns1:Alt[1] =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");
            log.WriteLine();

            property = meta.GetStructField(TestData.NS1, "Struct", TestData.NS2, "Field1");
            log.WriteLine("getStructField ns1:Struct/ns2:Field1 =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            property = meta.GetStructField(TestData.NS1, "ns1:Struct", TestData.NS2, "Field2");
            log.WriteLine("getStructField ns1:Struct/ns2:Field2 =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            property = meta.GetStructField(TestData.NS1, "ns1:Struct", TestData.NS2, "ns2:Field3");
            log.WriteLine("getStructField ns1:Struct/ns2:Field3 =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            property = meta.GetStructField(TestData.NS1, "ns1:Struct", TestData.NS2, "ns2:Field3");
            log.WriteLine("getStructField ns1:Struct/ns2:Field3 =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            property = meta.GetStructField(TestData.NS1, "ns1:Struct", TestData.NS2, "ns2:Field3");
            log.WriteLine("getStructField ns1:Struct/ns2:Field3 =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");
            log.WriteLine();


            writeMinorLabel("Get qualifier");

            property = meta.GetQualifier(TestData.NS1, "QualProp1", TestData.NS2, "Qual1");
            log.WriteLine("getQualifier  ns1:QualProp1/?ns2:Qual1 =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            try
            {
                meta.GetQualifier(null, "ns1:QualProp1", TestData.NS2, "Qual1");
            }
            catch (XmpException e)
            {
                log.WriteLine("getQualifier with no schema URI - threw XmpException #" + e.GetErrorCode() + " :   " + e.Message);
            }

            property = meta.GetQualifier(TestData.NS1, "QualProp3", XmpConstants.NsXml, "xml:lang");
            log.WriteLine("getQualifier ns1:QualProp3/@xml-lang =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");

            property = meta.GetQualifier(TestData.NS1, "QualProp3", TestData.NS2, "ns2:Qual");
            log.WriteLine("getQualifier ns1:QualProp3/?ns2:Qual =   " + property.Value + " (" + property.Options.GetOptionsString() + ")");
            log.WriteLine();


            writeMinorLabel("Get non-simple properties");

            property = meta.GetProperty(TestData.NS1, "Bag");
            log.WriteLine("getProperty ns1:Bag =   " + property.Value + " ("
                    + property.Options.GetOptionsString() + ")");

            property = meta.GetProperty(TestData.NS1, "Seq");
            log.WriteLine("getProperty ns1:Seq =   " + property.Value + " ("
                    + property.Options.GetOptionsString() + ")");

            property = meta.GetProperty(TestData.NS1, "Alt");
            log.WriteLine("getProperty ns1:Alt =   " + property.Value + " ("
                    + property.Options.GetOptionsString() + ")");

            property = meta.GetProperty(TestData.NS1, "Struct");
            log.WriteLine("getProperty ns1:Struct =   " + property.Value + " ("
                    + property.Options.GetOptionsString() + ")");
            log.WriteLine();


            writeMinorLabel("Get not existing properties");

            try
            {
                meta.GetProperty("ns:bogus/", "Bogus");
            }
            catch (XmpException e)
            {
                log.WriteLine("getProperty with bogus schema URI - threw XmpException #" + e.GetErrorCode() + " :   " + e.Message);
            }

            property = meta.GetProperty (TestData.NS1, "Bogus");
            log.WriteLine ("getProperty ns1:Bogus (not existing) =   " + property);

            property = meta.GetArrayItem(TestData.NS1, "Bag", 99);
            log.WriteLine ("ArrayItem ns1:Bag[99] (not existing) =   " + property);

            property = meta.GetStructField(TestData.NS1, "Struct", TestData.NS2, "Bogus");
            log.WriteLine ("getStructField ns1:Struct/ns2:Bogus (not existing) =   " + property);

            property = meta.GetQualifier (TestData.NS1, "Prop", TestData.NS2, "Bogus");
            log.WriteLine ("getQualifier ns1:Prop/?ns2:Bogus (not existing) =   " + property);
        }


        /**
         * Test doesPropertyExist, deleteProperty, and related methods.
         * @param meta a predefined <code>XmpMeta</code> object.
         */
        private static void CoverExistingProperties(IXmpMeta meta)
        {
            writeMajorLabel ("Test doesPropertyExist, deleteProperty, and related methods");

            printXmpMeta (meta, "XMP object");

            log.WriteLine("doesPropertyExist ns1:Prop =    " + meta.DoesPropertyExist(TestData.NS1, "Prop"));
            log.WriteLine("doesPropertyExist ns1:Struct =    " + meta.DoesPropertyExist(TestData.NS1, "ns1:Struct"));
            log.WriteLine("doesArrayItemExist ns1:Bag[2] =    " + meta.DoesArrayItemExist(TestData.NS1, "Bag", 2));
            log.WriteLine("doesArrayItemExist ns1:Seq[last()] =    "
                    + meta.DoesArrayItemExist(TestData.NS1, "ns1:Seq", XmpConstants.ArrayLastItem));
            log.WriteLine("doesStructFieldExist ns1:Struct/ns2:Field1 =    "
                    + meta.DoesStructFieldExist(TestData.NS1, "Struct", TestData.NS2, "Field1"));
            log.WriteLine("doesQualifierExist ns1:QualProp1/?ns2:Qual1 =    "
                    + meta.DoesQualifierExist(TestData.NS1, "QualProp1", TestData.NS2, "Qual1"));
            log.WriteLine("doesQualifierExist ns1:QualProp2/?xml:lang =    "
                    + meta.DoesQualifierExist(TestData.NS1, "QualProp2", XmpConstants.NsXml, "lang"));
            log.WriteLine();
            log.WriteLine("doesPropertyExist (namespace is null) =    "
                    + meta.DoesPropertyExist(null, "ns1:Bag"));
            log.WriteLine("doesArrayItemExist (namespace is null) =    "
                    + meta.DoesArrayItemExist(null, "ns1:Bag", XmpConstants.ArrayLastItem));
            log.WriteLine("doesQualifierExist ns:Bogus (namespace not existing) =    "
                    + meta.DoesPropertyExist("ns:bogus/", "Bogus"));
            log.WriteLine("doesPropertyExist ns1:Bogus =    " + meta.DoesPropertyExist(TestData.NS1, "Bogus"));
            log.WriteLine("doesArrayItemExist ns1:Bag[99] =    " + meta.DoesArrayItemExist(TestData.NS1, "Bag", 99));
            log.WriteLine("doesStructFieldExist ns1:Struct/ns2:Bogus =    "
                    + meta.DoesStructFieldExist(TestData.NS1, "Struct", TestData.NS2, "Bogus"));
            log.WriteLine("doesQualifierExist ns1:Prop/?ns2:Bogus =    "
                    + meta.DoesQualifierExist(TestData.NS1, "Prop", TestData.NS2, "Bogus"));
        }


        /**
         * Tests deletion of properties, array items, struct fields and qualifer.
         * @param meta a predefined <code>XmpMeta</code> object.
         */
        private static void CoverDeleteProperties(IXmpMeta meta)
        {
            writeMajorLabel("Test deleteProperty");

            meta.DeleteProperty (TestData.NS1, "Prop");
            meta.DeleteArrayItem (TestData.NS1, "Bag", 2);
            meta.DeleteStructField (TestData.NS1, "Struct", TestData.NS2, "Field1");

            printXmpMeta (meta, "Delete Prop, Bag[2], and Struct1/Field1");

            meta.DeleteQualifier (TestData.NS1, "QualProp1", TestData.NS2, "Qual1");
            meta.DeleteQualifier (TestData.NS1, "QualProp2", XmpConstants.NsXml, "lang");
            meta.DeleteQualifier (TestData.NS1, "QualProp3", TestData.NS2, "Qual");
            meta.DeleteQualifier (TestData.NS1, "QualProp4", XmpConstants.NsXml, "lang");

            printXmpMeta(meta,
                "Delete QualProp1/?ns2:Qual1, QualProp2/?xml:lang, " +
                "QualProp3:/ns2:Qual, and QualProp4/?xml:lang");

            meta.DeleteProperty (TestData.NS1, "Bag");
            meta.DeleteProperty (TestData.NS1, "Struct");

            printXmpMeta (meta, "Delete all of Bag and Struct");
        }


        /**
         * Localized text set/get methods.
         * @throws XmpException Forwards exceptions
         */
        private static void CoverLocalisedProperties()
        {
            writeMajorLabel ("Test setLocalizedText and getLocalizedText");

            var meta = XmpMetaFactory.Create();
            meta.SetLocalizedText (TestData.NS1, "AltText", "", "x-default", "default value");
            printXmpMeta (meta, "Set x-default value");

            meta.SetLocalizedText (TestData.NS1, "AltText", "en", "en-us", "en-us value");
            printXmpMeta (meta, "Set en/en-us value");

            meta.SetLocalizedText (TestData.NS1, "AltText", "en", "en-uk", "en-uk value");
            printXmpMeta (meta, "Set en/en-uk value");
            log.WriteLine();

            var property = meta.GetLocalizedText(TestData.NS1, "AltText", "en", "en-ca");
            log.WriteLine("getLocalizedText en/en-ca =   " + property.Value + " (lang: " + property.Language + ", opt: " + property.Options.GetOptionsString() + ")");

            property = meta.GetProperty(TestData.NS1, "AltText");
            log.WriteLine("getProperty ns1:AltText =   "  + property.Value + " (lang: " + property.Language + ", opt: " + property.Options.GetOptionsString() + ")");
        }


        /**
         * Literal value set/get methods
         * @throws XmpException
         */
        private static void CoverLiteralProperties()
        {
            writeMajorLabel("Test SetProperty... and getProperty... methods " +
                "(set/get with literal values)");

            var meta = XmpMetaFactory.ParseFromString(TestData.DATETIME_RDF);
            var dateValue = XmpDateTimeFactory.Create(2000, 1, 2, 3, 4, 5, 0);

            meta.SetPropertyBoolean (TestData.NS1, "Bool0", false);
            meta.SetPropertyBoolean (TestData.NS1, "Bool1", true);
            meta.SetPropertyInteger (TestData.NS1, "Int", 42);
            meta.SetPropertyDouble (TestData.NS1, "Double", 4.2);

            meta.SetPropertyDate (TestData.NS1, "Date10", dateValue);
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

            printXmpMeta (meta, "A few basic binary Set... calls");
            log.WriteLine();

            bool b = meta.GetPropertyBoolean(TestData.NS1, "Bool0");
            log.WriteLine ("getPropertyBoolean ns1:Bool0 =   " + b);

            b = meta.GetPropertyBoolean(TestData.NS1, "Bool1");
            log.WriteLine ("getPropertyBoolean ns1:Bool1 =   " + b);

            int integer = meta.GetPropertyInteger(TestData.NS1, "Int");
            log.WriteLine ("getPropertyBoolean ns1:Int =   " + integer);

            double d = meta.GetPropertyDouble(TestData.NS1, "Double");
            log.WriteLine ("getPropertyBoolean ns1:Int =   " + d);
            log.WriteLine();

            for (int i = 1; i <= 13; i++)
            {
                var dateName = "Date" + i;
                var dt = meta.GetPropertyDate (TestData.NS1, dateName);
                if(dt != null && dt.HasTimeZone)
                    dt.TimeZone = TimeZoneInfo.CreateCustomTimeZone("OFFSET" + dt.Offset, dt.Offset, string.Empty, string.Empty);

                log.WriteLine ("getPropertyDate (" + i + ") =   " + dt);
                meta.SetPropertyDate (TestData.NS2, dateName, dateValue);
            }

            printXmpMeta (meta, "Get and re-set the dates in NS2");
        }


        /**
         * Parse and serialize methods.
         * @throws XmpException Forwards exceptions
         */
        private static void CoverParsing()
        {
            writeMajorLabel ("Test parsing with multiple buffers and various options");

            var meta = XmpMetaFactory.ParseFromString(TestData.SIMPLE_RDF);
            printXmpMeta (meta, "Parse from String");

            meta = XmpMetaFactory.ParseFromString(TestData.SIMPLE_RDF, new ParseOptions { RequireXmpMeta = true });
            printXmpMeta(meta, "Parse and require xmpmeta element, which is missing");

            meta = XmpMetaFactory.ParseFromString(TestData.NAMESPACE_RDF);
            printXmpMeta(meta, "Parse RDF with multiple nested namespaces");

            meta = XmpMetaFactory.ParseFromString(TestData.XMPMETA_RDF, new ParseOptions { RequireXmpMeta = true });
            printXmpMeta(meta, "Parse and require xmpmeta element, which is present");

            meta = XmpMetaFactory.ParseFromString(TestData.INCONSISTENT_RDF);
            printXmpMeta(meta, "Parse and reconcile inconsistent aliases");

            try
            {
                XmpMetaFactory.ParseFromString(TestData.INCONSISTENT_RDF, new ParseOptions { StrictAliasing = true });
            }
            catch (XmpException e)
            {
                log.WriteLine("Parse and do not reconcile inconsistent aliases - threw XmpException #{0} :   {1}", e.GetErrorCode(), e.Message);
            }
        }


        /**
         * Test CR and LF in values.
         * @throws XmpException Forwards exceptions
         */
        private static void CoverLinefeedValues()
        {
            writeMajorLabel ("Test CR and LF in values");

            string valueWithCR        = "ASCII \r CR";
            string valueWithLF        = "ASCII \n LF";
            string valueWithCRLF    = "ASCII \r\n CRLF";

            var meta = XmpMetaFactory.ParseFromString(TestData.NEWLINE_RDF);

            meta.SetProperty (TestData.NS2, "HasCR", valueWithCR);
            meta.SetProperty (TestData.NS2, "HasLF", valueWithLF);
            meta.SetProperty (TestData.NS2, "HasCRLF", valueWithCRLF);

            string result = XmpMetaFactory.SerializeToString(meta, new SerializeOptions { OmitPacketWrapper = true });
            log.WriteLine(result);

            var hasCR = meta.GetPropertyString (TestData.NS1, "HasCR");
            var hasCR2 = meta.GetPropertyString (TestData.NS2, "HasCR");
            var hasLF = meta.GetPropertyString (TestData.NS1, "HasLF");
            var hasLF2 = meta.GetPropertyString (TestData.NS2, "HasLF");
            var hasCRLF = meta.GetPropertyString (TestData.NS1, "HasCRLF");
            var hasCRLF2 = meta.GetPropertyString (TestData.NS2, "HasCRLF");

            if (hasCR == valueWithCR && hasCR2 == valueWithCR &&
                hasLF == valueWithLF && hasLF2 == valueWithLF &&
                hasCRLF == valueWithCRLF && hasCRLF2 == valueWithCRLF)
            {
                log.WriteLine();
                log.WriteLine("\n## HasCR and HasLF and HasCRLF correctly retrieved\n");
            }
        }


        /**
         * Covers the serialization of an <code>XmpMeta</code> object with different options.
         * @throws Exception Forwards exceptions
         */
        private static void CoverSerialization()
        {
            writeMajorLabel ("Test serialization with various options");

            var meta = XmpMetaFactory.ParseFromString(TestData.SIMPLE_RDF);
            meta.SetProperty (TestData.NS2, "Another", "Something in another schema");
            meta.SetProperty (TestData.NS2, "Yet/pdf:More", "Yet more in another schema");

            printXmpMeta (meta, "Parse simple RDF, serialize with various options");

            writeMinorLabel ("Default serialize");
            log.WriteLine(XmpMetaFactory.SerializeToString(meta, null));

            writeMinorLabel ("Compact RDF, no packet serialize");
            log.WriteLine(XmpMetaFactory.SerializeToString(meta, new SerializeOptions { UseCompactFormat = true, OmitPacketWrapper = true }));

            writeMinorLabel ("Read-only serialize");
            log.WriteLine(XmpMetaFactory.SerializeToString(meta, new SerializeOptions { ReadOnlyPacket = true }));

            writeMinorLabel ("Alternate newline serialize");
            log.WriteLine(XmpMetaFactory.SerializeToString(meta, new SerializeOptions { Newline = "<--newline-->\n", OmitPacketWrapper = true }));

            writeMinorLabel ("Alternate indent serialize");
            log.WriteLine(XmpMetaFactory.SerializeToString(meta, new SerializeOptions { Indent = "-->", BaseIndent = 5, OmitPacketWrapper = true }));

            writeMinorLabel ("Small padding serialize");
            log.WriteLine(XmpMetaFactory.SerializeToString(meta, new SerializeOptions { Padding = 10 }));

            writeMinorLabel ("Serialize with exact packet size");
            int s = XmpMetaFactory.SerializeToBuffer(meta, new SerializeOptions { ReadOnlyPacket = true }).Length;
            log.WriteLine ("Minimum packet size is " + s + " bytes\n");

            // with the flag "exact packet size" the padding becomes the overall length of the packet
            byte[] buffer = XmpMetaFactory.SerializeToBuffer(meta, new SerializeOptions { ExactPacketLength = true, Padding = s });
            log.WriteLine(Encoding.UTF8.GetString(buffer));

            try
            {
                XmpMetaFactory.ParseFromString(XmpMetaFactory.SerializeToString(meta, new SerializeOptions { ExactPacketLength = true, Padding = s - 1 }));
            }
            catch (XmpException e)
            {
                log.WriteLine("\nExact packet size smaller than minimal packet length - threw XmpException #{0} :   {1}", e.GetErrorCode(), e.Message);
            }
        }


        /**
         * Cover different use cases of the <code>XmpIterator</code>.
         * @throws XmpException Forwards exceptions
         */
        private static void CoverIterator()
        {
            writeMajorLabel ("Test iteration methods");

            var meta = XmpMetaFactory.ParseFromString(TestData.RDF_COVERAGE);
            meta.SetProperty (TestData.NS2, "Prop", "Prop value");
            meta.AppendArrayItem(TestData.NS2, "Bag", new PropertyOptions { IsArray = true }, "BagItem 2", null);
            meta.AppendArrayItem(TestData.NS2, "Bag", "BagItem 1");
            meta.AppendArrayItem(TestData.NS2, "Bag", "BagItem 3");

            printXmpMeta (meta, "Parse \"coverage\" RDF, add Bag items out of order");
/*
            TODO reinstate this code

            writeMinorLabel ("Default iteration");
            for (XmpIterator it = meta.Iterator(); it.hasNext();)
            {
                XmpPropertyInfo prop = (XmpPropertyInfo) it.next();
                printPropertyInfo(prop);
            }

            writeMinorLabel ("Iterate omitting qualifiers");
            for (XmpIterator it = meta.iterator(new IteratorOptions().setOmitQualifiers(true)); it
                    .hasNext();)
            {
                XmpPropertyInfo prop = (XmpPropertyInfo) it.next();
                printPropertyInfo(prop);
            }

            writeMinorLabel("Iterate with just leaf names");
            for (XmpIterator it = meta.iterator(new IteratorOptions().setJustLeafname(true)); it
                    .hasNext();)
            {
                XmpPropertyInfo prop = (XmpPropertyInfo) it.next();
                printPropertyInfo(prop);
            }

            writeMinorLabel("Iterate with just leaf nodes");
            for (XmpIterator it = meta.iterator(new IteratorOptions().setJustLeafnodes(true)); it
                    .hasNext();)
            {
                XmpPropertyInfo prop = (XmpPropertyInfo) it.next();
                printPropertyInfo(prop);
            }

            writeMinorLabel("Iterate just the schema nodes");
            for (XmpIterator it = meta.iterator(new IteratorOptions().setJustChildren(true)); it
                    .hasNext();)
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
            for (XmpIterator it = meta.iterator(NS2, null, new IteratorOptions().setJustChildren(true)
                    .setJustLeafname(true)); it.hasNext();)
            {
                XmpPropertyInfo prop = (XmpPropertyInfo) it.next();
                printPropertyInfo(prop);
            }

            writeMinorLabel("Iterate the top of the ns2: namespace with just leaf nodes");
            for (XmpIterator it = meta.iterator(NS2, null, new IteratorOptions().setJustChildren(true)
                    .setJustLeafnodes(true)); it.hasNext();)
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
        private static void CoverPathCreation()
        {
            writeMajorLabel ("XPath composition utilities");

            var meta = XmpMetaFactory.Create();

            meta.AppendArrayItem(TestData.NS1, "ArrayProp", new PropertyOptions { IsArray = true }, "Item 1", null);

            string path = XmpPathFactory.ComposeArrayItemPath("ArrayProp", 2);
            log.WriteLine ("composeArrayItemPath ArrayProp[2] =   " + path);
            meta.SetProperty (TestData.NS1, path, "new ns1:ArrayProp[2] value");

            path = "StructProperty";
            path += XmpPathFactory.ComposeStructFieldPath(TestData.NS2, "Field3");
            log.WriteLine ("composeStructFieldPath StructProperty/ns2:Field3 =   " + path);
            meta.SetProperty (TestData.NS1, path, "new ns1:StructProp/ns2:Field3 value");

            path = "QualProp";
            path += XmpPathFactory.ComposeQualifierPath(TestData.NS2, "Qual");
            log.WriteLine ("composeStructFieldPath QualProp/?ns2:Qual =   " + path);
            meta.SetProperty (TestData.NS1, path, "new ns1:QualProp/?ns2:Qual value");

            meta.SetLocalizedText(TestData.NS1, "AltTextProp", null, "en-US", "initival value");
            path = "AltTextProp";
            path += XmpPathFactory.ComposeQualifierPath(XmpConstants.NsXml, "lang");
            log.WriteLine ("composeQualifierPath ns1:AltTextProp/?xml:lang =   " + path);
            meta.SetProperty (TestData.NS1, path, "new ns1:AltTextProp/?xml:lang value");

            printXmpMeta (meta, "Modified simple RDF");
        }


        /**
         * Date/Time utilities
         */
        private static void CoverDateTime()
        {
            writeMajorLabel ("Test date/time utilities and special values");

            var    date1    = XmpDateTimeFactory.Create(2000, 1, 31, 12, 34, 56, -1);
            date1.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

            var    date2    = XmpDateTimeFactory.Create(0, 0, 0, 0, 0, 0, 0);

            GregorianCalendar cal = new GregorianCalendar(2007, 1, 28);
            var    date3    = XmpDateTimeFactory.CreateFromCalendar(cal);

            var currentDateTime = XmpDateTimeFactory.GetCurrentDateTime();

            log.WriteLine("Print date 2000 Jan 31 12:34:56 PST =   {0}", date1);
            log.WriteLine("Print zero date =   {0}", date2);
            log.WriteLine("Print date created by a calendar =   {0}", date3);
            log.WriteLine("Print current date =   {0}", currentDateTime);
            log.WriteLine();
        }




        // ---------------------------------------------------------------------------------------------
        // Utilities for this example
        // ---------------------------------------------------------------------------------------------

        /**
         * Print the content of an XmpMeta object a headline and its name.
         * @param meta an <code>XmpMeta</code> object
         * @param title the headline
         */
        private static void printXmpMeta(IXmpMeta meta, string title)
        {
            string name = meta.GetObjectName();
            if (!string.IsNullOrEmpty(name))
            {
                log.WriteLine("{0} (Name: '{1}'):", title, name);
            }
            else
            {
                log.WriteLine("{0}:", title);
            }
            log.WriteLine(meta.DumpObject());
            log.WriteLine();
        }


        /**
         * @param prop an <code>XmpPropertyInfo</code> from the <code>XmpIterator</code>.
         */
        private static void printPropertyInfo(IXmpPropertyInfo prop)
        {
            log.WriteLine("NS ({0})   PATH ({1})   VALUE ({2})  OPTIONS ({3})",
                prop.Namespace, prop.Path, prop.Value, prop.Options.GetOptionsString());
        }


        /**
         * Dump the alias list to the output.
         */
        private static void dumpAliases()
        {
            var aliases = registry.Aliases;
            foreach (var qname in aliases.Keys)
                log.WriteLine("{0}   --->   {1}", qname, aliases[qname]);
            log.WriteLine();
        }


        /**
         * Writes a major headline to the output.
         * @param title the headline
         */
        private static void writeMajorLabel (string title)
        {
            log.WriteLine();
            log.WriteLine("// =============================================================================");
            log.WriteLine("// {0}", title);
            log.WriteLine("// =============================================================================");
            log.WriteLine();
        }


        /**
         * Writes a minor headline to the output.
         * @param title the headline
         */
        private static void writeMinorLabel (string title)
        {
            log.WriteLine();
            log.WriteLine ("// -----------------------------------------------------------------------------".Substring(0, title.Length + 3));
            log.WriteLine ("// {0}", title);
            log.WriteLine();
        }
    }
}
