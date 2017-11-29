// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Linq;

namespace UnityEditor
{
    internal class LightTableColumns
    {
        static class Styles
        {
            public static readonly GUIContent[] ProjectionStrings = { EditorGUIUtility.TextContent("Infinite"), EditorGUIUtility.TrTextContent("Box") };
            public static readonly GUIContent[] LightmapEmissiveStrings = { EditorGUIUtility.TextContent("Realtime"), EditorGUIUtility.TrTextContent("Baked") };
            public static readonly GUIContent Name = EditorGUIUtility.TrTextContent("Name");
            public static readonly GUIContent On = EditorGUIUtility.TrTextContent("On");
            public static readonly GUIContent Type = EditorGUIUtility.TrTextContent("Type");
            public static readonly GUIContent Mode = EditorGUIUtility.TrTextContent("Mode");
            public static readonly GUIContent Color = EditorGUIUtility.TrTextContent("Color");
            public static readonly GUIContent Intensity = EditorGUIUtility.TrTextContent("Intensity");
            public static readonly GUIContent IndirectMultiplier = EditorGUIUtility.TrTextContent("Indirect Multiplier");
            public static readonly GUIContent ShadowType = EditorGUIUtility.TrTextContent("Shadow Type");
            public static readonly GUIContent Projection = EditorGUIUtility.TrTextContent("Projection");
            public static readonly GUIContent HDR = EditorGUIUtility.TrTextContent("HDR");
            public static readonly GUIContent ShadowDistance = EditorGUIUtility.TrTextContent("Shadow Distance");
            public static readonly GUIContent NearPlane = EditorGUIUtility.TrTextContent("Near Plane");
            public static readonly GUIContent FarPlane = EditorGUIUtility.TrTextContent("Far Plane");
            public static readonly GUIContent GlobalIllumination = EditorGUIUtility.TrTextContent("Global Illumination");
            public static readonly GUIContent SelectObjects = EditorGUIUtility.TextContent("");
            public static readonly GUIContent SelectObjectsButton = EditorGUIUtility.TrTextContentWithIcon("", "Find References in Scene", "UnityEditor.FindDependencies");

            public static readonly GUIContent[] LightmapBakeTypeTitles = { EditorGUIUtility.TrTextContent("Realtime"), EditorGUIUtility.TrTextContent("Mixed"), EditorGUIUtility.TrTextContent("Baked") };
            public static readonly int[] LightmapBakeTypeValues = { (int)LightmapBakeType.Realtime, (int)LightmapBakeType.Mixed, (int)LightmapBakeType.Baked };
        }

        private static SerializedPropertyTreeView.Column[] FinalizeColumns(SerializedPropertyTreeView.Column[] columns, out string[] propNames)
        {
            propNames = new string[columns.Length];

            for (int i = 0; i < columns.Length; i++)
                propNames[i] = columns[i].propertyName;

            return columns;
        }

        private static bool IsEditable(Object target)
        {
            return ((target.hideFlags & HideFlags.NotEditable) == 0);
        }

