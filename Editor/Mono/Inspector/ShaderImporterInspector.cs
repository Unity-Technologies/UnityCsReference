// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEditor
{
    [CustomEditor(typeof(ShaderImporter))]
    internal class ShaderImporterInspector : AssetImporterEditor
    {
        private List<string> propertyNames = new List<string>();
        private List<string> displayNames = new List<string>();
        private List<Texture> textures = new List<Texture>();
        private List<UnityEngine.Rendering.TextureDimension> dimensions = new List<UnityEngine.Rendering.TextureDimension>();

        internal override void OnHeaderControlsGUI()
        {
            var shaderAsset = assetEditor.target as Shader;
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Open...", EditorStyles.miniButton))
            {
                AssetDatabase.OpenAsset(shaderAsset);
                GUIUtility.ExitGUI();
            }
        }

        public override void OnEnable()
        {
            ResetValues();
        }

        private void ShowDefaultTextures()
        {
            if (propertyNames.Count == 0)
                return;

            EditorGUILayout.LabelField("Default Maps", EditorStyles.boldLabel);
            for (var i = 0; i < propertyNames.Count; i++)
            {
                var oldTexture = textures[i];
                Texture newTexture = null;

                EditorGUI.BeginChangeCheck();

                System.Type textureType = MaterialEditor.GetTextureTypeFromDimension(dimensions[i]);

                if (textureType != null)
                {
                    // Require at least two character in display name to prevent names like "-"
                    var text = string.IsNullOrEmpty(displayNames[i]) ? ObjectNames.NicifyVariableName(propertyNames[i]) : displayNames[i];
                    newTexture = EditorGUILayout.MiniThumbnailObjectField(GUIContent.Temp(text), oldTexture, textureType) as Texture;
                }

                if (EditorGUI.EndChangeCheck())
                    textures[i] = newTexture;
            }
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
            for (int i = 0; i < propertyCount; i++)
            {
                var propertyName = ShaderUtil.GetPropertyName(shader, i);
                for (int k = 0; k < propertyNames.Count; k++)
                {
                    if (propertyNames[k] == propertyName && textures[k] != importer.GetDefaultTexture(propertyName))
                        return true;
                }
            }
            return false;
        }

        protected override void ResetValues()
        {
            base.ResetValues();

            propertyNames = new List<string>();
            displayNames = new List<string>();
            textures = new List<Texture>();
            dimensions = new List<UnityEngine.Rendering.TextureDimension>();

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
                var texture = importer.GetDefaultTexture(propertyName);

                propertyNames.Add(propertyName);
                displayNames.Add(displayName);
                textures.Add(texture);

                dimensions.Add(ShaderUtil.GetTexDim(shader, i));
            }
        }

        protected override void Apply()
        {
            base.Apply();

            var importer = target as ShaderImporter;
            if (importer == null)
                return;

            importer.SetDefaultTextures(propertyNames.ToArray(), textures.ToArray());
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

            if (GetNumberOfTextures(shader) != propertyNames.Count)
                ResetValues();

            ShowDefaultTextures();
            ApplyRevertGUI();
        }
    }
}
