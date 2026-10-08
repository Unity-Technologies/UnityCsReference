// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
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

    static partial class RemoveLegacyUmbraMenuItems
    {
        [AutoStaticsCleanupOnCodeReload]
        static bool s_RemovalScheduled;

        [OnCodeLoaded]
        static void Initialize()
        {
            if (EditorSettings.enableLegacyUmbraCulling)
                return;

            Menu.menuChanged += ScheduleRemoval;
            ScheduleRemoval();
        }

        [OnCodeUnloading]
        static void Teardown()
        {
            Menu.menuChanged -= ScheduleRemoval;
        }

        static void ScheduleRemoval()
        {
            if (s_RemovalScheduled)
                return;

            s_RemovalScheduled = true;
            EditorApplication.delayCall += RemoveMenuItems;
        }

        static readonly string[] k_MenuItems =
        {
            "Component/Rendering/Occlusion Area",
            "Component/Rendering/Occlusion Portal",
            "Window/Rendering/Occlusion Culling",
        };

        static void RemoveMenuItems()
        {
            try
            {
                EditorUtility.Internal_UpdateAllMenus();
                foreach (var menuItem in k_MenuItems)
                {
                    if (Menu.MenuItemExists(menuItem))
                        Menu.RemoveMenuItem(menuItem);
                }
            }
            finally
            {
                s_RemovalScheduled = false;
            }
        }
    }
}
