// =================================================================================================
// ADOBE SYSTEMS INCORPORATED
// Copyright 2006 Adobe Systems Incorporated
// All Rights Reserved
//
// NOTICE:  Adobe permits you to use, modify, and distribute this file in accordance with the terms
// of the Adobe license agreement accompanying it.
// =================================================================================================


using System.Collections.Generic;
using System.Text;

namespace XmpCore.Options
{
    /// <summary>The base class for a collection of 32 flag bits.</summary>
    /// <remarks>
    /// The base class for a collection of 32 flag bits. Individual flags are defined as enum value bit
    /// masks. Inheriting classes add convenience accessor methods.
    /// </remarks>
    /// <since>24.01.2006</since>
    public abstract class Options
    {
        /// <summary>the internal int containing all options</summary>
        private int _options;

        /// <summary>a map containing the bit names</summary>
        private IDictionary<int, string> _optionNames;

        /// <summary>The default constructor.</summary>
        protected Options()
        {
        }

        /// <summary>Constructor with the options bit mask.</summary>
        /// <param name="options">the options bit mask</param>
        /// <exception cref="XmpException">If the options are not correct</exception>
        protected Options(int options)
        {
            AssertOptionsValid(options);
            SetOptions(options);
        }

        /// <summary>Resets the options.</summary>
        public void Clear()
        {
            _options = 0;
        }

        /// <param name="optionBits">an option bitmask</param>
        /// <returns>Returns true, if this object is equal to the given options.</returns>
        public bool IsExactly(int optionBits)
        {
            return GetOptions() == optionBits;
        }

        /// <param name="optionBits">an option bitmask</param>
        /// <returns>Returns true, if this object contains all given options.</returns>
        public bool ContainsAllOptions(int optionBits)
        {
            return (GetOptions() & optionBits) == optionBits;
        }

        /// <param name="optionBits">an option bitmask</param>
        /// <returns>Returns true, if this object contain at least one of the given options.</returns>
        public bool ContainsOneOf(int optionBits)
        {
            return ((GetOptions()) & optionBits) != 0;
        }

        /// <param name="optionBit">the binary bit or bits that are requested</param>
        /// <returns>Returns if <emp>all</emp> of the requested bits are set or not.</returns>
        protected bool GetOption(int optionBit)
        {
            return (_options & optionBit) != 0;
        }

        /// <param name="optionBits">the binary bit or bits that shall be set to the given value</param>
        /// <param name="value">the boolean value to set</param>
        public void SetOption(int optionBits, bool value)
        {
            _options = value ? _options | optionBits : _options & ~optionBits;
        }

        /// <summary>Is friendly to access it during the tests.</summary>
        /// <returns>Returns the options.</returns>
        public int GetOptions()
        {
            return _options;
        }

        /// <param name="options">The options to set.</param>
        /// <exception cref="XmpException"></exception>
        public void SetOptions(int options)
        {
            AssertOptionsValid(options);
            _options = options;
        }

        public override bool Equals(object obj)
        {
            var options = obj as Options;
            return options != null && GetOptions() == options.GetOptions();
        }

        public override int GetHashCode()
        {
            return GetOptions();
        }

        /// <summary>Creates a human readable string from the set options.</summary>
        /// <remarks>
        /// <em>Note:</em> This method is quite expensive and should only be used within tests or as
        /// </remarks>
        /// <returns>
        /// Returns a string listing all options that are set to <c>true</c> by their name,
        /// like "option1 | option4".
        /// </returns>
        public string GetOptionsString()
        {
            if (_options != 0)
            {
                var sb = new StringBuilder();
                var theBits = _options;
                while (theBits != 0)
                {
                    var oneLessBit = theBits & (theBits - 1);
                    // clear rightmost one bit
                    var singleBit = theBits ^ oneLessBit;
                    var bitName = GetOptionName(singleBit);
                    sb.Append(bitName);
                    if (oneLessBit != 0)
                        sb.Append(" | ");
                    theBits = oneLessBit;
                }
                return sb.ToString();
            }
            return "<none>";
        }

        /// <returns>Returns the options as hex bitmask.</returns>
        public override string ToString()
        {
            return $"0x{_options:X}";
        }

        /// <summary>To be implemented by inheritants.</summary>
        /// <returns>Returns a bit mask where all valid option bits are set.</returns>
        protected abstract int GetValidOptions();

        /// <summary>To be implemented by inheritants.</summary>
        /// <param name="option">a single, valid option bit.</param>
        /// <returns>Returns a human readable name for an option bit.</returns>
        protected abstract string DefineOptionName(int option);

        /// <summary>The inheriting option class can do additional checks on the options.</summary>
        /// <remarks>
        /// The inheriting option class can do additional checks on the options.
        /// <em>Note:</em> For performance reasons this method is only called
        /// when setting bitmasks directly.
        /// When get- and set-methods are used, this method must be called manually,
        /// normally only when the Options-object has been created from a client
        /// (it has to be made public therefore).
        /// </remarks>
        /// <param name="options">the bitmask to check.</param>
        /// <exception cref="XmpException">Thrown if the options are not consistent.</exception>
        internal virtual void AssertConsistency(int options)
        {
        }

        /// <summary>Checks options before they are set.</summary>
        /// <remarks>
        /// First it is checked if only defined options are used, second the additional
        /// <see cref="AssertConsistency(int)"/>-method is called.
        /// </remarks>
        /// <param name="options">the options to check</param>
        /// <exception cref="XmpException">Thrown if the options are invalid.</exception>
        private void AssertOptionsValid(int options)
        {
            var invalidOptions = options & ~GetValidOptions();

            if (invalidOptions != 0)
                throw new XmpException($"The option bit(s) 0x{invalidOptions:X} are invalid!", XmpErrorCode.BadOptions);

            AssertConsistency(options);
        }

        /// <summary>Looks up or asks the inherited class for the name of an option bit.</summary>
        /// <remarks>
        /// Looks up or asks the inherited class for the name of an option bit.
        /// Its save that there is only one valid option handed into the method.
        /// </remarks>
        /// <param name="option">a single option bit</param>
        /// <returns>Returns the option name or undefined.</returns>
        private string GetOptionName(int option)
        {
            var optionsNames = ProcureOptionNames();

            string result;
            optionsNames.TryGetValue(option, out result);

            if (result == null)
            {
                result = DefineOptionName(option);
                if (result != null)
                    optionsNames[option] = result;
                else
                    result = "<option name not defined>";
            }

            return result;
        }

        /// <returns>Returns the optionNames map and creates it if required.</returns>
        private IDictionary<int, string> ProcureOptionNames()
        {
            return _optionNames ?? (_optionNames = new Dictionary<int, string>());
        }
    }
}
