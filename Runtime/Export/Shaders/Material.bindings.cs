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
    [NativeHeader("Runtime/Graphics/ShaderScriptBindings.h")]
    [NativeHeader("Runtime/Shaders/Material.h")]
    public partial class Material : Object
    {
        [FreeFunction("MaterialScripting::CreateWithShader")]   extern private static void CreateWithShader([Writable] Material self, [NotNull] Shader shader);
        [FreeFunction("MaterialScripting::CreateWithMaterial")] extern private static void CreateWithMaterial([Writable] Material self, [NotNull] Material source);

        public Material(Shader shader)   { CreateWithShader(this, shader); }
        // will otherwise be stripped if scene only uses default materials not explicitly referenced
        // (ie some components will get a default material if a material reference is null)
        [RequiredByNativeCode]
        public Material(Material source) { CreateWithMaterial(this, source); }

        // TODO: is it time to make it deprecated with error?
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Creating materials from shader source string is no longer supported. Use Shader assets instead.", true)]
        public Material(string contents) {}

        static extern internal Material GetDefaultMaterial();
        static extern internal Material GetDefaultParticleMaterial();
        static extern internal Material GetDefaultLineMaterial();

        extern public Shader shader { get; set; }

        static readonly int k_ColorId = Shader.PropertyToID("_Color");
        static readonly int k_MainTexId = Shader.PropertyToID("_MainTex");

        public Color color
        {
            get
            {
                // Try to find property with [MainColor] attribute and use that, otherwise fallback to old hardcoded one.
                int nameId = GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags.MainColor);
                if (nameId >= 0)
                    return GetColor(nameId);
                else
                    return GetColor(k_ColorId);
            }
            set
            {
                int nameId = GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags.MainColor);
                if (nameId >= 0)
                    SetColor(nameId, value);
                else
                    SetColor(k_ColorId, value);
            }
        }
        public Texture mainTexture
        {
            get
            {
                // Try to find property with [MainTexture] attribute and use that, otherwise fallback to old hardcoded one.
                int nameId = GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags.MainTexture);
                if (nameId >= 0)
                    return GetTexture(nameId);
                else
                    return GetTexture(k_MainTexId);
            }
            set
            {
                int nameId = GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags.MainTexture);
                if (nameId >= 0)
                    SetTexture(nameId, value);
                else
                    SetTexture(k_MainTexId, value);
            }
        }
        public Vector2 mainTextureOffset
        {
            get
            {
                int nameId = GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags.MainTexture);
                if (nameId >= 0)
                    return GetTextureOffset(nameId);
                else
                    return GetTextureOffset(k_MainTexId);
            }
            set
            {
                int nameId = GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags.MainTexture);
                if (nameId >= 0)
                    SetTextureOffset(nameId, value);
                else
                    SetTextureOffset(k_MainTexId, value);
            }
        }
        public Vector2 mainTextureScale
        {
            get
            {
                int nameId = GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags.MainTexture);
                if (nameId >= 0)
                    return GetTextureScale(nameId);
                else
                    return GetTextureScale(k_MainTexId);
            }
            set
            {
                int nameId = GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags.MainTexture);
                if (nameId >= 0)
                    SetTextureScale(nameId, value);
                else
                    SetTextureScale(k_MainTexId, value);
            }
        }
        [NativeName("GetFirstPropertyNameIdByAttributeFromScript")] extern private int GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags attributeFlag);

        [NativeName("HasPropertyFromScript")] extern public bool HasProperty(int nameID);
        public bool HasProperty(string name) { return HasProperty(Shader.PropertyToID(name)); }

        [NativeName("HasFloatFromScript")] extern private bool HasFloatImpl(int name);
        public bool HasFloat(string name) { return HasFloatImpl(Shader.PropertyToID(name)); }
        public bool HasFloat(int nameID) { return HasFloatImpl(nameID); }

        public bool HasInt(string name) { return HasFloatImpl(Shader.PropertyToID(name)); }
        public bool HasInt(int nameID) { return HasFloatImpl(nameID); }

        [NativeName("HasIntegerFromScript")] extern private bool HasIntImpl(int name);
        public bool HasInteger(string name) { return HasIntImpl(Shader.PropertyToID(name)); }
        public bool HasInteger(int nameID) { return HasIntImpl(nameID); }
        [NativeName("HasTextureFromScript")] extern private bool HasTextureImpl(int name);
        public bool HasTexture(string name) { return HasTextureImpl(Shader.PropertyToID(name)); }
        public bool HasTexture(int nameID) { return HasTextureImpl(nameID); }
        [NativeName("HasMatrixFromScript")] extern private bool HasMatrixImpl(int name);
        public bool HasMatrix(string name) { return HasMatrixImpl(Shader.PropertyToID(name)); }
        public bool HasMatrix(int nameID) { return HasMatrixImpl(nameID); }
        [NativeName("HasVectorFromScript")] extern private bool HasVectorImpl(int name);
        public bool HasVector(string name) { return HasVectorImpl(Shader.PropertyToID(name)); }
        public bool HasVector(int nameID) { return HasVectorImpl(nameID); }
        public bool HasColor(string name) { return HasVectorImpl(Shader.PropertyToID(name)); }
        public bool HasColor(int nameID) { return HasVectorImpl(nameID); }
        [NativeName("HasBufferFromScript")] extern private bool HasBufferImpl(int name);
        public bool HasBuffer(string name) { return HasBufferImpl(Shader.PropertyToID(name)); }
        public bool HasBuffer(int nameID) { return HasBufferImpl(nameID); }
        [NativeName("HasConstantBufferFromScript")] extern private bool HasConstantBufferImpl(int name);
        public bool HasConstantBuffer(string name) { return HasConstantBufferImpl(Shader.PropertyToID(name)); }
        public bool HasConstantBuffer(int nameID) { return HasConstantBufferImpl(nameID); }

        extern public int renderQueue {[NativeName("GetActualRenderQueue")] get; [NativeName("SetCustomRenderQueue")] set; }
        extern public int rawRenderQueue {[NativeName("GetCustomRenderQueue")] get; }

        extern public void EnableKeyword(string keyword);
        extern public void DisableKeyword(string keyword);
        extern public bool IsKeywordEnabled(string keyword);

        [FreeFunction("MaterialScripting::EnableKeyword", HasExplicitThis = true)] extern private void EnableLocalKeyword(LocalKeyword keyword);
        [FreeFunction("MaterialScripting::DisableKeyword", HasExplicitThis = true)] extern private void DisableLocalKeyword(LocalKeyword keyword);
        [FreeFunction("MaterialScripting::SetKeyword", HasExplicitThis = true)] extern private void SetLocalKeyword(LocalKeyword keyword, bool value);
        [FreeFunction("MaterialScripting::IsKeywordEnabled", HasExplicitThis = true)] extern private bool IsLocalKeywordEnabled(LocalKeyword keyword);

        public void EnableKeyword(in LocalKeyword keyword) { EnableLocalKeyword(keyword); }
        public void DisableKeyword(in LocalKeyword keyword) { DisableLocalKeyword(keyword); }
        public void SetKeyword(in LocalKeyword keyword, bool value) { SetLocalKeyword(keyword, value); }
        public bool IsKeywordEnabled(in LocalKeyword keyword) { return IsLocalKeywordEnabled(keyword); }

        [FreeFunction("MaterialScripting::GetEnabledKeywords", HasExplicitThis = true)] extern private LocalKeyword[] GetEnabledKeywords();
        [FreeFunction("MaterialScripting::SetEnabledKeywords", HasExplicitThis = true)] extern private void SetEnabledKeywords(LocalKeyword[] keywords);
        public LocalKeyword[] enabledKeywords { get { return GetEnabledKeywords(); } set { SetEnabledKeywords(value); } }

        extern public MaterialGlobalIlluminationFlags globalIlluminationFlags { get; set; }
        extern public bool doubleSidedGI { get; set; }
        [NativeProperty("EnableInstancingVariants")] extern public bool enableInstancing { get; set; }

        extern public int passCount { [NativeName("GetShader()->GetPassCount")] get; }
        [FreeFunction("MaterialScripting::SetShaderPassEnabled", HasExplicitThis = true)] extern public void SetShaderPassEnabled(string passName, bool enabled);
        [FreeFunction("MaterialScripting::GetShaderPassEnabled", HasExplicitThis = true)] extern public bool GetShaderPassEnabled(string passName);
        extern public string GetPassName(int pass);
        extern public int FindPass(string passName);

        extern public void SetOverrideTag(string tag, string val);
        [NativeName("GetTag")] extern private string GetTagImpl(string tag, bool currentSubShaderOnly, string defaultValue);
        public string GetTag(string tag, bool searchFallbacks, string defaultValue) { return GetTagImpl(tag, !searchFallbacks, defaultValue); }
        public string GetTag(string tag, bool searchFallbacks) { return GetTagImpl(tag, !searchFallbacks, ""); }

        [NativeThrows]
        [FreeFunction("MaterialScripting::Lerp", HasExplicitThis = true)] extern public void Lerp(Material start, Material end, float t);
        [FreeFunction("MaterialScripting::SetPass", HasExplicitThis = true)] extern public bool SetPass(int pass);
        [FreeFunction("MaterialScripting::CopyPropertiesFrom", HasExplicitThis = true)] extern public void CopyPropertiesFromMaterial(Material mat);
        [FreeFunction("MaterialScripting::CopyMatchingPropertiesFrom", HasExplicitThis = true)] extern public void CopyMatchingPropertiesFromMaterial(Material mat);

        [FreeFunction("MaterialScripting::GetShaderKeywords", HasExplicitThis = true)] extern private string[] GetShaderKeywords();
        [FreeFunction("MaterialScripting::SetShaderKeywords", HasExplicitThis = true)] extern private void SetShaderKeywords(string[] names);
        public string[] shaderKeywords { get { return GetShaderKeywords(); } set { SetShaderKeywords(value); } }

        [FreeFunction("MaterialScripting::GetPropertyNames", HasExplicitThis = true)]
        extern private string[] GetPropertyNamesImpl(int propertyType);

        extern public int ComputeCRC();

        [FreeFunction("MaterialScripting::GetTexturePropertyNames", HasExplicitThis = true)]
        extern public String[] GetTexturePropertyNames();

        [FreeFunction("MaterialScripting::GetTexturePropertyNameIDs", HasExplicitThis = true)]
        extern public int[] GetTexturePropertyNameIDs();

        [FreeFunction("MaterialScripting::GetTexturePropertyNamesInternal", HasExplicitThis = true)]
        extern private void GetTexturePropertyNamesInternal(object outNames);

        [FreeFunction("MaterialScripting::GetTexturePropertyNameIDsInternal", HasExplicitThis = true)]
        extern private void GetTexturePropertyNameIDsInternal(object outNames);

        public void GetTexturePropertyNames(List<string> outNames)
        {
            if (outNames == null)
            {
                throw new ArgumentNullException(nameof(outNames));
            }

            GetTexturePropertyNamesInternal(outNames);
        }

        public void GetTexturePropertyNameIDs(List<int> outNames)
        {
            if (outNames == null)
            {
                throw new ArgumentNullException(nameof(outNames));
            }

            GetTexturePropertyNameIDsInternal(outNames);
        }


        [NativeName("SetIntFromScript")]     extern private void SetIntImpl(int name, int value);
        [NativeName("SetFloatFromScript")]   extern private void SetFloatImpl(int name, float value);
        [NativeName("SetColorFromScript")]   extern private void SetColorImpl(int name, Color value);
        [NativeName("SetMatrixFromScript")]  extern private void SetMatrixImpl(int name, Matrix4x4 value);
        [NativeName("SetTextureFromScript")] extern private void SetTextureImpl(int name, Texture value);
        [NativeName("SetRenderTextureFromScript")] extern private void SetRenderTextureImpl(int name, RenderTexture value, Rendering.RenderTextureSubElement element);
        [NativeName("SetBufferFromScript")] extern private void SetBufferImpl(int name, ComputeBuffer value);
        [NativeName("SetBufferFromScript")] extern private void SetGraphicsBufferImpl(int name, GraphicsBuffer value);
        [NativeName("SetConstantBufferFromScript")] extern private void SetConstantBufferImpl(int name, ComputeBuffer value, int offset, int size);
        [NativeName("SetConstantBufferFromScript")] extern private void SetConstantGraphicsBufferImpl(int name, GraphicsBuffer value, int offset, int size);

        [NativeName("GetIntFromScript")]     extern private int       GetIntImpl(int name);
        [NativeName("GetFloatFromScript")]   extern private float     GetFloatImpl(int name);
        [NativeName("GetColorFromScript")]   extern private Color     GetColorImpl(int name);
        [NativeName("GetMatrixFromScript")]  extern private Matrix4x4 GetMatrixImpl(int name);
        [NativeName("GetTextureFromScript")] extern private Texture   GetTextureImpl(int name);
        [NativeName("GetBufferFromScript")] extern private GraphicsBufferHandle GetBufferImpl(int name);
        [NativeName("GetConstantBufferFromScript")] extern private GraphicsBufferHandle GetConstantBufferImpl(int name);

        [FreeFunction(Name = "MaterialScripting::SetFloatArray", HasExplicitThis = true)]  extern private void SetFloatArrayImpl(int name, float[] values, int count);
        [FreeFunction(Name = "MaterialScripting::SetVectorArray", HasExplicitThis = true)] extern private void SetVectorArrayImpl(int name, Vector4[] values, int count);
        [FreeFunction(Name = "MaterialScripting::SetColorArray", HasExplicitThis = true)]  extern private void SetColorArrayImpl(int name, Color[] values, int count);
        [FreeFunction(Name = "MaterialScripting::SetMatrixArray", HasExplicitThis = true)] extern private void SetMatrixArrayImpl(int name, Matrix4x4[] values, int count);

        [FreeFunction(Name = "MaterialScripting::GetFloatArray", HasExplicitThis = true)]  extern private float[]     GetFloatArrayImpl(int name);
        [FreeFunction(Name = "MaterialScripting::GetVectorArray", HasExplicitThis = true)] extern private Vector4[]   GetVectorArrayImpl(int name);
        [FreeFunction(Name = "MaterialScripting::GetColorArray", HasExplicitThis = true)]  extern private Color[]     GetColorArrayImpl(int name);
        [FreeFunction(Name = "MaterialScripting::GetMatrixArray", HasExplicitThis = true)] extern private Matrix4x4[] GetMatrixArrayImpl(int name);

        [FreeFunction(Name = "MaterialScripting::GetFloatArrayCount", HasExplicitThis = true)]  extern private int GetFloatArrayCountImpl(int name);
        [FreeFunction(Name = "MaterialScripting::GetVectorArrayCount", HasExplicitThis = true)] extern private int GetVectorArrayCountImpl(int name);
        [FreeFunction(Name = "MaterialScripting::GetColorArrayCount", HasExplicitThis = true)]  extern private int GetColorArrayCountImpl(int name);
        [FreeFunction(Name = "MaterialScripting::GetMatrixArrayCount", HasExplicitThis = true)] extern private int GetMatrixArrayCountImpl(int name);

        [FreeFunction(Name = "MaterialScripting::ExtractFloatArray", HasExplicitThis = true)]  extern private void ExtractFloatArrayImpl(int name, [Out] float[] val);
        [FreeFunction(Name = "MaterialScripting::ExtractVectorArray", HasExplicitThis = true)] extern private void ExtractVectorArrayImpl(int name, [Out] Vector4[] val);
        [FreeFunction(Name = "MaterialScripting::ExtractColorArray", HasExplicitThis = true)]  extern private void ExtractColorArrayImpl(int name, [Out] Color[] val);
        [FreeFunction(Name = "MaterialScripting::ExtractMatrixArray", HasExplicitThis = true)] extern private void ExtractMatrixArrayImpl(int name, [Out] Matrix4x4[] val);

        [NativeName("GetTextureScaleAndOffsetFromScript")] extern private Vector4 GetTextureScaleAndOffsetImpl(int name);
        [NativeName("SetTextureOffsetFromScript")] extern private void SetTextureOffsetImpl(int name, Vector2 offset);
        [NativeName("SetTextureScaleFromScript")]  extern private void SetTextureScaleImpl(int name, Vector2 scale);

        extern public Material parent { get; set; }
        extern public bool isVariant { [NativeName("IsVariant")] get; }

        extern internal int overrideCount { get; }
        extern internal int lockCount { get; }
        extern internal bool allowLocking { get; set; }

        [NativeName("IsChildOf")]          extern public bool IsChildOf([NotNull] Material ancestor);
        [NativeName("RevertAllOverrides")] extern public void RevertAllPropertyOverrides();

        public bool IsPropertyOverriden(int nameID)
        {
            GetPropertyState(nameID, out bool isOverriden, out _, out _);
            return isOverriden;
        }
        public bool IsPropertyLocked(int nameID)
        {
            GetPropertyState(nameID, out _, out bool isLockedInChildren, out _);
            return isLockedInChildren;
        }
        public bool IsPropertyLockedByAncestor(int nameID)
        {
            GetPropertyState(nameID, out _, out _, out bool isLockedByAncestor);
            return isLockedByAncestor;
        }

        public bool IsPropertyOverriden(string name)        => IsPropertyOverriden(Shader.PropertyToID(name));
        public bool IsPropertyLocked(string name)           => IsPropertyLocked(Shader.PropertyToID(name));
        public bool IsPropertyLockedByAncestor(string name) => IsPropertyLockedByAncestor(Shader.PropertyToID(name));

        // For MaterialProperty
        [NativeName("SetPropertyLock")]  extern public void SetPropertyLock(int nameID, bool value);
        [NativeName("ApplyOverride")]    extern public void ApplyPropertyOverride([NotNull] Material destination, int nameID, bool recordUndo = true);
        [NativeName("RevertOverride")]   extern public void RevertPropertyOverride(int nameID);
        [NativeName("GetPropertyState")] extern internal void GetPropertyState(int nameID, out bool isOverriden, out bool isLockedInChildren, out bool isLockedByAncestor);

        public void SetPropertyLock(string name, bool value)                                         => SetPropertyLock(Shader.PropertyToID(name), value);
        public void ApplyPropertyOverride(Material destination, string name, bool recordUndo = true) => ApplyPropertyOverride(destination, Shader.PropertyToID(name), recordUndo);
        public void RevertPropertyOverride(string name)                                              => RevertPropertyOverride(Shader.PropertyToID(name));

        // For MaterialSerializedProperty - bindings don't support overloads, so rename and use intermediate functions
        [NativeName("SetPropertyLock")]  extern private void SetPropertyLock_Serialized(MaterialSerializedProperty property, bool value);
        [NativeName("ApplyOverride")]    extern private void ApplyPropertyOverride_Serialized([NotNull] Material destination, MaterialSerializedProperty property, bool recordUndo = true);
        [NativeName("RevertOverride")]   extern private void RevertPropertyOverride_Serialized(MaterialSerializedProperty property);
        [NativeName("GetPropertyState")] extern private void GetPropertyState_Serialized(MaterialSerializedProperty property, out bool isOverriden, out bool isLockedInChildren, out bool isLockedByAncestor);

        internal void SetPropertyLock(MaterialSerializedProperty property, bool value) => SetPropertyLock_Serialized(property, value);
        internal void ApplyPropertyOverride(Material destination, MaterialSerializedProperty property, bool recordUndo = true) => ApplyPropertyOverride_Serialized(destination, property, recordUndo);
        internal void RevertPropertyOverride(MaterialSerializedProperty property) => RevertPropertyOverride_Serialized(property);
        internal void GetPropertyState(MaterialSerializedProperty propertyName, out bool isOverriden, out bool isLockedInChildren, out bool isLockedByAncestor)
            => GetPropertyState_Serialized(propertyName, out isOverriden, out isLockedInChildren, out isLockedByAncestor);

        // Clear stale references
        [NativeName("RemoveUnusedProperties")] extern internal void RemoveUnusedProperties();
        extern internal void MarkChildrenNeedValidation(string changedProperty);
    }
}
