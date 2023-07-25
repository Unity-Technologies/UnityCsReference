// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

namespace UnityEngine
{
    public sealed partial class ShaderVariantCollection : Object
    {
        public partial struct ShaderVariant
        {
            [FreeFunction][NativeConditional("UNITY_EDITOR")]
            extern private static string CheckShaderVariant(Shader shader, UnityEngine.Rendering.PassType passType, string[] keywords);
        }
    }

    public sealed partial class ShaderVariantCollection : Object
    {
        extern public int  shaderCount  { get; }
        extern public int  variantCount { get; }
        extern public bool isWarmedUp   {[NativeName("IsWarmedUp")] get; }

        extern private bool AddVariant(Shader shader, UnityEngine.Rendering.PassType passType, [Unmarshalled] string[] keywords);
        extern private bool RemoveVariant(Shader shader, UnityEngine.Rendering.PassType passType,[Unmarshalled] string[] keywords);
        extern private bool ContainsVariant(Shader shader, UnityEngine.Rendering.PassType passType, [Unmarshalled] string[] keywords);

        [NativeName("ClearVariants")] extern public void Clear();
        [NativeName("WarmupShaders")] extern public void WarmUp();

        [NativeName("CreateFromScript")] extern private static void Internal_Create([Writable] ShaderVariantCollection svc);
    }
}
