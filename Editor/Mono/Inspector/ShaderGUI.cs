// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    public abstract class ShaderGUI
    {
        virtual public void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            materialEditor.PropertiesDefaultGUI(properties);
        }

        virtual public void OnMaterialPreviewGUI(MaterialEditor materialEditor, Rect r, GUIStyle background)
        {
            materialEditor.DefaultPreviewGUI(r, background);
        }

        virtual public void OnMaterialInteractivePreviewGUI(MaterialEditor materialEditor, Rect r, GUIStyle background)
        {
            materialEditor.DefaultPreviewGUI(r, background);
        }

        virtual public void OnMaterialPreviewSettingsGUI(MaterialEditor materialEditor)
        {
            materialEditor.DefaultPreviewSettingsGUI();
        }

        virtual public void OnClosed(Material material)
        {
        }

        virtual public void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            material.shader = newShader;
        }

        // Utility methods
        protected static MaterialProperty FindProperty(string propertyName, MaterialProperty[] properties)
        {
            return FindProperty(propertyName, properties, true);
        }

        protected static MaterialProperty FindProperty(string propertyName, MaterialProperty[] properties, bool propertyIsMandatory)
        {
            for (var i = 0; i < properties.Length; i++)
                if (properties[i] != null && properties[i].name == propertyName)
                    return properties[i];

            // We assume all required properties can be found, otherwise something is broken
            if (propertyIsMandatory)
                throw new ArgumentException("Could not find MaterialProperty: '" + propertyName + "', Num properties: " + properties.Length);
            return null;
        }
    }

    internal static class ShaderGUIUtility
    {
        private static Type ExtractCustomEditorType(string customEditorName)
        {
            if (string.IsNullOrEmpty(customEditorName)) return null;

            // To allow users to implement their own ShaderGUI for the Standard shader we iterate in reverse order
            // because the UnityEditor assembly is assumed first in the assembly list.
            // Users can now place a copy of the StandardShaderGUI script in the project and start modifying that copy to make their own version.

            string unityEditorFullName = "UnityEditor." + customEditorName; // for convenience: adding UnityEditor namespace is not needed in the shader

            var editorAssemblies = EditorAssemblies.loadedAssemblies;
            for (int i = editorAssemblies.Length - 1; i >= 0; i--)
            {
                foreach (var type in AssemblyHelper.GetTypesFromAssembly(editorAssemblies[i]))
                {
                    if (type.FullName.Equals(customEditorName, StringComparison.Ordinal) || type.FullName.Equals(unityEditorFullName, StringComparison.Ordinal))
                        return typeof(ShaderGUI).IsAssignableFrom(type) ? type : null;
                }
            }
            return null;
        }

        internal static ShaderGUI CreateShaderGUI(string customEditorName)
        {
            Type customEditorType = ExtractCustomEditorType(customEditorName);
            return customEditorType != null ? (Activator.CreateInstance(customEditorType) as ShaderGUI) : null;
        }
    }
} // namespace UnityEditor
