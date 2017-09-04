// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEditor.Build;

namespace UnityEditor
{
    internal class SkyboxPanoramicShaderGUI : ShaderGUI
    {
        readonly AnimBool m_ShowLatLongLayout = new AnimBool();
        readonly AnimBool m_ShowMirrorOnBack = new AnimBool();
        readonly AnimBool m_Show3DControl = new AnimBool();

        bool m_Initialized = false;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            if (!m_Initialized)
            {
                m_ShowLatLongLayout.valueChanged.AddListener(materialEditor.Repaint);
                m_ShowMirrorOnBack.valueChanged.AddListener(materialEditor.Repaint);
                m_Show3DControl.valueChanged.AddListener(materialEditor.Repaint);
                m_Initialized = true;
            }

            // Allow the default implementation to set widths for consistency for common properties.
            float lw = EditorGUIUtility.labelWidth;
            materialEditor.SetDefaultGUIWidths();
            ShowProp(materialEditor, FindProperty("_Tint", props));
            ShowProp(materialEditor, FindProperty("_Exposure", props));
            ShowProp(materialEditor, FindProperty("_Rotation", props));
            ShowProp(materialEditor, FindProperty("_MainTex", props));
            EditorGUIUtility.labelWidth = lw;

            m_ShowLatLongLayout.target = ShowProp(materialEditor, FindProperty("_Mapping", props)) == 1;
            if (EditorGUILayout.BeginFadeGroup(m_ShowLatLongLayout.faded))
            {
                m_ShowMirrorOnBack.target = ShowProp(materialEditor, FindProperty("_ImageType", props)) == 1;
                if (EditorGUILayout.BeginFadeGroup(m_ShowMirrorOnBack.faded))
                {
                    EditorGUI.indentLevel++;
                    ShowProp(materialEditor, FindProperty("_MirrorOnBack", props));
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFadeGroup();

                // Show 3D settings if VR support is enabled on ANY platform.
                m_Show3DControl.value = false;
                foreach (BuildPlatform cur in BuildPlatforms.instance.buildPlatforms)
                {
                    if (UnityEditorInternal.VR.VREditor.GetVREnabledOnTargetGroup(cur.targetGroup))
                    {
                        m_Show3DControl.value = true;
                        break;
                    }
                }
                if (EditorGUILayout.BeginFadeGroup(m_Show3DControl.faded))
                    ShowProp(materialEditor, FindProperty("_Layout", props));
                EditorGUILayout.EndFadeGroup();
            }
            EditorGUILayout.EndFadeGroup();

            // Let the default implementation add the extra shader properties at the bottom.
            materialEditor.PropertiesDefaultGUI(new MaterialProperty[0]);
        }

        private float ShowProp(MaterialEditor materialEditor, MaterialProperty prop)
        {
            materialEditor.ShaderProperty(prop, prop.displayName);
            return prop.floatValue;
        }
    }
}
