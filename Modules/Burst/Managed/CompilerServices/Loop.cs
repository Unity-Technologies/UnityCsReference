// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Burst.CompilerServices
{
    /// <summary>
    /// Provides intrinsics for controlling loop vectorization in Burst-compiled code.
    /// </summary>
    public static class Loop
    {
        /// <summary>
        /// Must be called from inside a loop.
        /// Will cause a compiler error in Burst-compiled code if the loop is not auto-vectorized.
        /// </summary>
        public static void ExpectVectorized() { }

        /// <summary>
        /// Must be called from inside a loop.
        /// Will cause a compiler error in Burst-compiled code if the loop is auto-vectorized.
        /// </summary>
        public static void ExpectNotVectorized() { }
    }
}
