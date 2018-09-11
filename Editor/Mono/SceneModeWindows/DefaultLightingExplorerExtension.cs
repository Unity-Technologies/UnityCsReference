// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Linq;

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

            public static readonly GUIContent[] LightmapBakeTypeTitles = { EditorGUIUtility.TrTextContent("Realtime"), EditorGUIUtility.TrTextContent("Mixed"), EditorGUIUtility.TrTextContent("Baked") };
            public static readonly int[] LightmapBakeTypeValues = { (int)LightmapBakeType.Realtime, (int)LightmapBakeType.Mixed, (int)LightmapBakeType.Baked };

            public static readonly GUIContent[] LightTypeTitles = { EditorGUIUtility.TrTextContent("Spot"), EditorGUIUtility.TrTextContent("Directional"), EditorGUIUtility.TrTextContent("Point"), EditorGUIUtility.TrTextContent("Area (baked only)") };
            public static readonly int[] LightTypeValues = { (int)LightType.Spot, (int)LightType.Directional, (int)LightType.Point, (int)LightType.Rectangle };

            public static readonly GUIContent[] LightShapeTitles = { EditorGUIUtility.TrTextContent("Rectangle"), EditorGUIUtility.TrTextContent("Disc") };
            public static readonly int[] LightShapeValues = { (int)LightType.Rectangle, (int)LightType.Disc };
        }

        public virtual LightingExplorerTab[] GetContentTabs()
        {
            return new[]
            {
                new LightingExplorerTab("Light Table", GetLights, GetLightColumns),
                new LightingExplorerTab("Reflection Probes", GetReflectionProbes, GetReflectionProbeColumns),
                new LightingExplorerTab("Light Probes", GetLightProbes, GetLightProbeColumns),
                new LightingExplorerTab("Static Emissives", GetEmissives, GetEmissivesColumns)
            };
        }

        public virtual void OnEnable() {}
        public virtual void OnDisable() {}

        private static bool IsEditable(Object target)
        {
            return ((target.hideFlags & HideFlags.NotEditable) == 0);
        }

        protected virtual UnityEngine.Object[] GetLights()
        {
            return UnityEngine.Object.FindObjectsOfType<Light>();
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
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, Styles.Intensity, "m_Intensity", 60), // 6: Intensity
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Float, Styles.IndirectMultiplier, "m_BounceIntensity", 110), // 7: Indirect Multiplier
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
                }, null, null, new int[] { 2 }),     // 8: Shadow Type
            };
        }

        protected virtual UnityEngine.Object[] GetReflectionProbes()
        {
            return UnityEngine.Object.FindObjectsOfType<ReflectionProbe>();
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

        protected virtual UnityEngine.Object[] GetLightProbes()
        {
            return UnityEngine.Object.FindObjectsOfType<LightProbeGroup>();
        }

        protected virtual LightingExplorerTableColumn[] GetLightProbeColumns()
        {
            return new[]
            {
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Checkbox, Styles.Enabled, "m_Enabled", 50), // 0: Enabled
                new LightingExplorerTableColumn(LightingExplorerTableColumn.DataType.Name, Styles.Name, null, 200), // 1: Name
            };
        }

        protected virtual UnityEngine.Object[] GetEmissives()
        {
            return Object.FindObjectsOfType<MeshRenderer>().Where((MeshRenderer mr) => {
                return (GameObjectUtility.AreStaticEditorFlagsSet(mr.gameObject, StaticEditorFlags.LightmapStatic));
            }).SelectMany(meshRenderer => meshRenderer.sharedMaterials).Where((Material m) => {
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
                            material.globalIlluminationFlags = giFlags;

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
                            material.SetColor("_EmissionColor", newValue);
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
                        targetMaterial.SetColor("_EmissionColor", color);
                    }) // 3: Color
            };
        }
    }
}
