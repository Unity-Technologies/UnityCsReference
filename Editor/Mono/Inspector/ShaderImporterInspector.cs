// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Experimental.AssetImporters;
using System.Linq;
using System.Reflection;
using UnityEngine.Scripting;

namespace UnityEditor
{
    [CustomEditor(typeof(ShaderImporter))]
    internal class ShaderImporterInspector : AssetImporterEditor
    {
        private class TextureProp
        {
            public string propertyName;
            public string displayName;
            public Texture texture;
            public UnityEngine.Rendering.TextureDimension dimension;
            public bool modifiable;
        }

        private List<TextureProp> m_Properties = new List<TextureProp>();

        internal override void OnHeaderControlsGUI()
        {
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Open...", EditorStyles.miniButton))
            {
                AssetDatabase.OpenAsset(assetTarget);
                GUIUtility.ExitGUI();
            }
        }

        public override void OnEnable()
        {
            ResetValues();
        }

        private void ShowTextures()
        {
            if (m_Properties.Count == 0)
                return;

            EditorGUILayout.LabelField("Default Maps", EditorStyles.boldLabel);
            for (var i = 0; i < m_Properties.Count; i++)
            {
                if (!m_Properties[i].modifiable)
                    continue;

                DrawTextureField(m_Properties[i]);
            }

            EditorGUILayout.LabelField("NonModifiable Maps", EditorStyles.boldLabel);
            for (var i = 0; i < m_Properties.Count; i++)
            {
                if (m_Properties[i].modifiable)
                    continue;

                DrawTextureField(m_Properties[i]);
            }
        }

        private void DrawTextureField(TextureProp prop)
        {
            var oldTexture = prop.texture;
            Texture newTexture = null;

            EditorGUI.BeginChangeCheck();

            System.Type textureType = MaterialEditor.GetTextureTypeFromDimension(prop.dimension);

            if (textureType != null)
            {
                // Require at least two character in display name to prevent names like "-"
                var text = string.IsNullOrEmpty(prop.displayName) ? ObjectNames.NicifyVariableName(prop.propertyName) : prop.displayName;
                newTexture = EditorGUILayout.MiniThumbnailObjectField(GUIContent.Temp(text), oldTexture, textureType) as Texture;
            }

            if (EditorGUI.EndChangeCheck())
                prop.texture = newTexture;
        }

        public override bool HasModified()
        {
            if (base.HasModified())
                return true;

            var importer = target as ShaderImporter;
            if (importer == null)
                return false;

            var shader = importer.GetShader();
            if (shader == null)
                return false;

            var propertyCount = ShaderUtil.GetPropertyCount(shader);
            for (var i = 0; i < propertyCount; i++)
            {
                var propertyName = ShaderUtil.GetPropertyName(shader, i);
                for (var k = 0; k < m_Properties.Count; k++)
                {
                    if (m_Properties[k].propertyName == propertyName)
                    {
                        var tex = m_Properties[k].modifiable ? importer.GetDefaultTexture(propertyName) : importer.GetNonModifiableTexture(propertyName);
                        if (m_Properties[k].texture != tex)
                            return true;
                    }
                }
            }
            return false;
        }

        protected override void ResetValues()
        {
            base.ResetValues();

            m_Properties.Clear();

            var importer = target as ShaderImporter;
            if (importer == null)
                return;

            var shader = importer.GetShader();
            if (shader == null)
                return;

            var propertyCount = ShaderUtil.GetPropertyCount(shader);

            for (var i = 0; i < propertyCount; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
                    continue;

                var propertyName = ShaderUtil.GetPropertyName(shader, i);
                var displayName = ShaderUtil.GetPropertyDescription(shader, i);  // might be empty
                var modifiable = !ShaderUtil.IsShaderPropertyNonModifiableTexureProperty(shader, i);

                Texture tex;
                if (!modifiable)
                    tex = importer.GetNonModifiableTexture(propertyName);
                else
                    tex = importer.GetDefaultTexture(propertyName);

                var temp = new TextureProp
                {
                    propertyName = propertyName,
                    texture = tex,
                    dimension = ShaderUtil.GetTexDim(shader, i),
                    displayName = displayName,
                    modifiable = modifiable
                };
                m_Properties.Add(temp);
            }
        }

        protected override void Apply()
        {
            base.Apply();

            var importer = target as ShaderImporter;
            if (importer == null)
                return;

            var defaultNames = m_Properties.Where(x => x.modifiable).Select(x => x.propertyName).ToArray();
            var defaultTextures = m_Properties.Where(x => x.modifiable).Select(x => x.texture).ToArray();
            importer.SetDefaultTextures(defaultNames, defaultTextures);

            var nonModNames = m_Properties.Where(x => !x.modifiable).Select(x => x.propertyName).ToArray();
            var nonModTextures = m_Properties.Where(x => !x.modifiable).Select(x => x.texture).ToArray();
            importer.SetNonModifiableTextures(nonModNames, nonModTextures);

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(importer));
        }

        private static int GetNumberOfTextures(Shader shader)
        {
            int numberOfTextures = 0;
            var propertyCount = ShaderUtil.GetPropertyCount(shader);

            for (var i = 0; i < propertyCount; i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                    numberOfTextures++;
            }
            return numberOfTextures;
        }

        public override void OnInspectorGUI()
        {
            var importer = target as ShaderImporter;
            if (importer == null)
                return;

            var shader = importer.GetShader();
            if (shader == null)
                return;

            if (GetNumberOfTextures(shader) != m_Properties.Count)
                ResetValues();

            ShowTextures();
            ApplyRevertGUI();
        }

        private static string[] s_ShaderIncludePaths = null;
        [RequiredByNativeCode]
        internal static string[] GetShaderIncludePaths()
        {
            if (s_ShaderIncludePaths == null)
            {
                List<string> results = new List<string>();
                var methods = AttributeHelper.GetMethodsWithAttribute<ShaderIncludePathAttribute>(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
                foreach (var method in methods.methodsWithAttributes)
                {
                    if (method.info.ReturnType == typeof(string[]) && method.info.GetParameters().Length == 0)
                    {
                        var result = method.info.Invoke(null, new object[] {}) as string[];
                        if (result != null)
                            results.AddRange(result);
                    }
                }
                // The paths appear in the list in random order. We sort the list here so that the same paths will always
                // result into the exact same list. Otherwise the shader importer will think the paths have changed.
                results.Sort();
                s_ShaderIncludePaths = results.ToArray();
            }
            return s_ShaderIncludePaths;
        }
    }
}
