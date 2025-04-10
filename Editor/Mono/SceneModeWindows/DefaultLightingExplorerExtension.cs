// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    public class DefaultLightingExplorerExtension : ILightingExplorerExtension
    {
        private static class Styles
        {
            public static readonly GUIContent[] ProjectionStrings = { EditorGUIUtility.TrTextContent("Infinite"), EditorGUIUtility.TrTextContent("Box") };
            public static readonly GUIContent[] LightmapEmissiveStrings = { EditorGUIUtility.TrTextContent("Realtime"), EditorGUIUtility.TrTextContent("Baked") };
            public static readonly GUIContent Name = EditorGUIUtility.TrTextContent("Name");
            public static readonly GUIContent Enabled = EditorGUIUtility.TrTextContent("Enabled");
            public static readonly GUIContent Type = EditorGUIUtility.TrTextContent("Type");
            public static readonly GUIContent Shape = EditorGUIUtility.TrTextContent("Shape");
            public static readonly GUIContent Mode = EditorGUIUtility.TrTextContent("Mode");
            public static readonly GUIContent Color = EditorGUIUtility.TrTextContent("Color");
            public static readonly GUIContent Range = EditorGUIUtility.TrTextContent("Range");
            public static readonly GUIContent Intensity = EditorGUIUtility.TrTextContent("Intensity");
            public static readonly GUIContent IndirectMultiplier = EditorGUIUtility.TrTextContent("Indirect Multiplier");
            public static readonly GUIContent ShadowType = EditorGUIUtility.TrTextContent("Shadows");
            public static readonly GUIContent Projection = EditorGUIUtility.TrTextContent("Projection");
            public static readonly GUIContent HDR = EditorGUIUtility.TrTextContent("HDR");
            public static readonly GUIContent ShadowDistance = EditorGUIUtility.TrTextContent("Shadow Distance");
            public static readonly GUIContent NearPlane = EditorGUIUtility.TrTextContent("Near Plane");
            public static readonly GUIContent FarPlane = EditorGUIUtility.TrTextContent("Far Plane");
            public static readonly GUIContent GlobalIllumination = EditorGUIUtility.TrTextContent("Global Illumination");
            public static readonly GUIContent SelectObjects = EditorGUIUtility.TextContent("");
            public static readonly GUIContent SelectObjectsButton = EditorGUIUtility.TrTextContentWithIcon("", "Find References in Scene", "UnityEditor.FindDependencies");

            public static readonly GUIContent LightCookieSprite = EditorGUIUtility.TrTextContent("Sprite");
            public static readonly GUIContent FallOff = EditorGUIUtility.TrTextContent("Falloff");
            public static readonly GUIContent FallOffStrength = EditorGUIUtility.TrTextContent("Falloff Strength");
            public static readonly GUIContent TargetSortingLayer = EditorGUIUtility.TrTextContent("Target Sorting Layer");
            public static readonly GUIContent All = EditorGUIUtility.TrTextContent("All");
            public static readonly GUIContent None = EditorGUIUtility.TrTextContent("None");
            public static readonly GUIContent Mixed = EditorGUIUtility.TrTextContent("Mixed...");
            public static readonly GUIContent ShadowIntensityEnabled = EditorGUIUtility.TrTextContent("Shadow");
            public static readonly GUIContent ShadowIntensity = EditorGUIUtility.TrTextContent("Shadow Strength");
            public static readonly GUIContent Light2DParametric = EditorGUIUtility.TrTextContentWithIcon("Parametric", "Parametric Lights have been deprecated. To continue, upgrade your Parametric Lights to Freeform Lights to enjoy similar light functionality.", MessageType.Warning);

            public static readonly GUIContent[] LightmapBakeTypeTitles = { EditorGUIUtility.TrTextContent("Realtime"), EditorGUIUtility.TrTextContent("Mixed"), EditorGUIUtility.TrTextContent("Baked") };
            public static readonly int[] LightmapBakeTypeValues = { (int)LightmapBakeType.Realtime, (int)LightmapBakeType.Mixed, (int)LightmapBakeType.Baked };

            public static readonly GUIContent[] LightTypeTitles = { EditorGUIUtility.TrTextContent("Spot"), EditorGUIUtility.TrTextContent("Directional"), EditorGUIUtility.TrTextContent("Point"), EditorGUIUtility.TrTextContent("Area (baked only)") };
            public static readonly int[] LightTypeValues = { (int)LightType.Spot, (int)LightType.Directional, (int)LightType.Point, (int)LightType.Rectangle };

            public static readonly GUIContent[] LightShapeTitles = { EditorGUIUtility.TrTextContent("Rectangle"), EditorGUIUtility.TrTextContent("Disc") };
            public static readonly int[] LightShapeValues = { (int)LightType.Rectangle, (int)LightType.Disc };

            public static readonly GUIContent[] Light2DTypeTitles = { EditorGUIUtility.TrTextContent("Freeform"), EditorGUIUtility.TrTextContent("Sprite"), EditorGUIUtility.TrTextContent("Spot"), EditorGUIUtility.TrTextContent("Global") };
            public static readonly int[] Light2DTypeValues = Enumerable.Range(0, Light2DTypeTitles.Length).ToArray();
        }

        public virtual LightingExplorerTab[] GetContentTabs()
        {
            return new[]
            {
                new LightingExplorerTab("Lights", GetLights, GetLightColumns, true),
                new LightingExplorerTab("2D Lights", Get2DLights, Get2DLightColumns, true),
                new LightingExplorerTab("Reflection Probes", GetReflectionProbes, GetReflectionProbeColumns, true),
                new LightingExplorerTab("Light Probes", GetLightProbes, GetLightProbeColumns, true),
                new LightingExplorerTab("Static Emissives", GetEmissives, GetEmissivesColumns, false)
            };
        }

        public virtual void OnEnable() {}
        public virtual void OnDisable() {}

        private static bool IsEditable(Object target)
        {
            return ((target.hideFlags & HideFlags.NotEditable) == 0);
        }

        protected static System.Collections.Generic.IEnumerable<T> GetObjectsForLightingExplorer<T>() where T : UnityEngine.Component
        {
            var objects = Resources.FindObjectsOfTypeAll<T>().Where((T obj) =>
            {
                return !EditorUtility.IsPersistent(obj) && !obj.hideFlags.HasFlag(HideFlags.HideInHierarchy) && !obj.hideFlags.HasFlag(HideFlags.HideAndDontSave);
            });

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            // No prefab mode.
            if (prefabStage == null)
            {
                // Return all object instances in the scene including prefab instances, but not those that are in prefab assets.
                return objects;
            }
            // In Context prefab mode with Normal rendering mode
            else if (prefabStage.mode == PrefabStage.Mode.InContext &&
                    StageNavigationManager.instance.contextRenderMode == StageUtility.ContextRenderMode.Normal)
            {
                // Return all object instances in the scene and objects in the opened prefab asset, but not objects in the opened prefab instance.
                return objects.Where((T obj) =>
                {
                    return !StageUtility.IsPrefabInstanceHiddenForInContextEditing(obj.gameObject);
                });
            }
            // All remaining cases, e.g. In Context with Hidden or GrayedOut rendering mode, or In Isolation prefab mode.
            else
            {
                // Return only objects in the opened prefab asset.
                return objects.Where((T obj) =>
                {
                    return EditorSceneManager.IsPreviewSceneObject(obj);
                });
            }
        }

        protected internal virtual UnityEngine.Object[] GetLights()
        {
            return GetObjectsForLightingExplorer<Light>().ToArray();
        }

        protected internal virtual UnityEngine.Object[] Get2DLights()
        {
            return GetObjectsForLightingExplorer<Light2DBase>().ToArray();
        }

        protected virtual  LightingExplorerTableColumn[] Get2DLightColumns()
        {
            return new[]
            {
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, Styles.Enabled, "m_Enabled", 50), // 0: Enabled
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, Styles.Name, null, 200), // 1: Name
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, Styles.Type, "m_LightType", 100, (r, prop, dep) => // 2
                {
                    if (prop != null)
                    {
                        if (prop.intValue != (int)Light2DType.Parametric)
                        {
                            EditorGUI.BeginProperty(r, GUIContent.none, prop);
                            EditorGUI.BeginChangeCheck();
                            int lightType = EditorGUI.IntPopup(r, prop.intValue - 1, Styles.Light2DTypeTitles, Styles.Light2DTypeValues);
                            if (EditorGUI.EndChangeCheck())
                                prop.intValue = lightType + 1;
                            EditorGUI.EndProperty();
                        }
                        else
                        {
                            EditorGUI.LabelField(r, Styles.Light2DParametric);
                        }
                    }
                }), // 2: LightType
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Color, Styles.Color, "m_Color", 100, (r, prop, dep) => // 3
                {
                    if (prop != null)
                        EditorGUI.PropertyField(r, prop, GUIContent.none);
                }), // 3: Color
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Custom, Styles.LightCookieSprite, "m_LightCookieSprite", 70, (r, prop, dep) => // 4
                {
                    if (prop != null)
                    {
                        var hasSpriteField = dep.Length > 0 && (dep[0].enumValueIndex == (int)Light2DType.Sprite);
                        if (hasSpriteField)
                        {
                            EditorGUI.BeginProperty(r, GUIContent.none, prop);
                            EditorGUI.BeginChangeCheck();
                            var sprite = EditorGUI.ObjectField(r, prop.objectReferenceValue, typeof(Sprite), false);
                            if (EditorGUI.EndChangeCheck())
                                prop.objectReferenceValue = sprite;
                            EditorGUI.EndProperty();
                        }
                    }
                }, null, null, new[] { 2 }), // 4: Sprite
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, Styles.Intensity, "m_Intensity", 60, (r, prop, dep) => // 5
                {
                    if (prop != null)
                    {
                        EditorGUI.BeginProperty(r, GUIContent.none, prop);
                        EditorGUI.BeginChangeCheck();
                        var intensity = EditorGUI.FloatField(r, prop.floatValue);
                        if (EditorGUI.EndChangeCheck())
                            prop.floatValue = intensity < 0f ? 0f : intensity;
                        EditorGUI.EndProperty();
                    }
                }), // 5: Intensity
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, Styles.FallOff, "m_ShapeLightFalloffSize", 60, (r, prop, dep) => // 6
                {
                    if (prop != null)
                    {
                        var hasFalloff = dep.Length > 0 && (dep[0].enumValueIndex == (int)Light2DType.Freeform);
                        if (hasFalloff)
                        {
                            EditorGUI.BeginProperty(r, GUIContent.none, prop);
                            EditorGUI.BeginChangeCheck();
                            var falloff = EditorGUI.FloatField(r, prop.floatValue);
                            if (EditorGUI.EndChangeCheck())
                                prop.floatValue = falloff < 0f ? 0f : falloff;
                            EditorGUI.EndProperty();
                        }
                    }
                }, null, null, new[] { 2 }),  // 6: Falloff
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, Styles.FallOffStrength, "m_FalloffIntensity", 120, (r, prop, dep) => // 7
                {
                    if (prop != null)
                    {
                        var hasFalloff = dep.Length > 0 && (dep[0].enumValueIndex == (int)Light2DType.Freeform || dep[0].enumValueIndex == (int)Light2DType.Point);
                        if (hasFalloff)
                        {
                            EditorGUI.BeginProperty(r, GUIContent.none, prop);
                            EditorGUI.BeginChangeCheck();
                            var newValue = EditorGUI.Slider(r, prop.floatValue, 0f, 1f);
                            if (EditorGUI.EndChangeCheck())
                                prop.floatValue = Mathf.Clamp01(newValue);
                            EditorGUI.EndProperty();
                        }
                    }
                }, null, null, new[] { 2 }),  // 7: Falloff intensity
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, Styles.TargetSortingLayer, "m_ApplyToSortingLayers", 120, // 8
                    (r, prop, dep) =>
                    {
                        if (prop != null && prop.isArray)
                        {
                            var allSortingLayers = SortingLayer.layers;

                            var propArraySize = prop.arraySize;
                            if (propArraySize == allSortingLayers.Length)
                                EditorGUI.LabelField(r, Styles.All);
                            else if (propArraySize == 1)
                                EditorGUI.LabelField(r, SortingLayer.IDToName(prop.GetArrayElementAtIndex(0).intValue));
                            else if (propArraySize == 0)
                                EditorGUI.LabelField(r, Styles.None);
                            else
                                EditorGUI.LabelField(r, Styles.Mixed);
                        }
                    }), // 8: Target Sorting Layer
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Custom, Styles.ShadowIntensityEnabled, "m_ShadowIntensityEnabled", 50, (r, prop, dep) => // 9
                {
                    if (prop != null)
                    {
                        var hasShadow = dep.Length > 0 && (dep[0].enumValueIndex != (int)Light2DType.Global);
                        if (hasShadow)
                        {
                            float off = Mathf.Max(0.0f, ((r.width / 2) - 8));
                            r.x += off;
                            r.width -= off;
                            EditorGUI.PropertyField(r, prop, GUIContent.none);
                        }
                    }
                }, null, null, new[] { 2 }), // 9: Shadow Intensity Enabled
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Custom, Styles.ShadowIntensity, "m_ShadowIntensity", 140, (r, prop, dep) => // 10
                {
                    if (prop != null)
                    {
                        var hasShadow = dep.Length > 0 && (dep[0].enumValueIndex != (int)Light2DType.Global);
                        if (hasShadow)
                        {
                            var shadowIntensityEnabled = dep[1].boolValue;

                            EditorGUI.BeginDisabled(!shadowIntensityEnabled);
                            EditorGUI.BeginChangeCheck();
                            var shadowIntensityProp = dep[0].serializedObject.FindProperty("m_ShadowIntensity");

                            var newShadowIntensity = EditorGUI.Slider(r, shadowIntensityProp.floatValue, 0f, 1f);
                            if (EditorGUI.EndChangeCheck())
                            {
                                shadowIntensityProp.floatValue = Mathf.Clamp01(newShadowIntensity);
                            }

                            EditorGUI.EndDisabled();
                        }
                    }
                } , null, null, new[] { 2, 9 })// 10: Shadow Intensity
            };
        }

        protected virtual LightingExplorerTableColumn[] GetLightColumns()
        {
            return new[]
            {
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, Styles.Enabled, "m_Enabled", 50), // 0: Enabled
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, Styles.Name, null, 200), // 1: Name
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, Styles.Type, "m_Type", 120, (r, prop, dep) =>
                {
                    // To the user, we will only display it as a area light, but under the hood, we have Rectangle and Disc. This is not to confuse people
                    // who still use our legacy light inspector.

                    int selectedLightType = prop.intValue;

                    if (!Styles.LightTypeValues.Contains(prop.intValue))
                    {
                        if (prop.intValue == (int)LightType.Disc)
                        {
                            selectedLightType = (int)LightType.Rectangle;
                        }
                        else
                        {
                            Debug.LogError("Light type is not supported by the Light Explorer.");
                        }
                    }

                    EditorGUI.BeginProperty(r, GUIContent.none, prop);
                    EditorGUI.BeginChangeCheck();
                    int type = EditorGUI.IntPopup(r, selectedLightType, Styles.LightTypeTitles, Styles.LightTypeValues);

                    if (EditorGUI.EndChangeCheck())
                    {
                        prop.intValue = type;
                    }
                    EditorGUI.EndProperty();
                }),      // 2: Type
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, Styles.Shape, "m_Type", 120, (r, prop, dep) =>
                {
                    // This is only appliable to the Area lights that have a shape. For the other lights, nothing will be shown
                    if (Styles.LightShapeValues.Contains(prop.intValue))
                    {
                        EditorGUI.BeginProperty(r, GUIContent.none, prop);
                        EditorGUI.BeginChangeCheck();
                        int type = EditorGUI.IntPopup(r, prop.intValue, Styles.LightShapeTitles, Styles.LightShapeValues);

                        if (EditorGUI.EndChangeCheck())
                        {
                            prop.intValue = type;
                        }
                        EditorGUI.EndProperty();
                    }
                }),      // 3: Shape
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, Styles.Mode, "m_Lightmapping", 70, (r, prop, dep) =>
                {
                    bool areaLight = dep.Length > 1 && (dep[0].enumValueIndex == (int)LightType.Rectangle || dep[0].enumValueIndex == (int)LightType.Disc);

                    using (new EditorGUI.DisabledScope(areaLight))
                    {
                        EditorGUI.BeginProperty(r, GUIContent.none, prop);
                        EditorGUI.BeginChangeCheck();
                        int newval = EditorGUI.IntPopup(r, prop.intValue, Styles.LightmapBakeTypeTitles, Styles.LightmapBakeTypeValues);
                        if (EditorGUI.EndChangeCheck())
                        {
                            prop.intValue = newval;
                        }
                        EditorGUI.EndProperty();
                    }
                }, null, null, new int[] { 2 }),     // 4: Mode
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Color, Styles.Color, "m_Color", 70), // 5: Color
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, Styles.Range, "m_Range", 60, (r, prop, dep) => 
                {
                    var lightType = prop.serializedObject.FindProperty("m_Type");
                    if (lightType != null)
                    {
                        bool directionalLight = lightType.enumValueIndex == (int)LightType.Directional;
                        if (!directionalLight)
                        {
                            EditorGUI.PropertyField(r, prop, GUIContent.none);
                        }
                    }
                }), // 6: Range
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, Styles.Intensity, "m_Intensity", 60), // 7: Intensity
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, Styles.IndirectMultiplier, "m_BounceIntensity", 110, (r, prop, dep) =>
                {
                    bool realtimeLight = dep.Length > 1 && dep[0].intValue == (int)LightmapBakeType.Realtime;

                    if (!(realtimeLight && !SupportedRenderingFeatures.IsLightmapBakeTypeSupported(LightmapBakeType.Realtime)))
                    {
                        EditorGUI.PropertyField(r, prop, GUIContent.none);
                    }
                }, null, null, new int[] { 4 }), // 8: Indirect Multiplier
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Enum, Styles.ShadowType, "m_Shadows.m_Type", 100, (r, prop, dep) =>
                {
                    bool areaLight = dep.Length > 1 && (dep[0].enumValueIndex == (int)LightType.Rectangle || dep[0].enumValueIndex == (int)LightType.Disc);

                    if (areaLight)
                    {
                        EditorGUI.BeginProperty(r, GUIContent.none, prop);
                        EditorGUI.BeginChangeCheck();
                        bool shadows = EditorGUI.Toggle(r, prop.intValue != (int)LightShadows.None);

                        if (EditorGUI.EndChangeCheck())
                        {
                            prop.intValue = shadows ? (int)LightShadows.Soft : (int)LightShadows.None;
                        }
                        EditorGUI.EndProperty();
                    }
                    else
                    {
                        EditorGUI.PropertyField(r, prop, GUIContent.none);
                    }
                }, null, null, new int[] { 2 }),     // 9: Shadow Type
            };
        }

        protected internal virtual UnityEngine.Object[] GetReflectionProbes()
        {
            return GetObjectsForLightingExplorer<ReflectionProbe>().ToArray();
        }

        protected virtual LightingExplorerTableColumn[] GetReflectionProbeColumns()
        {
            return new[]
            {
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, Styles.Enabled, "m_Enabled", 50), // 0: Enabled
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, Styles.Name, null, 200),  // 1: Name
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Int, Styles.Mode, "m_Mode", 70, (r, prop, dep) =>
                {
                    EditorGUI.IntPopup(r, prop, ReflectionProbeEditor.Styles.reflectionProbeMode, ReflectionProbeEditor.Styles.reflectionProbeModeValues, GUIContent.none);
                }),     // 2: Mode
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Int, Styles.Projection, "m_BoxProjection", 80, (r, prop, dep) =>
                {
                    int[] opts = { 0, 1 };
                    prop.boolValue = EditorGUI.IntPopup(r, prop.boolValue ? 1 : 0, Styles.ProjectionStrings, opts) == 1;
                }),     // 3: Projection
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, Styles.HDR, "m_HDR", 35),  // 4: HDR
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, Styles.ShadowDistance, "m_ShadowDistance", 35), // 5: Shadow Distance
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, Styles.NearPlane, "m_NearClip", 70), // 6: Near Plane
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, Styles.FarPlane, "m_FarClip", 70), // 7: Far Plane
            };
        }

        protected internal virtual UnityEngine.Object[] GetLightProbes()
        {
            return GetObjectsForLightingExplorer<LightProbeGroup>().ToArray();
        }

        protected virtual LightingExplorerTableColumn[] GetLightProbeColumns()
        {
            return new[]
            {
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, Styles.Enabled, "m_Enabled", 50), // 0: Enabled
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, Styles.Name, null, 200), // 1: Name
            };
        }

        protected internal virtual UnityEngine.Object[] GetEmissives()
        {
            return GetObjectsForLightingExplorer<MeshRenderer>().Where((MeshRenderer mr) =>
            {
                return GameObjectUtility.AreStaticEditorFlagsSet(mr.gameObject, StaticEditorFlags.ContributeGI);
            }).SelectMany(meshRenderer => meshRenderer.sharedMaterials).Where((Material m) =>

            {
                return m != null && ((m.globalIlluminationFlags & MaterialGlobalIlluminationFlags.AnyEmissive) != 0) && m.HasProperty("_EmissionColor");
            }).Distinct().ToArray();
        }

        protected virtual LightingExplorerTableColumn[] GetEmissivesColumns()
        {
            return new[]
            {
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Custom, Styles.SelectObjects, "m_LightmapFlags", 20, (r, prop, dep) =>
                {
                    if (GUI.Button(r, Styles.SelectObjectsButton, "label"))
                    {
                        SearchableEditorWindow.SearchForReferencesToInstanceID(prop.serializedObject.targetObject.GetInstanceID());
                    }
                }),     // 0: Icon
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, Styles.Name, null, 200), // 1: Name
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Int, Styles.GlobalIllumination, "m_LightmapFlags", 120, (r, prop, dep) =>
                {
                    if (!prop.serializedObject.targetObject.GetType().Equals(typeof(Material)))
                        return;

                    using (new EditorGUI.DisabledScope(!IsEditable(prop.serializedObject.targetObject)))
                    {
                        MaterialGlobalIlluminationFlags giFlags = ((prop.intValue & (int)MaterialGlobalIlluminationFlags.BakedEmissive) != 0) ? MaterialGlobalIlluminationFlags.BakedEmissive : MaterialGlobalIlluminationFlags.RealtimeEmissive;

                        int[] lightmapEmissiveValues = { (int)MaterialGlobalIlluminationFlags.RealtimeEmissive, (int)MaterialGlobalIlluminationFlags.BakedEmissive };

                        EditorGUI.BeginProperty(r, GUIContent.none, prop);
                        EditorGUI.BeginChangeCheck();

                        giFlags = (MaterialGlobalIlluminationFlags)EditorGUI.IntPopup(r, (int)giFlags, Styles.LightmapEmissiveStrings, lightmapEmissiveValues);

                        if (EditorGUI.EndChangeCheck())
                        {
                            Material material = (Material)prop.serializedObject.targetObject;
                            Undo.RecordObject(material, $"Modify Emission Flags of {material.name}");
                            material.globalIlluminationFlags = giFlags;
                            EditorUtility.SetDirty(material);

                            prop.serializedObject.Update();
                        }
                        EditorGUI.EndProperty();
                    }
                }),     // 2: GI
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Custom, Styles.Color, "m_Shader", 120, (r, prop, dep) =>
                {
                    if (!prop.serializedObject.targetObject.GetType().Equals(typeof(Material)))
                        return;

                    using (new EditorGUI.DisabledScope(!IsEditable(prop.serializedObject.targetObject)))
                    {
                        Material material = (Material)prop.serializedObject.targetObject;

                        Color color = material.GetColor("_EmissionColor");

                        EditorGUI.BeginProperty(r, GUIContent.none, prop);
                        EditorGUI.BeginChangeCheck();
                        Color newValue = EditorGUI.ColorField(r, GUIContent.Temp(""), color, true, false, true);

                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(material, $"Modify Emission Flags of {material.name}");
                            material.SetColor("_EmissionColor", newValue);
                            EditorUtility.SetDirty(material);
                        }
                        EditorGUI.EndProperty();
                    }
                }, (lhs, rhs) =>
                    {
                        float lh, ls, lv, rh, rs, rv;
                        Color.RGBToHSV(((Material)lhs.serializedObject.targetObject).GetColor("_EmissionColor"), out lh, out ls, out lv);
                        Color.RGBToHSV(((Material)rhs.serializedObject.targetObject).GetColor("_EmissionColor"), out rh, out rs, out rv);
                        return lv.CompareTo(rv);
                    }, (target, source) =>
                    {
                        Material sourceMaterial = (Material)source.serializedObject.targetObject;
                        Color color = sourceMaterial.GetColor("_EmissionColor");

                        Material targetMaterial = (Material)target.serializedObject.targetObject;
                        Undo.RecordObject(targetMaterial, $"Modify Emission Flags of {targetMaterial.name}");
                        targetMaterial.SetColor("_EmissionColor", color);
                        EditorUtility.SetDirty(targetMaterial);
                    }) // 3: Color
            };
        }
    }
}
