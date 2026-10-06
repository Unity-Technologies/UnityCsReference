// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [global::UnityEngine.NativeClass("ShaderImporter", PersistentTypeId = 1007)]
    [NativeHeader("Editor/Src/AssetPipeline/ShaderImporter.h")]
    public sealed partial class ShaderImporter : AssetImporter
    {
        internal ShaderImporter(global::UnityEngine.EntityId id) : base(id) {}
        public ShaderImporter() {}
        public extern Shader GetShader();

        public extern void SetDefaultTextures(string[] name, Texture[] textures);

        public extern Texture GetDefaultTexture(string name);

        public extern void SetNonModifiableTextures(string[] name, Texture[] textures);

        public extern Texture GetNonModifiableTexture(string name);

        public PreprocessorOverride preprocessorOverride { get { return PreprocessorOverride.UseProjectSettings; } set {} }
    }
}
