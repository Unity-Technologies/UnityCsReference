// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.VFX
{
    [RequiredByNativeCode]
    struct VFXTemplate
    {
       public string name;
       public string category;
       public string description;
       public Texture2D icon;
       public Texture2D thumbnail;
    }

    [NativeHeader("Modules/VFXEditor/Public/VisualEffectImporter.h")]
    internal sealed partial class VisualEffectImporter : AssetImporter
    {
        public extern VFXTemplate templateProperty { get; set; }
    }
}
