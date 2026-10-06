// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/GI/Enlighten/LightingDataAsset.h")]
    [global::UnityEngine.NativeClass("LightingDataAsset", PersistentTypeId = 1120)]
    [ExcludeFromPreset]
    public sealed partial class LightingDataAsset : Object
    {
        internal LightingDataAsset(global::UnityEngine.EntityId id) : base(id) {}
        private LightingDataAsset() {}

        public LightingDataAsset(Scene scene)
        {
            SetEntityIdFromConstructor(Internal_Create(scene));
        }

        [NativeMethod(ThrowsException = true)]
        private extern static EntityId Internal_Create(Scene scene);

        public extern void SetLights(Light[] lights);

        public extern SphericalHarmonicsL2 GetAmbientProbe();
        public extern void SetAmbientProbe(SphericalHarmonicsL2 probe);
        public extern Texture GetDefaultReflectionCubemap();
        public extern void SetDefaultReflectionCubemap(Texture cubemap);

        internal extern bool isValid {[NativeName("IsValid")] get; }

        internal extern string validityErrorMessage { get; }
    }
}
