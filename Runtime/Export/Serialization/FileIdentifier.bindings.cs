// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    /*UCBP-PUBLIC*/ internal enum FileIdentifierType
    {
        // guid is valid if it's a const guid otherwise null, pathName can point to anywhere except in registered asset folders
        NonAsset = 0,

        // guid is valid, pathName is empty, the resolved path is in a registered asset folder
        SourceAsset = 2,

        // guid is valid, pathName is empty, the resolved path is a primary artifact path
        PrimaryArtifact = 3
    };
}