        public static SerializedPropertyTreeView.Column[] CreateLightColumns(out string[] propNames)
        {
            var columns = new[]
            {
                new SerializedPropertyTreeView.Column // 0: Name
                {
                    headerContent           = Styles.Name,
                    headerTextAlignment     = TextAlignment.Left,
                    sortedAscending         = true,
                    sortingArrowAlignment   = TextAlignment.Center,
                    width                   = 200,
                    minWidth                = 100,
                    autoResize              = false,
                    allowToggleVisibility   = true,
                    propertyName            = null,
                    dependencyIndices       = null,
                    compareDelegate         = SerializedPropertyTreeView.DefaultDelegates.s_CompareName,
                    drawDelegate            = SerializedPropertyTreeView.DefaultDelegates.s_DrawName,
                    filter                  = new SerializedPropertyFilters.Name()
                },
                new SerializedPropertyTreeView.Column // 1: Enabled
                {
                    headerContent           = Styles.On,
                    headerTextAlignment     = TextAlignment.Center,
                    sortedAscending         = true,
                    sortingArrowAlignment   = TextAlignment.Center,
                    width                   = 25,
                    minWidth                = 25,
                    maxWidth                = 25,
                    autoResize              = false,
                    allowToggleVisibility   = true,
                    propertyName            = "m_Enabled",
                    dependencyIndices       = null,
                    compareDelegate         = SerializedPropertyTreeView.DefaultDelegates.s_CompareCheckbox,
                    drawDelegate            = SerializedPropertyTreeView.DefaultDelegates.s_DrawCheckbox
                },
                new SerializedPropertyTreeView.Column // 2: Type
                {
                    headerContent           = Styles.Type,
                    headerTextAlignment     = TextAlignment.Left,
                    sortedAscending         = true,
                    sortingArrowAlignment   = TextAlignment.Center,
                    width                   = 120,
                    minWidth                = 60,
                    autoResize              = false,
                    allowToggleVisibility   = true,
                    propertyName            = "m_Type",
                    dependencyIndices       = null,
                    compareDelegate         = SerializedPropertyTreeView.DefaultDelegates.s_CompareEnum,
                    drawDelegate            = SerializedPropertyTreeView.DefaultDelegates.s_DrawDefault
                },
                new SerializedPropertyTreeView.Column // 3: Mode
                {
                    headerContent           = Styles.Mode,
                    headerTextAlignment     = TextAlignment.Left,
                    sortedAscending         = true,
                    sortingArrowAlignment   = TextAlignment.Center,
                    width                   = 70,
                    minWidth                = 40,
                    maxWidth                = 70,
                    autoResize              = false,
                    allowToggleVisibility   = true,
                    propertyName            = "m_Lightmapping",
                    dependencyIndices       = new int[] { 2 },
                    compareDelegate         = SerializedPropertyTreeView.DefaultDelegates.s_CompareEnum,
                    drawDelegate            = (Rect r, SerializedProperty prop, SerializedProperty[] dep) =>
                        {
                            bool areaLight = dep.Length > 1 && dep[0].enumValueIndex == (int)LightType.Area;

                            using (new EditorGUI.DisabledScope(areaLight))
                            {
                                EditorGUI.BeginChangeCheck();
                                int newval = EditorGUI.IntPopup(r, prop.intValue, Styles.LightmapBakeTypeTitles, Styles.LightmapBakeTypeValues);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    prop.intValue = newval;
                                }
                            }
                        }
                },
                new SerializedPropertyTreeView.Column // 4: Color
                {
                    headerContent           = Styles.Color,
                    headerTextAlignment     = TextAlignment.Left,
                    sortedAscending         = true,
                    sortingArrowAlignment   = TextAlignment.Center,
                    width                   = 70,
                    minWidth                = 40,
                    autoResize              = false,
                    allowToggleVisibility   = true,
                    propertyName            = "m_Color",
                    dependencyIndices       = null,
                    compareDelegate         = SerializedPropertyTreeView.DefaultDelegates.s_CompareColor,
                    drawDelegate            = SerializedPropertyTreeView.DefaultDelegates.s_DrawDefault
                },
                new SerializedPropertyTreeView.Column // 5: Intensity
                {
                    headerContent           = Styles.Intensity,
                    headerTextAlignment     = TextAlignment.Left,
                    sortedAscending         = true,
                    sortingArrowAlignment   = TextAlignment.Center,
                    width                   = 60,
                    minWidth                = 30,
                    autoResize              = false,
                    allowToggleVisibility   = true,
                    propertyName            = "m_Intensity",
                    dependencyIndices       = null,
                    compareDelegate         = SerializedPropertyTreeView.DefaultDelegates.s_CompareFloat,
                    drawDelegate            = SerializedPropertyTreeView.DefaultDelegates.s_DrawDefault
                },
                new SerializedPropertyTreeView.Column     // 6: Indirect Multiplier
                {
                    headerContent           = Styles.IndirectMultiplier,
                    headerTextAlignment     = TextAlignment.Left,
                    sortedAscending         = true,
                    sortingArrowAlignment   = TextAlignment.Center,
                    width                   = 110,
                    minWidth                = 60,
                    autoResize              = false,
                    allowToggleVisibility   = true,
                    propertyName            = "m_BounceIntensity",
                    dependencyIndices       = null,
                    compareDelegate         = SerializedPropertyTreeView.DefaultDelegates.s_CompareFloat,
                    drawDelegate            = SerializedPropertyTreeView.DefaultDelegates.s_DrawDefault
                },
                new SerializedPropertyTreeView.Column // 7: Shadow Type
                {
                    headerContent           = Styles.ShadowType,
                    headerTextAlignment     = TextAlignment.Left,
                    sortedAscending         = true,
                    sortingArrowAlignment   = TextAlignment.Center,
                    width                   = 100,
                    minWidth                = 60,
                    autoResize              = false,
                    allowToggleVisibility   = true,
                    propertyName            = "m_Shadows.m_Type",
                    dependencyIndices       = null,
                    compareDelegate         = SerializedPropertyTreeView.DefaultDelegates.s_CompareEnum,
                    drawDelegate            = SerializedPropertyTreeView.DefaultDelegates.s_DrawDefault
                },
            };

