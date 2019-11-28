// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor.Build;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.U2D;

namespace UnityEditor.U2D
{
    // SpriteAtlas Importer lets you modify [[SpriteAtlas]]
    [NativeHeader("Editor/Src/2D/SpriteAtlas/SpriteAtlasImporter.h")]
    public sealed partial class SpriteAtlasImporter : AssetImporter
    {
        extern internal static void MigrateAllSpriteAtlases();
    }
};
