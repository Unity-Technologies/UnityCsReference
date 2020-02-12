// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering
{
    // Keep in sync with ShaderLab::SerializedProperty::SerializedPropertyType.
    public enum ShaderPropertyType
    {
        Color,
        Vector,
        Float,
        Range,
        Texture,
    }

    // Keep in sync with ShaderLab::SerializedProperty::Flags.
    [Flags]
    public enum ShaderPropertyFlags
    {
        None                        = 0,
        HideInInspector             = 1 << 0,
        PerRendererData             = 1 << 1,
        NoScaleOffset               = 1 << 2,
        Normal                      = 1 << 3,
        HDR                         = 1 << 4,
        Gamma                       = 1 << 5,
        NonModifiableTextureData    = 1 << 6,
        MainTexture                 = 1 << 7,
        MainColor                   = 1 << 8,
    }
}

namespace UnityEngine
{
    [NativeHeader("Runtime/Graphics/ShaderScriptBindings.h")]
    partial class Shader
    {
        [FreeFunction("ShaderScripting::GetPropertyName")]
        extern private static string GetPropertyName([NotNull] Shader shader, int propertyIndex);
        [FreeFunction("ShaderScripting::GetPropertyNameId")]
        extern private static int GetPropertyNameId([NotNull] Shader shader, int propertyIndex);
        [FreeFunction("ShaderScripting::GetPropertyType")]
        extern private static ShaderPropertyType GetPropertyType([NotNull] Shader shader, int propertyIndex);
        [FreeFunction("ShaderScripting::GetPropertyDescription")]
        extern private static string GetPropertyDescription([NotNull] Shader shader, int propertyIndex);
        [FreeFunction("ShaderScripting::GetPropertyFlags")]
        extern private static ShaderPropertyFlags GetPropertyFlags([NotNull] Shader shader, int propertyIndex);
        [FreeFunction("ShaderScripting::GetPropertyAttributes")]
        extern private static string[] GetPropertyAttributes([NotNull] Shader shader, int propertyIndex);
        [FreeFunction("ShaderScripting::GetPropertyDefaultValue")]
        extern private static Vector4 GetPropertyDefaultValue([NotNull] Shader shader, int propertyIndex);
        [FreeFunction("ShaderScripting::GetPropertyTextureDimension")]
        extern private static TextureDimension GetPropertyTextureDimension([NotNull] Shader shader, int propertyIndex);
        [FreeFunction("ShaderScripting::GetPropertyTextureDefaultName")]
        extern private static string GetPropertyTextureDefaultName([NotNull] Shader shader, int propertyIndex);
        [FreeFunction("ShaderScripting::FindTextureStack")]
        extern private static bool FindTextureStackImpl([NotNull] Shader s, int propertyIdx, out string stackName, out int layerIndex);

        private static void CheckPropertyIndex(Shader s, int propertyIndex)
        {
            if (propertyIndex < 0 || propertyIndex >= s.GetPropertyCount())
                throw new ArgumentOutOfRangeException("propertyIndex");
        }

        extern public int GetPropertyCount();

        extern public int FindPropertyIndex(string propertyName);

        public string GetPropertyName(int propertyIndex)
        {
            CheckPropertyIndex(this, propertyIndex);
            return GetPropertyName(this, propertyIndex);
        }

        public int GetPropertyNameId(int propertyIndex)
        {
            CheckPropertyIndex(this, propertyIndex);
            return GetPropertyNameId(this, propertyIndex);
        }

        public ShaderPropertyType GetPropertyType(int propertyIndex)
        {
            CheckPropertyIndex(this, propertyIndex);
            return GetPropertyType(this, propertyIndex);
        }

        public string GetPropertyDescription(int propertyIndex)
        {
            CheckPropertyIndex(this, propertyIndex);
            return GetPropertyDescription(this, propertyIndex);
        }

        public ShaderPropertyFlags GetPropertyFlags(int propertyIndex)
        {
            CheckPropertyIndex(this, propertyIndex);
            return GetPropertyFlags(this, propertyIndex);
        }

        public string[] GetPropertyAttributes(int propertyIndex)
        {
            CheckPropertyIndex(this, propertyIndex);
            return GetPropertyAttributes(this, propertyIndex);
        }

        public float GetPropertyDefaultFloatValue(int propertyIndex)
        {
            CheckPropertyIndex(this, propertyIndex);
            var propType = GetPropertyType(propertyIndex);
            if (propType != ShaderPropertyType.Float && propType != ShaderPropertyType.Range)
                throw new ArgumentException("Property type is not Float or Range.");
            return GetPropertyDefaultValue(this, propertyIndex)[0];
        }

        public Vector4 GetPropertyDefaultVectorValue(int propertyIndex)
        {
            CheckPropertyIndex(this, propertyIndex);
            var propType = GetPropertyType(propertyIndex);
            if (propType != ShaderPropertyType.Color && propType != ShaderPropertyType.Vector)
                throw new ArgumentException("Property type is not Color or Vector.");
            return GetPropertyDefaultValue(this, propertyIndex);
        }

        public Vector2 GetPropertyRangeLimits(int propertyIndex)
        {
            CheckPropertyIndex(this, propertyIndex);
            if (GetPropertyType(propertyIndex) != ShaderPropertyType.Range)
                throw new ArgumentException("Property type is not Range.");
            var defValues = GetPropertyDefaultValue(this, propertyIndex);
            return new Vector2(defValues[1], defValues[2]);
        }

        public TextureDimension GetPropertyTextureDimension(int propertyIndex)
        {
            CheckPropertyIndex(this, propertyIndex);
            if (GetPropertyType(propertyIndex) != ShaderPropertyType.Texture)
                throw new ArgumentException("Property type is not TexEnv.");
            return GetPropertyTextureDimension(this, propertyIndex);
        }

        public string GetPropertyTextureDefaultName(int propertyIndex)
        {
            CheckPropertyIndex(this, propertyIndex);
            var propType = GetPropertyType(propertyIndex);
            if (propType != ShaderPropertyType.Texture)
                throw new ArgumentException("Property type is not Texture.");
            return GetPropertyTextureDefaultName(this, propertyIndex);
        }

        public bool FindTextureStack(int propertyIndex, out string stackName, out int layerIndex)
        {
            CheckPropertyIndex(this, propertyIndex);
            var propType = GetPropertyType(propertyIndex);
            if (propType != ShaderPropertyType.Texture)
                throw new ArgumentException("Property type is not Texture.");
            return FindTextureStackImpl(this, propertyIndex, out stackName, out layerIndex);
        }
    }
}
