// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Entities
{
    /// <summary>
    /// Attribute used to signal this type should be scanned for <see cref="BurstMonoInteropMethodAttribute"/> decorated methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed class GenerateBurstMonoInteropAttribute : Attribute
    {
        /// <summary>
        /// Name of source file (without the .cs suffix) to generate interop code for. 
        /// </summary>
        /// <remarks> The interop file is placed beside this file in a `*.interop.gen.cs` file.
        /// </remarks>
        public string AssetName { get; private set; }

        /// <summary>
        /// <see cref="AssetName"/>
        /// </summary>
        /// <param name="assetName">Name of source file (without the .cs suffix) to generate interop code for.</param>
        public GenerateBurstMonoInteropAttribute(string assetName)
        {
            AssetName = assetName;
        }
    }

    /// <summary>
    /// Attribute that indicates that a method should have Burst to mono interop code generated.
    /// </summary>
    /// <remarks>
    /// Methods decorated with this attribute must be in a type also decorated with <see cref="GenerateBurstMonoInteropAttribute"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class BurstMonoInteropMethodAttribute : Attribute
    {
        /// <summary>
        /// If set, the generated method will be exposed as public. Otherwise it will have the same access modifier as
        /// the original method.
        /// </summary>
        public bool MakePublic;

        /// <summary>
        /// <see cref="MakePublic"/>
        /// </summary>
        /// <param name="makePublic">If set, the generated method will be exposed as public</param>
        public BurstMonoInteropMethodAttribute(bool makePublic = false)
        {
            MakePublic = makePublic;
        }
    }
}
