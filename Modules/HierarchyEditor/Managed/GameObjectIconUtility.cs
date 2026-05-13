// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Hierarchy.Editor
{
    /// <summary>
    /// Gameobject icon utility class.
    /// </summary>
    static class GameObjectIconUtility
    {
        static readonly List<Component> s_ComponentBuffer = new List<Component>(16);

        // Maps legacy sv_icon names to their Hv2 resource paths.
        // Dots 0–7 are circle variants; dots 8–15 are diamond variants.
        static readonly Dictionary<string, string> s_Hv2GizmoIconPaths = new()
        {
            { "sv_icon_dot0_pix16_gizmo",  "Hv2/Circles/CircleGray"      },
            { "sv_icon_dot1_pix16_gizmo",  "Hv2/Circles/CircleBlue"      },
            { "sv_icon_dot2_pix16_gizmo",  "Hv2/Circles/CircleTeal"      },
            { "sv_icon_dot3_pix16_gizmo",  "Hv2/Circles/CircleGreen"     },
            { "sv_icon_dot4_pix16_gizmo",  "Hv2/Circles/CircleYellow"    },
            { "sv_icon_dot5_pix16_gizmo",  "Hv2/Circles/CircleOrange"    },
            { "sv_icon_dot6_pix16_gizmo",  "Hv2/Circles/CircleRed"       },
            { "sv_icon_dot7_pix16_gizmo",  "Hv2/Circles/CirclePurple"    },
            { "sv_icon_dot8_pix16_gizmo",  "Hv2/Diamonds/DiamondGray"    },
            { "sv_icon_dot9_pix16_gizmo",  "Hv2/Diamonds/DiamondBlue"    },
            { "sv_icon_dot10_pix16_gizmo", "Hv2/Diamonds/DiamondTeal"    },
            { "sv_icon_dot11_pix16_gizmo", "Hv2/Diamonds/DiamondGreen"   },
            { "sv_icon_dot12_pix16_gizmo", "Hv2/Diamonds/DiamondYellow"  },
            { "sv_icon_dot13_pix16_gizmo", "Hv2/Diamonds/DiamondOrange"  },
            { "sv_icon_dot14_pix16_gizmo", "Hv2/Diamonds/DiamondRed"     },
            { "sv_icon_dot15_pix16_gizmo", "Hv2/Diamonds/DiamondPurple"  },
            { "sv_label_0", "Hv2/Pills/PillGray"   },
            { "sv_label_1", "Hv2/Pills/PillBlue"   },
            { "sv_label_2", "Hv2/Pills/PillTeal"   },
            { "sv_label_3", "Hv2/Pills/PillGreen"  },
            { "sv_label_4", "Hv2/Pills/PillYellow" },
            { "sv_label_5", "Hv2/Pills/PillOrange" },
            { "sv_label_6", "Hv2/Pills/PillRed"    },
            { "sv_label_7", "Hv2/Pills/PillPurple" },
        };

        // Textures resolved from s_Hv2GizmoIconPaths on first use and cached here.
        static readonly Dictionary<string, Texture2D> s_Hv2GizmoIconCache = new(s_Hv2GizmoIconPaths.Count);

        static Texture2D GetHv2GizmoIcon(string iconName)
        {
            if (!s_Hv2GizmoIconCache.TryGetValue(iconName, out var texture))
            {
                s_Hv2GizmoIconPaths.TryGetValue(iconName, out var path);
                texture = path != null ? EditorGUIUtility.LoadIcon(path) : null;
                s_Hv2GizmoIconCache[iconName] = texture;
            }
            return texture;
        }

        static bool ShouldShowUserDefinedIcons => HierarchyPreferences.GameObjectIconMode == HierarchyPreferences.IconMode.ComponentsAndGizmos;

        static bool ShouldShowComponentIcons => HierarchyPreferences.GameObjectIconMode != HierarchyPreferences.IconMode.GameObjectOnly;

        public static void SetNodeIconForObject(HierarchyViewItem item, GameObject gameObject)
        {
            item.Icon.style.backgroundImage = StyleKeyword.Null;

            var isPrefabRoot = PrefabUtility.IsAnyPrefabInstanceRoot(gameObject);

            // Skip SetNodePrefabGenericStyle and SetNodePrefabRootStyle for broken prefabs
            if (isPrefabRoot && (PrefabUtility.GetPrefabAssetType(gameObject) == PrefabAssetType.MissingAsset || PrefabUtility.GetPrefabInstanceStatus(gameObject) != PrefabInstanceStatus.Connected))
            {
                HierarchyViewPrefabStyleUtility.SetBrokenPrefabStyle(item);
                return;
            }

            HierarchyViewPrefabStyleUtility.SetNodePrefabGenericStyle(gameObject, item);

            // User Defined
            var gameObjectIcon = ShouldShowUserDefinedIcons ? EditorGUIUtility.GetIconForObject(gameObject) : null;
            if (gameObjectIcon != null)
            {
                item.Icon.style.backgroundImage = GetHv2GizmoIcon(gameObjectIcon.name) ?? gameObjectIcon;
                HierarchyViewPrefabStyleUtility.ClearPrefabRootStyle(item);
            }

            // Prefab/Asset Root
            else if (isPrefabRoot)
            {
                HierarchyViewPrefabStyleUtility.SetNodePrefabRootStyle(gameObject, item);
            }

            // Component Icon
            else
            {
                HierarchyViewPrefabStyleUtility.ClearPrefabRootStyle(item);

                if (ShouldShowComponentIcons)
                {
                    gameObject.GetComponents(s_ComponentBuffer);

                    // Use topmost component, if none use transform
                    var icon = AssetPreview.GetMiniThumbnail(s_ComponentBuffer.Count > 1 ? s_ComponentBuffer[1] : s_ComponentBuffer[0]);
                    if (icon != null)
                        item.Icon.style.backgroundImage = GetHv2GizmoIcon(icon.name) ?? icon;
                }
            }
        }
    }
}