            return FinalizeColumns(columns, out propNames);
        }

        public static SerializedPropertyTreeView.Column[] CreateReflectionColumns(out string[] propNames)
        {
            var columns = new[]
            {
                new SerializedPropertyTreeView.Column // 0: Name
                {
                    headerContent           = Styles.Name,
                    headerTextAlignment     = TextAlignment.Left,
                    sortedAscending         = true,
                    sortingArrowAlignment   = TextAlignment.Center,
                    width                   = 200,
                    minWidth                = 100,
                    autoResize              = false,
                    allowToggleVisibility   = true,
                    propertyName            = null,
                    dependencyIndices       = null,
                    compareDelegate         = SerializedPropertyTreeView.DefaultDelegates.s_CompareName,
                    drawDelegate            = SerializedPropertyTreeView.DefaultDelegates.s_DrawName,
                    filter                  = new SerializedPropertyFilters.Name()
                },
                new SerializedPropertyTreeView.Column // 1: Enabled
                {
                    headerContent           = Styles.On,
                    headerTextAlignment     = TextAlignment.Center,
                    sortedAscending         = true,
                    sortingArrowAlignment   = TextAlignment.Center,
                    width                   = 25,
                    minWidth                = 25,
                    maxWidth                = 25,
                    autoResize              = false,
                    allowToggleVisibility   = true,
                    propertyName            = "m_Enabled",
                    dependencyIndices       = null,
                    compareDelegate         = SerializedPropertyTreeView.DefaultDelegates.s_CompareCheckbox,
                    drawDelegate            = SerializedPropertyTreeView.DefaultDelegates.s_DrawCheckbox
                },
                new SerializedPropertyTreeView.Column // 2: Mode
                {
                    headerContent           = Styles.Mode,
                    headerTextAlignment     = TextAlignment.Left,
                    sortedAscending         = true,
                    sortingArrowAlignment   = TextAlignment.Center,
                    width                   = 70,
                    minWidth                = 40,
                    autoResize              = false,
                    allowToggleVisibility   = true,
                    propertyName            = "m_Mode",
                    dependencyIndices       = null,
                    compareDelegate         = SerializedPropertyTreeView.DefaultDelegates.s_CompareInt,
                    drawDelegate            = (Rect r, SerializedProperty prop, SerializedProperty[] dep) =>
                        {
                            EditorGUI.IntPopup(r, prop, ReflectionProbeEditor.Styles.reflectionProbeMode, ReflectionProbeEditor.Styles.reflectionProbeModeValues, GUIContent.none);
                        }
                },
                new SerializedPropertyTreeView.Column // 3: Projection
                {
                    headerContent           = Styles.Projection,
                    headerTextAlignment     = TextAlignment.Left,
                    sortedAscending         = true,
                    sortingArrowAlignment   = TextAlignment.Center,
                    width                   = 80,
                    minWidth                = 40,
                    autoResize              = false,
                    allowToggleVisibility   = true,
                    propertyName            = "m_BoxProjection",
                    dependencyIndices       = null,
                    compareDelegate         = SerializedPropertyTreeView.DefaultDelegates.s_CompareCheckbox,
                    drawDelegate            = (Rect r, SerializedProperty prop, SerializedProperty[] dep) =>
                        {
                            int[] opts = { 0, 1 };
                            prop.boolValue = EditorGUI.IntPopup(r, prop.boolValue ? 1 : 0, Styles.ProjectionStrings, opts) == 1;
                        }
                },
                new SerializedPropertyTreeView.Column // 4: HDR
                {
                    headerContent           = Styles.HDR,
                    headerTextAlignment     = TextAlignment.Center,
                    sortedAscending         = true,
                    sortingArrowAlignment   = TextAlignment.Center,
                    width                   = 35,
                    minWidth                = 35,
                    maxWidth                = 35,
                    autoResize              = false,
                    allowToggleVisibility   = true,
                    propertyName            = "m_HDR",
                    dependencyIndices       = null,
                    compareDelegate         = SerializedPropertyTreeView.DefaultDelegates.s_CompareCheckbox,
                    drawDelegate            = SerializedPropertyTreeView.DefaultDelegates.s_DrawCheckbox
                },
                new SerializedPropertyTreeView.Column // 5: Shadow Distance
                {
                    headerContent           = Styles.ShadowDistance,
                    headerTextAlignment     = TextAlignment.Left,
                    sortedAscending         = true,
                    sortingArrowAlignment   = TextAlignment.Center,
                    width                   = 110,
                    minWidth                = 50,
                    autoResize              = false,
                    allowToggleVisibility   = true,
                    propertyName            = "m_ShadowDistance",
                    dependencyIndices       = null,
                    compareDelegate         = SerializedPropertyTreeView.DefaultDelegates.s_CompareFloat,
                    drawDelegate            = SerializedPropertyTreeView.DefaultDelegates.s_DrawDefault
                },
                new SerializedPropertyTreeView.Column // 6: Near Plane
                {
                    headerContent           = Styles.NearPlane,
                    headerTextAlignment     = TextAlignment.Left,
                    sortedAscending         = true,
                    sortingArrowAlignment   = TextAlignment.Center,
                    width                   = 70,
                    minWidth                = 30,
                    autoResize              = false,
                    allowToggleVisibility   = true,
                    propertyName            = "m_NearClip",
                    dependencyIndices       = null,
                    compareDelegate         = SerializedPropertyTreeView.DefaultDelegates.s_CompareFloat,
                    drawDelegate            = SerializedPropertyTreeView.DefaultDelegates.s_DrawDefault
                },
                new SerializedPropertyTreeView.Column // 7: Far Plane
                {
                    headerContent           = Styles.FarPlane,
                    headerTextAlignment     = TextAlignment.Left,
                    sortedAscending         = true,
                    sortingArrowAlignment   = TextAlignment.Center,
                    width                   = 70,
                    minWidth                = 30,
                    autoResize              = false,
                    allowToggleVisibility   = true,
                    propertyName            = "m_FarClip",
                    dependencyIndices       = null,
                    compareDelegate         = SerializedPropertyTreeView.DefaultDelegates.s_CompareFloat,
                    drawDelegate            = SerializedPropertyTreeView.DefaultDelegates.s_DrawDefault
                },
            };

            return FinalizeColumns(columns, out propNames);
        }

