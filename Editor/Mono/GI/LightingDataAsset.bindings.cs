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
    [ExcludeFromPreset]
    public sealed partial class LightingDataAsset : Object
    {
        private LightingDataAsset() {}

        public LightingDataAsset(Scene scene)
        {
            Internal_Create(this, scene);
        }

        [NativeThrows]
        private extern static void Internal_Create([Writable] LightingDataAsset self, Scene scene);

        public extern void SetLights(Light[] lights);

        public extern SphericalHarmonicsL2 GetAmbientProbe();
        public extern void SetAmbientProbe(SphericalHarmonicsL2 probe);
        public extern Texture GetDefaultReflectionCubemap();
        public extern void SetDefaultReflectionCubemap(Texture cubemap);

        internal extern bool isValid {[NativeName("IsValid")] get; }

        internal extern string validityErrorMessage { get; }
    }
}
