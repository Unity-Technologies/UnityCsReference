// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    internal class SkyboxProceduralShaderGUI : ShaderGUI
    {
        private enum SunDiskMode
        {
            None,
            Simple,
            HighQuality
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            materialEditor.SetDefaultGUIWidths();

            MaterialProperty sunDiskModeProp = FindProperty("_SunDisk", props);
            SunDiskMode sunDiskMode = (SunDiskMode)sunDiskModeProp.floatValue;

            for (var i = 0; i < props.Length; i++)
            {
                if ((props[i].flags & (MaterialProperty.PropFlags.HideInInspector | MaterialProperty.PropFlags.PerRendererData)) != 0)
                    continue;

                //_SunSizeConvergence is only used with the HighQuality sun disk.
                if ((props[i].name == "_SunSizeConvergence") && (sunDiskMode != SunDiskMode.HighQuality))
                    continue;

                float h = materialEditor.GetPropertyHeight(props[i], props[i].displayName);
                Rect r = EditorGUILayout.GetControlRect(true, h, EditorStyles.layerMaskField);

                materialEditor.ShaderProperty(r, props[i], props[i].displayName);
            }
        }
    }
} // namespace UnityEditor
