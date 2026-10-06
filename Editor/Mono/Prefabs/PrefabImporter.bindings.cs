// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using Object = UnityEngine.Object;
using System;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/AssetPipeline/PrefabImporter.h")]
    [NativeClass("PrefabImporter", PersistentTypeId = 0x1BEBB377)]
    [ExcludeFromPreset]
    internal class PrefabImporter : AssetImporter
    {
        internal PrefabImporter(global::UnityEngine.EntityId id) : base(id) {}
    }
}
