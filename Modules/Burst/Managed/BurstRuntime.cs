// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Burst
{
    /// <summary>
    /// Provides helper intrinsics that can be used at runtime.
    /// </summary>
    public static class BurstRuntime
    {
        /// <summary>
        /// Gets a 32-bits hashcode from a type computed for the <see cref="System.Type.AssemblyQualifiedName"/>
        /// </summary>
        /// <typeparam name="T">The type to compute the hash from</typeparam>
        /// <returns>The 32-bit hashcode.</returns>
        public static int GetHashCode32<T>()
        {
            return HashCode32<T>.Value;
        }

        /// <summary>
        /// Gets a 32-bits hashcode from a type computed for the <see cref="System.Type.AssemblyQualifiedName"/>
        /// This method cannot be used from a burst job.
        /// </summary>
        /// <param name="type">The type to compute the hash from</param>
        /// <returns>The 32-bit hashcode.</returns>
        public static int GetHashCode32(Type type)
        {
            return HashStringWithFNV1A32(type.AssemblyQualifiedName);
        }

        /// <summary>
        /// Gets a 64-bits hashcode from a type computed for the <see cref="System.Type.AssemblyQualifiedName"/>
        /// </summary>
        /// <typeparam name="T">The type to compute the hash from</typeparam>
        /// <returns>The 64-bit hashcode.</returns>
        public static long GetHashCode64<T>()
        {
            return HashCode64<T>.Value;
        }

        /// <summary>
        /// Gets a 64-bits hashcode from a type computed for the <see cref="System.Type.AssemblyQualifiedName"/>.
        /// This method cannot be used from a burst job.
        /// </summary>
        /// <param name="type">Type to calculate a hash for</param>
        /// <returns>The 64-bit hashcode.</returns>
        public static long GetHashCode64(Type type)
        {
            return HashStringWithFNV1A64(type.AssemblyQualifiedName);
        }

        // method internal as it is used by the compiler directly
        internal static int HashStringWithFNV1A32(string text)
        {
            // Using http://www.isthe.com/chongo/tech/comp/fnv/index.html#FNV-1a
            // with basis and prime:
            const uint offsetBasis = 2166136261;
            const uint prime = 16777619;

            uint result = offsetBasis;
            foreach (var c in text)
            {
                result = prime * (result ^ (byte)(c & 255));
                result = prime * (result ^ (byte)(c >> 8));
            }
            return (int)result;
        }

        // method internal as it is used by the compiler directly
        // WARNING: This **must** be kept in sync with the definition in ILPostProcessing.cs!
        internal static long HashStringWithFNV1A64(string text)
        {
            // Using http://www.isthe.com/chongo/tech/comp/fnv/index.html#FNV-1a
            // with basis and prime:
            const ulong offsetBasis = 14695981039346656037;
            const ulong prime = 1099511628211;

            ulong result = offsetBasis;

            foreach (var c in text)
            {
                result = prime * (result ^ (byte)(c & 255));
                result = prime * (result ^ (byte)(c >> 8));
            }

            return (long)result;
        }

        private struct HashCode32<T>
        {
            public static readonly int Value = HashStringWithFNV1A32(typeof(T).AssemblyQualifiedName);
        }

        private struct HashCode64<T>
        {
            public static readonly long Value = HashStringWithFNV1A64(typeof(T).AssemblyQualifiedName);
        }


        /// <summary>
        /// Allows for loading additional Burst native libraries
        /// Important: Designed for Play mode / Desktop Standalone Players ONLY
        /// In Editor, any libraries that have been loaded will be unloaded on exit of playmode
        /// Only supported from 2021.1 and later. You can use BurstCompiler.IsLoadAdditionalLibrarySupported() to confirm it is available.
        /// </summary>
        /// <param name="pathToLibBurstGenerated">Absolute filesystem location of bursted library to load</param>
        /// <returns>true if the library was loaded successfully</returns>
        public static bool LoadAdditionalLibrary(string pathToLibBurstGenerated)
        {
            if (BurstCompiler.IsLoadAdditionalLibrarySupported())
            {
                return LoadAdditionalLibraryInternal(pathToLibBurstGenerated);
            }
            return false;
        }

        internal static bool LoadAdditionalLibraryInternal(string pathToLibBurstGenerated)
        {
            return (bool)typeof(Unity.Burst.LowLevel.BurstCompilerService).GetMethod("LoadBurstLibrary").Invoke(null, new object[] { pathToLibBurstGenerated });
        }

        // This #if is a hack to workaround the BurstCompilerService.RuntimeLog API not being available
        // in the player mock library yet. And we can't rebuild the player mock library right now because
        // it requires netstandard2.1, which breaks our test project that compiles against net471. When
        // we can change our tests project to compile against netstandard2.1 instead of net471, we can
        // remove this #if.
        [Unity.Scripting.RequiredByAssembly]
        internal static unsafe void RuntimeLog(byte* message, int logType, byte* fileName, int lineNumber)
        {
            Unity.Burst.LowLevel.BurstCompilerService.RuntimeLog((byte*) 0, (Unity.Burst.LowLevel.BurstCompilerService.BurstLogType)logType, message, fileName, lineNumber);
        }
        [VisibleToOtherModules("UnityEditor.BurstModule")]
        internal static void Initialize()
        {
        }


        [Unity.Scripting.RequiredByAssembly]
        internal static void PreventRequiredAttributeStrip()
        {
            new BurstDiscardAttribute();
            // We also need to retain [Condition("UNITY_ASSERTION")] attributes in order to compile
            // some assertion correctly (i.e. not compile them)
            new ConditionalAttribute("HEJSA");
        }


        [Unity.Scripting.RequiredByAssembly]

        [RequiredByNativeCode(Optional = true)]
        internal static unsafe void Log(byte* message, int logType, byte* fileName, int lineNumber)
        {
            Unity.Burst.LowLevel.BurstCompilerService.Log((byte*) 0, (Unity.Burst.LowLevel.BurstCompilerService.BurstLogType)logType, message, (byte*) 0, lineNumber);
        }

        /// <summary>
        /// Return a pointer to read-only memory consisting of the literal UTF-8 bytes of a string constant.
        /// </summary>
        /// <param name="str">A string which must a string literal</param>
        /// <param name="byteCount">Receives the number of UTF-8 encoded bytes the constant contains (excluding null terminator)</param>
        /// <returns>A pointer to constant data representing the UTF-8 encoded bytes of the string literal, terminated with a null terminator</returns>
        public unsafe static byte* GetUTF8LiteralPointer(string str, out int byteCount)
        {
            throw new NotImplementedException("This function only works from Burst");
        }

    }
}