        public static SerializedPropertyTreeView.Column[] CreateLightProbeColumns(out string[] propNames)
        {
            var columns = new[]
            {
                new SerializedPropertyTreeView.Column // 0: Name
                {
                    headerContent           = Styles.Name,
                    headerTextAlignment     = TextAlignment.Left,
                    sortedAscending         = true,
                    sortingArrowAlignment   = TextAlignment.Center,
                    width                   = 200,
                    minWidth                = 100,
                    autoResize              = false,
                    allowToggleVisibility   = true,
                    propertyName            = null,
                    dependencyIndices       = null,
                    compareDelegate         = SerializedPropertyTreeView.DefaultDelegates.s_CompareName,
                    drawDelegate            = SerializedPropertyTreeView.DefaultDelegates.s_DrawName,
                    filter                  = new SerializedPropertyFilters.Name()
                },
                new SerializedPropertyTreeView.Column // 1: Enabled
                {
                    headerContent           = Styles.On,
                    headerTextAlignment     = TextAlignment.Center,
                    sortedAscending         = true,
                    sortingArrowAlignment   = TextAlignment.Center,
                    width                   = 25,
                    minWidth                = 25,
                    maxWidth                = 25,
                    autoResize              = false,
                    allowToggleVisibility   = true,
                    propertyName            = "m_Enabled",
                    dependencyIndices       = null,
                    compareDelegate         = SerializedPropertyTreeView.DefaultDelegates.s_CompareCheckbox,
                    drawDelegate            = SerializedPropertyTreeView.DefaultDelegates.s_DrawCheckbox
                }
            };

            return FinalizeColumns(columns, out propNames);
        }

