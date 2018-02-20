// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEditor.Rendering
{
    [NativeHeader("Editor/Mono/EditorGraphicsSettings.bindings.h")]
    [StaticAccessor("EditorGraphicsSettingsScripting", StaticAccessorType.DoubleColon)]
    public sealed partial class EditorGraphicsSettings
    {
        [NativeName("SetTierSettings")] extern internal static void SetTierSettingsImpl(BuildTargetGroup target, GraphicsTier tier, TierSettings settings);

        extern public   static TierSettings GetTierSettings(BuildTargetGroup target, GraphicsTier tier);
        extern internal static TierSettings GetCurrentTierSettings();

        extern internal static bool AreTierSettingsAutomatic(BuildTargetGroup target, GraphicsTier tier);
        extern internal static void MakeTierSettingsAutomatic(BuildTargetGroup target, GraphicsTier tier, bool automatic);

        extern internal static void OnUpdateTierSettings(BuildTargetGroup target, bool shouldReloadShaders);

        // we give access to shader settings from both UI and script, and usually script access do not touch Undo system by itself
        // hence we provide small helper for our UI to register Undo changes when needed
        extern internal static void RegisterUndo();

        extern private static AlbedoSwatchInfo[] GetAlbedoSwatches();
        extern private static void               SetAlbedoSwatches(AlbedoSwatchInfo[] swatches);

        public static AlbedoSwatchInfo[] albedoSwatches
        {
            get { return GetAlbedoSwatches(); }
            set { SetAlbedoSwatches(value); }
        }
    }
}
