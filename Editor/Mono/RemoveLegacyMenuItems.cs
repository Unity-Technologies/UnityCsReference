// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: SRPSettings not yet converted
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
    [InitializeOnLoad]
    static class RemoveLegacyRPMenuItems
    {
        static RemoveLegacyRPMenuItems()
        {
            EditorApplication.delayCall += RemoveBIRPMenuItems;
        }

        static void RemoveBIRPMenuItems()
        {
            if (QualitySettings.renderPipeline == null && GraphicsSettings.defaultRenderPipeline == null) return;

            Menu.RemoveMenuItem("Assets/Create/Shader/Standard Surface Shader");
            Menu.RemoveMenuItem("Assets/Create/Shader/Image Effect Shader");
            Menu.RemoveMenuItem("Assets/Create/Shader/Unlit Shader");

            Menu.RemoveMenuItem("Assets/Create/Shader Graph/BuiltIn/Lit Shader Graph");
            Menu.RemoveMenuItem("Assets/Create/Shader Graph/BuiltIn/Unlit Shader Graph");
            Menu.RemoveMenuItem("Assets/Create/Shader Graph/BuiltIn/Canvas Shader Graph");

            Menu.RemoveMenuItem("Assets/Create/Rendering/Lens Flare");

            Menu.RemoveMenuItem("Component/Effects/Projector");
            Menu.RemoveMenuItem("Component/Effects/Halo");
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
