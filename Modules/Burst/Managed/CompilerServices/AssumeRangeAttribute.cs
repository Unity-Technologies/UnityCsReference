// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Burst.CompilerServices
{
    /// <summary>
    /// Can be used to specify that a parameter or return has a range assumption.
    /// Assumptions feed directly into the optimizer and allow better codegen.
    ///
    /// Only usable on values of type scalar integer.
    ///
    /// The range is a closed interval [min..max] - EG. the attributed value
    /// is greater-than-or-equal-to min and less-than-or-equal-to max.
    /// </summary>
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter)]
    public class AssumeRangeAttribute : Attribute
    {
        /// <summary>
        /// Assume that an integer is in the signed closed interval [min..max].
        /// </summary>
        /// <param name="min">The inclusive minimum value.</param>
        /// <param name="max">The inclusive maximum value.</param>
        public AssumeRangeAttribute(int min, int max) { }

        /// <summary>
        /// Assume that an integer is in the unsigned closed interval [min..max].
        /// </summary>
        /// <param name="min">The inclusive minimum value.</param>
        /// <param name="max">The inclusive maximum value.</param>
        public AssumeRangeAttribute(uint min, uint max) { }

        /// <summary>
        /// Assume that an integer is in the signed closed interval [min..max].
        /// </summary>
        /// <param name="min">The inclusive minimum value.</param>
        /// <param name="max">The inclusive maximum value.</param>
        public AssumeRangeAttribute(long min, long max) { }

        /// <summary>
        /// Assume that an integer is in the unsigned closed interval [min..max].
        /// </summary>
        /// <param name="min">The inclusive minimum value.</param>
        /// <param name="max">The inclusive maximum value.</param>
        public AssumeRangeAttribute(ulong min, ulong max) { }
    }
}
