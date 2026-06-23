// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Burst.CompilerServices
{
    /// <summary>
    /// Compile-time aliasing intrinsics.
    /// </summary>
    public static class Aliasing
    {
        /// <summary>
        /// Will cause a compiler error in Burst-compiled code if a and b do not alias.
        /// </summary>
        /// <param name="a">A pointer to do aliasing checks on.</param>
        /// <param name="b">A pointer to do aliasing checks on.</param>
        public static unsafe void ExpectAliased(void* a, void* b) { }

        /// <summary>
        /// Will cause a compiler error in Burst-compiled code if a and b do not alias.
        /// </summary>
        /// <typeparam name="A">The type of a.</typeparam>
        /// <typeparam name="B">The type of b.</typeparam>
        /// <param name="a">A reference to do aliasing checks on.</param>
        /// <param name="b">A reference to do aliasing checks on.</param>
        public static void ExpectAliased<A, B>(in A a, in B b) where A : struct where B : struct { }

        /// <summary>
        /// Will cause a compiler error in Burst-compiled code if a and b do not alias.
        /// </summary>
        /// <typeparam name="B">The type of b.</typeparam>
        /// <param name="a">A pointer to do aliasing checks on.</param>
        /// <param name="b">A reference to do aliasing checks on.</param>
        public static unsafe void ExpectAliased<B>(void* a, in B b) where B : struct { }

        /// <summary>
        /// Will cause a compiler error in Burst-compiled code if a and b do not alias.
        /// </summary>
        /// <typeparam name="A">The type of a.</typeparam>
        /// <param name="a">A reference to do aliasing checks on.</param>
        /// <param name="b">A pointer to do aliasing checks on.</param>
        public static unsafe void ExpectAliased<A>(in A a, void* b) where A : struct { }

        /// <summary>
        /// Will cause a compiler error in Burst-compiled code if a and b can alias.
        /// </summary>
        /// <param name="a">A pointer to do aliasing checks on.</param>
        /// <param name="b">A pointer to do aliasing checks on.</param>
        public static unsafe void ExpectNotAliased(void* a, void* b) { }

        /// <summary>
        /// Will cause a compiler error in Burst-compiled code if a and b can alias.
        /// </summary>
        /// <typeparam name="A">The type of a.</typeparam>
        /// <typeparam name="B">The type of b.</typeparam>
        /// <param name="a">A reference to do aliasing checks on.</param>
        /// <param name="b">A reference to do aliasing checks on.</param>
        public static void ExpectNotAliased<A, B>(in A a, in B b) where A : struct where B : struct { }

        /// <summary>
        /// Will cause a compiler error in Burst-compiled code if a and b can alias.
        /// </summary>
        /// <typeparam name="B">The type of b.</typeparam>
        /// <param name="a">A pointer to do aliasing checks on.</param>
        /// <param name="b">A reference to do aliasing checks on.</param>
        public static unsafe void ExpectNotAliased<B>(void* a, in B b) where B : struct { }

        /// <summary>
        /// Will cause a compiler error in Burst-compiled code if a and b can alias.
        /// </summary>
        /// <typeparam name="A">The type of a.</typeparam>
        /// <param name="a">A reference to do aliasing checks on.</param>
        /// <param name="b">A pointer to do aliasing checks on.</param>
        public static unsafe void ExpectNotAliased<A>(in A a, void* b) where A : struct { }
    }
}
