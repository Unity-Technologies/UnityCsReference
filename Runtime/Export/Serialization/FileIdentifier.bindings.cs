// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    /// <summary>
    /// Specifies the type of file that a reference points to.
    /// </summary>
    /// <remarks>
    /// Used by LoadableReference to distinguish between different types of asset references.
    /// </remarks>
    [VisibleToOtherModules]
    /*UCBP-PUBLIC*/ internal enum FileIdentifierType
    {
        /// <summary>
        /// Reference to a non-asset file or built asset. The GUID may be a constant GUID or null.
        /// The path can point anywhere except in registered asset folders.
        /// </summary>
        NonAsset = 0,

        /// <summary>
        /// Reference to a source asset in the Assets folder. The GUID is valid and the resolved path
        /// is in a registered asset folder.
        /// </summary>
        SourceAsset = 2,

        /// <summary>
        /// Reference to a primary artifact in the Library folder. The GUID is valid and the resolved path
        /// is a primary artifact path.
        /// </summary>
        PrimaryArtifact = 3
    };
}