        public static SerializedPropertyTreeView.Column[] CreateEmissivesColumns(out string[] propNames)
        {
            var columns = new[]
            {
                new SerializedPropertyTreeView.Column // 0: Icon
                {
                    headerContent           = Styles.SelectObjects,
                    headerTextAlignment     = TextAlignment.Left,
                    sortedAscending         = true,
                    sortingArrowAlignment   = TextAlignment.Center,
                    width                   = 20,
                    minWidth                = 20,
                    maxWidth                = 20,
                    autoResize              = false,
                    allowToggleVisibility   = true,
                    propertyName            = "m_LightmapFlags",
                    dependencyIndices       = null,
                    compareDelegate         = null,
                    drawDelegate            = (Rect r, SerializedProperty prop, SerializedProperty[] dep) =>
                        {
                            if (GUI.Button(r, Styles.SelectObjectsButton, "label"))
                            {
                                SearchableEditorWindow.SearchForReferencesToInstanceID(prop.serializedObject.targetObject.GetInstanceID());
                            }
                        }
                },
                new SerializedPropertyTreeView.Column // 1: Name
                {
                    headerContent           = Styles.Name,
                    headerTextAlignment     = TextAlignment.Left,
                    sortedAscending         = true,
                    sortingArrowAlignment   = TextAlignment.Center,
                    width                   = 200,
                    minWidth                = 100,
                    autoResize              = false,
                    allowToggleVisibility   = true,
                    propertyName            = null,
                    dependencyIndices       = null,
                    compareDelegate         = SerializedPropertyTreeView.DefaultDelegates.s_CompareName,
                    drawDelegate            = SerializedPropertyTreeView.DefaultDelegates.s_DrawName,
                    filter                  = new SerializedPropertyFilters.Name()
                },
                new SerializedPropertyTreeView.Column // 2: GI
                {
                    headerContent           = Styles.GlobalIllumination,
                    headerTextAlignment     = TextAlignment.Left,
                    sortedAscending         = true,
                    sortingArrowAlignment   = TextAlignment.Center,
                    width                   = 120,
                    minWidth                = 70,
                    autoResize              = false,
                    allowToggleVisibility   = true,
                    propertyName            = "m_LightmapFlags",
                    dependencyIndices       = null,
                    compareDelegate         = SerializedPropertyTreeView.DefaultDelegates.s_CompareInt,
                    drawDelegate            = (Rect r, SerializedProperty prop, SerializedProperty[] dep) =>
                        {
                            if (!prop.serializedObject.targetObject.GetType().Equals(typeof(Material)))
                                return;

                            using (new EditorGUI.DisabledScope(!IsEditable(prop.serializedObject.targetObject)))
                            {
                                MaterialGlobalIlluminationFlags giFlags = ((prop.intValue & (int)MaterialGlobalIlluminationFlags.BakedEmissive) != 0) ? MaterialGlobalIlluminationFlags.BakedEmissive : MaterialGlobalIlluminationFlags.RealtimeEmissive;

                                int[] lightmapEmissiveValues = { (int)MaterialGlobalIlluminationFlags.RealtimeEmissive, (int)MaterialGlobalIlluminationFlags.BakedEmissive };

                                EditorGUI.BeginChangeCheck();

                                giFlags = (MaterialGlobalIlluminationFlags)EditorGUI.IntPopup(r, (int)giFlags, Styles.LightmapEmissiveStrings, lightmapEmissiveValues);

                                if (EditorGUI.EndChangeCheck())
                                {
                                    Material material = (Material)prop.serializedObject.targetObject;
                                    Undo.RecordObjects(new Material[] { material } , "Modify GI Settings of " + material.name);

                                    material.globalIlluminationFlags = giFlags;

                                    prop.serializedObject.Update();
                                }
                            }
                        }
                },
                new SerializedPropertyTreeView.Column // 3: Color
                {
                    headerContent           = Styles.Color,
                    headerTextAlignment     = TextAlignment.Left,
                    sortedAscending         = true,
                    sortingArrowAlignment   = TextAlignment.Center,
                    width                   = 70,
                    minWidth                = 40,
                    autoResize              = false,
                    allowToggleVisibility   = true,
                    propertyName            = "m_Shader",
                    dependencyIndices       = null,
                    compareDelegate         = (SerializedProperty lhs, SerializedProperty rhs) =>
                        {
                            float lh, ls, lv, rh, rs, rv;
                            Color.RGBToHSV(((Material)lhs.serializedObject.targetObject).GetColor("_EmissionColor"), out lh, out ls, out lv);
                            Color.RGBToHSV(((Material)rhs.serializedObject.targetObject).GetColor("_EmissionColor"), out rh, out rs, out rv);
                            return lv.CompareTo(rv);
                        },
                    drawDelegate            = (Rect r, SerializedProperty prop, SerializedProperty[] dep) =>
                        {
                            if (!prop.serializedObject.targetObject.GetType().Equals(typeof(Material)))
                                return;

                            using (new EditorGUI.DisabledScope(!IsEditable(prop.serializedObject.targetObject)))
                            {
                                Material material = (Material)prop.serializedObject.targetObject;

                                Color color = material.GetColor("_EmissionColor");

                                EditorGUI.BeginChangeCheck();
                                Color newValue = EditorGUI.ColorField(r, GUIContent.Temp(""), color, true, false, true);

                                if (EditorGUI.EndChangeCheck())
                                {
                                    Undo.RecordObjects(new Material[] { material }, "Modify Emission Color of " + material.name);
                                    material.SetColor("_EmissionColor", newValue);
                                }
                            }
                        },
                    copyDelegate = (SerializedProperty target, SerializedProperty source) =>
                        {
                            Material sourceMaterial = (Material)source.serializedObject.targetObject;
                            Color color = sourceMaterial.GetColor("_EmissionColor");

                            Material targetMaterial = (Material)target.serializedObject.targetObject;
                            targetMaterial.SetColor("_EmissionColor", color);
                        }
                }
            };

            return FinalizeColumns(columns, out propNames);
        }
    }
}
