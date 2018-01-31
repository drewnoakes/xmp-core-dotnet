// =================================================================================================
// ADOBE SYSTEMS INCORPORATED
// Copyright 2006 Adobe Systems Incorporated
// All Rights Reserved
//
// NOTICE:  Adobe permits you to use, modify, and distribute this file in accordance with the terms
// of the Adobe license agreement accompanying it.
// =================================================================================================


using System.Text;

namespace XmpCore.Options
{
    /// <summary>
    /// Options for <see cref="XmpMetaFactory.SerializeToBuffer(IXmpMeta, SerializeOptions)"/>.
    /// </summary>
    /// <author>Stefan Makswit</author>
    /// <since>24.01.2006</since>
    public sealed class TemplateOptions : Options
    {
        /* Options used by apply Template functions */
        public const int ClearUnnamedPropertiesFlag = 0x0002;
        public const int ReplaceExistingPropertiesFlag = 0x0010;
        public const int IncludeInternalPropertiesFlag = 0x0020;
        public const int AddNewPropertiesFlag = 0x0040;
        public const int ReplaceWithDeleteEmptyFlag = 0x0080;

        /// <summary>Default constructor.</summary>
        public TemplateOptions()
        {
        }

        /// <summary>Constructor using initial options</summary>
        /// <param name="options">the initial options</param>
        /// <exception cref="XmpException">Thrown if options are not consistent.</exception>
        public TemplateOptions(int options)
            : base(options)
        {
        }

        /// <summary></summary>
        public bool ClearUnnamedProperties
        {
            get => GetOption(ClearUnnamedPropertiesFlag);
            set => SetOption(ClearUnnamedPropertiesFlag, value);
        }

        /// <summary></summary>
        public bool ReplaceExistingProperties
        {
            get => GetOption(ReplaceExistingPropertiesFlag);
            set => SetOption(ReplaceExistingPropertiesFlag, value);
        }

        /// <summary></summary>
        public bool IncludeInternalProperties
        {
            get => GetOption(IncludeInternalPropertiesFlag);
            set => SetOption(IncludeInternalPropertiesFlag, value);
        }

        /// <summary></summary>
        public bool AddNewProperties
        {
            get => GetOption(AddNewPropertiesFlag);
            set => SetOption(AddNewPropertiesFlag, value);
        }

        /// <summary></summary>
        public bool ReplaceWithDeleteEmpty
        {
            get => GetOption(ReplaceWithDeleteEmptyFlag);
            set => SetOption(ReplaceWithDeleteEmptyFlag, value);
        }

        /// <returns>Returns clone of this TemplateOptions-object with the same options set.</returns>
        public object Clone() => new TemplateOptions(GetOptions())
        {
        };

        protected override string DefineOptionName(int option)
        {
            switch (option)
            {
                case ClearUnnamedPropertiesFlag:
                    return "CLEAR_UNNAMED_PROPERTIES";
                case ReplaceExistingPropertiesFlag:
                    return "REPLACE_EXISTING_PROPERTIES";
                case IncludeInternalPropertiesFlag:
                    return "INCLUDE_INTERNAL_PROPERTIES";
                case AddNewPropertiesFlag:
                    return "ADD_NEW_PROPERTIES";
                case ReplaceWithDeleteEmptyFlag:
                    return "REPLACE_WITH_DELETE_EMPTY";
                default:
                    return null;
            }
        }

        protected override int GetValidOptions()
        {
            return ClearUnnamedPropertiesFlag | ReplaceExistingPropertiesFlag | IncludeInternalPropertiesFlag | AddNewPropertiesFlag
                | ReplaceWithDeleteEmptyFlag;
        }
    }
}
