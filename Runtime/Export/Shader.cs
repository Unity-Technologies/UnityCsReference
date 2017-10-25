// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using PassType = UnityEngine.Rendering.PassType;

// TODO: Shader and Material will be moved there later
// TODO: for now they are in Graphics.cs and are kept there while migration takes place to make diffs sane

namespace UnityEngine
{
    public sealed partial class ShaderVariantCollection : Object
    {
        public partial struct ShaderVariant
        {
            public Shader shader;
            public PassType passType;
            public string[] keywords;

            public ShaderVariant(Shader shader, PassType passType, params string[] keywords)
            {
                this.shader = shader; this.passType = passType; this.keywords = keywords;
                string checkMessage = CheckShaderVariant(shader, passType, keywords);
                if (!String.IsNullOrEmpty(checkMessage))
                    throw new ArgumentException(checkMessage);
            }
        }
    }

    public sealed partial class ShaderVariantCollection : Object
    {
        public ShaderVariantCollection() { Internal_Create(this); }

        public bool Add(ShaderVariant variant)      { return AddVariant(variant.shader, variant.passType, variant.keywords); }
        public bool Remove(ShaderVariant variant)   { return RemoveVariant(variant.shader, variant.passType, variant.keywords); }
        public bool Contains(ShaderVariant variant) { return ContainsVariant(variant.shader, variant.passType, variant.keywords); }
    }
}
