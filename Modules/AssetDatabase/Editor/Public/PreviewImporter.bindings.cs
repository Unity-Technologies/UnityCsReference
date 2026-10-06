// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using Object = UnityEngine.Object;
using System;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Modules/AssetPipelineEditor/Public/PreviewImporter.h")]
    [NativeClass("PreviewImporter", PersistentTypeId = 0x309881D4)]
    [ExcludeFromPreset]
    internal partial class PreviewImporter : AssetImporter
    {
        internal PreviewImporter(global::UnityEngine.EntityId id) : base(id) {}
    }
}
