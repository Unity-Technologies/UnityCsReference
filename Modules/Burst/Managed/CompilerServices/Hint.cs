// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Burst.CompilerServices
{
    /// <summary>
    /// Compile-time hint intrinsics.
    /// </summary>
    public static class Hint
    {
        /// <summary>
        /// Hints to the compiler that the condition is likely to be true.
        /// </summary>
        /// <param name="condition">The boolean condition that is likely to be true.</param>
        /// <returns>The condition.</returns>
        public static bool Likely(bool condition) => condition;

        /// <summary>
        /// Hints to the compiler that the condition is unlikely to be true.
        /// </summary>
        /// <param name="condition">The boolean condition that is unlikely to be true.</param>
        /// <returns>The condition.</returns>
        public static bool Unlikely(bool condition) => condition;

        /// <summary>
        /// Hints to the compiler that the condition can be assumed to be true.
        /// </summary>
        /// <param name="condition">The boolean condition that can be assumed to be true.</param>
        public static void Assume(bool condition) { }
    }
}
