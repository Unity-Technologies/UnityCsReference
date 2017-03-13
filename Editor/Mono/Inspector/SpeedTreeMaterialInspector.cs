// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor
{
    [CanEditMultipleObjects]
    internal class SpeedTreeMaterialInspector : MaterialEditor
    {
        private enum SpeedTreeGeometryType
        {
            Branch = 0,
            BranchDetail,
            Frond,
            Leaf,
            Mesh // mapped with GEOM_TYPE_MESH in SpeedTreeImporter
        }

        private string[] speedTreeGeometryTypeString =
        {
            "GEOM_TYPE_BRANCH",
            "GEOM_TYPE_BRANCH_DETAIL",
            "GEOM_TYPE_FROND",
            "GEOM_TYPE_LEAF",
            "GEOM_TYPE_MESH"
        };

        private bool ShouldEnableAlphaTest(SpeedTreeGeometryType geomType)
        {
            return geomType == SpeedTreeGeometryType.Frond
                || geomType == SpeedTreeGeometryType.Leaf;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var theShader = serializedObject.FindProperty("m_Shader");

            // if we are not visible... return
            if (!isVisible || theShader.hasMultipleDifferentValues || theShader.objectReferenceValue == null)
                return;

            List<MaterialProperty> props = new List<MaterialProperty>(GetMaterialProperties(targets));

            SetDefaultGUIWidths();

            // Geometry type choice
            //---------------------------------------------------------------
            var geomTypes = new SpeedTreeGeometryType[targets.Length];
            for (int i = 0; i < targets.Length; ++i)
            {
                geomTypes[i] = SpeedTreeGeometryType.Branch;
                for (int j = 0; j < speedTreeGeometryTypeString.Length; ++j)
                {
                    if (((Material)targets[i]).shaderKeywords.Contains(speedTreeGeometryTypeString[j]))
                    {
                        geomTypes[i] = (SpeedTreeGeometryType)j;
                        break;
                    }
                }
            }
            EditorGUI.showMixedValue = geomTypes.Distinct().Count() > 1;
            EditorGUI.BeginChangeCheck();
            var setGeomType = (SpeedTreeGeometryType)EditorGUILayout.EnumPopup("Geometry Type", geomTypes[0]);
            if (EditorGUI.EndChangeCheck())
            {
                bool shouldEnableAlphaTest = ShouldEnableAlphaTest(setGeomType);
                UnityEngine.Rendering.CullMode cullMode = shouldEnableAlphaTest ? UnityEngine.Rendering.CullMode.Off : UnityEngine.Rendering.CullMode.Back;

                foreach (var m in targets.Cast<Material>())
                {
                    if (shouldEnableAlphaTest)
                        m.SetOverrideTag("RenderType", "treeTransparentCutout");
                    for (int i = 0; i < speedTreeGeometryTypeString.Length; ++i)
                        m.DisableKeyword(speedTreeGeometryTypeString[i]);
                    m.EnableKeyword(speedTreeGeometryTypeString[(int)setGeomType]);
                    m.renderQueue = shouldEnableAlphaTest ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest : (int)UnityEngine.Rendering.RenderQueue.Geometry;
                    m.SetInt("_Cull", (int)cullMode);
                }
            }
            EditorGUI.showMixedValue = false;

            //---------------------------------------------------------------
            var mainTex = props.Find(prop => prop.name == "_MainTex");
            if (mainTex != null)
            {
                props.Remove(mainTex);
                ShaderProperty(mainTex, mainTex.displayName);
            }

            //---------------------------------------------------------------
            var bumpMap = props.Find(prop => prop.name == "_BumpMap");
            if (bumpMap != null)
            {
                props.Remove(bumpMap);

                var enableBump = targets.Select(t => ((Material)t).shaderKeywords.Contains("EFFECT_BUMP"));
                bool? enable = ToggleShaderProperty(bumpMap, enableBump.First(), enableBump.Distinct().Count() > 1);
                if (enable != null)
                {
                    foreach (var m in targets.Cast<Material>())
                    {
                        if (enable.Value)
                            m.EnableKeyword("EFFECT_BUMP");
                        else
                            m.DisableKeyword("EFFECT_BUMP");
                    }
                }
            }

            //---------------------------------------------------------------
            var detailTex = props.Find(prop => prop.name == "_DetailTex");
            if (detailTex != null)
            {
                props.Remove(detailTex);
                if (geomTypes.Contains(SpeedTreeGeometryType.BranchDetail))
                    ShaderProperty(detailTex, detailTex.displayName);
            }

            //---------------------------------------------------------------
            var enableHueVariation = targets.Select(t => ((Material)t).shaderKeywords.Contains("EFFECT_HUE_VARIATION"));
            var hueVariation = props.Find(prop => prop.name == "_HueVariation");
            if (enableHueVariation != null && hueVariation != null)
            {
                props.Remove(hueVariation);
                bool? enable = ToggleShaderProperty(hueVariation, enableHueVariation.First(), enableHueVariation.Distinct().Count() > 1);
                if (enable != null)
                {
                    foreach (var m in targets.Cast<Material>())
                    {
                        if (enable.Value)
                            m.EnableKeyword("EFFECT_HUE_VARIATION");
                        else
                            m.DisableKeyword("EFFECT_HUE_VARIATION");
                    }
                }
            }

            //---------------------------------------------------------------
            var alphaCutoff = props.Find(prop => prop.name == "_Cutoff");
            if (alphaCutoff != null)
            {
                props.Remove(alphaCutoff);
                if (geomTypes.Any(t => ShouldEnableAlphaTest(t)))
                    ShaderProperty(alphaCutoff, alphaCutoff.displayName);
            }

            //---------------------------------------------------------------
            foreach (var prop in props)
            {
                if ((prop.flags & (MaterialProperty.PropFlags.HideInInspector | MaterialProperty.PropFlags.PerRendererData)) != 0)
                    continue;
                ShaderProperty(prop, prop.displayName);
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            RenderQueueField();
            EnableInstancingField();
        }

        private bool? ToggleShaderProperty(MaterialProperty prop, bool enable, bool hasMixedEnable)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = hasMixedEnable;
            enable = EditorGUI.ToggleLeft(EditorGUILayout.GetControlRect(false, GUILayout.ExpandWidth(false)), prop.displayName, enable);
            EditorGUI.showMixedValue = false;
            bool? retValue = EditorGUI.EndChangeCheck() ? (bool?)enable : null;

            GUILayout.Space(-EditorGUIUtility.singleLineHeight);
            using (new EditorGUI.DisabledScope(!enable && !hasMixedEnable))
            {
                EditorGUI.showMixedValue = prop.hasMixedValue;
                ShaderProperty(prop, " ");
                EditorGUI.showMixedValue = false;
            }
            return retValue;
        }
    }
}
