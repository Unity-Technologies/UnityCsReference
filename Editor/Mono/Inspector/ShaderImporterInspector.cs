// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Experimental.AssetImporters;
using System.Linq;
using UnityEditor.VersionControl;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [CustomEditor(typeof(ShaderImporter))]
    internal class ShaderImporterInspector : AssetImporterEditor
    {
        internal class Styles
        {
            public static GUIContent overridePreprocessor = EditorGUIUtility.TrTextContent("Override preprocessor", "Select preprocessor to use for this shader.");
        }

        [Serializable]
        private class TextureProp
        {
            public string propertyName;
            public string displayName;
            public Texture texture;
            public TextureDimension dimension;
            public bool modifiable;
        }

        private class ShaderProperties : ScriptableObject
        {
            public List<TextureProp> m_Properties = new List<TextureProp>();

            public void CleanUp()
            {
                m_Properties.Clear();
            }
        }

        SerializedProperty m_Properties;

        SerializedProperty preprocessorOverride;

        internal override void OnHeaderControlsGUI()
        {
            GUILayout.FlexibleSpace();
            ShowOpenButton(new[] { assetTarget });
        }

        protected override Type extraDataType => typeof(ShaderProperties);

        protected override void InitializeExtraDataInstance(Object extraTarget, int targetIndex)
        {
            var data = (ShaderProperties)extraTarget;
            data.CleanUp();

            var importer = targets[targetIndex] as ShaderImporter;
            if (importer == null)
                return;

            var shader = importer.GetShader();
            if (shader == null)
                return;

            var propertyCount = shader.GetPropertyCount();

            for (var i = 0; i < propertyCount; i++)
            {
                if (shader.GetPropertyType(i) != ShaderPropertyType.Texture)
                    continue;

                var propertyName = shader.GetPropertyName(i);
                var displayName = shader.GetPropertyDescription(i);  // might be empty
                var modifiable = (shader.GetPropertyFlags(i) & ShaderPropertyFlags.NonModifiableTextureData) == 0;

                Texture tex;
                if (!modifiable)
                    tex = importer.GetNonModifiableTexture(propertyName);
                else
                    tex = importer.GetDefaultTexture(propertyName);

                var temp = new TextureProp
                {
                    propertyName = propertyName,
                    texture = tex,
                    dimension = shader.GetPropertyTextureDimension(i),
                    displayName = displayName,
                    modifiable = modifiable
                };
                data.m_Properties.Add(temp);
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();

            m_Properties = extraDataSerializedObject.FindProperty("m_Properties");

            preprocessorOverride = serializedObject.FindProperty("m_PreprocessorOverride");
        }

        private void ShowTextures()
        {
            if (m_Properties.arraySize == 0)
                return;

            EditorGUILayout.LabelField("Default Maps", EditorStyles.boldLabel);
            for (var i = 0; i < m_Properties.arraySize; i++)
            {
                var prop = m_Properties.GetArrayElementAtIndex(i);
                if (!prop.FindPropertyRelative("modifiable").boolValue)
                    continue;

                DrawTextureField(prop);
            }

            EditorGUILayout.LabelField("NonModifiable Maps", EditorStyles.boldLabel);
            for (var i = 0; i < m_Properties.arraySize; i++)
            {
                var prop = m_Properties.GetArrayElementAtIndex(i);
                if (prop.FindPropertyRelative("modifiable").boolValue)
                    continue;

                DrawTextureField(prop);
            }
        }

        private void DrawTextureField(SerializedProperty prop)
        {
            var oldTexture = prop.FindPropertyRelative("texture").objectReferenceValue;
            Texture newTexture = null;

            EditorGUI.BeginChangeCheck();

            Type textureType = MaterialEditor.GetTextureTypeFromDimension((TextureDimension)prop.FindPropertyRelative("dimension").intValue);

            if (textureType != null)
            {
                // Require at least two character in display name to prevent names like "-"
                var displayName = prop.FindPropertyRelative("displayName").stringValue;
                var text = string.IsNullOrEmpty(displayName) ? ObjectNames.NicifyVariableName(prop.FindPropertyRelative("propertyName").stringValue) : displayName;
                newTexture = EditorGUILayout.MiniThumbnailObjectField(GUIContent.Temp(text), oldTexture, textureType) as Texture;
            }

            if (EditorGUI.EndChangeCheck())
                prop.FindPropertyRelative("texture").objectReferenceValue = newTexture;
        }

        protected override void Apply()
        {
            base.Apply();

            var importer = target as ShaderImporter;
            if (importer == null)
                return;

            var properties = (ShaderProperties)extraDataTarget;
            var defaultNames = properties.m_Properties.Where(x => x.modifiable).Select(x => x.propertyName).ToArray();
            var defaultTextures = properties.m_Properties.Where(x => x.modifiable).Select(x => x.texture).ToArray();
            importer.SetDefaultTextures(defaultNames, defaultTextures);

            var nonModNames = properties.m_Properties.Where(x => !x.modifiable).Select(x => x.propertyName).ToArray();
            var nonModTextures = properties.m_Properties.Where(x => !x.modifiable).Select(x => x.texture).ToArray();
            importer.SetNonModifiableTextures(nonModNames, nonModTextures);

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(importer));
        }

        private static int GetNumberOfTextures(Shader shader)
        {
            int numberOfTextures = 0;
            var propertyCount = shader.GetPropertyCount();

            for (var i = 0; i < propertyCount; i++)
            {
                if (shader.GetPropertyType(i) == ShaderPropertyType.Texture)
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

            if (GetNumberOfTextures(shader) != m_Properties.arraySize)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    InitializeExtraDataInstance(extraDataTargets[i], i);
                }
            }

            serializedObject.Update();
            extraDataSerializedObject.Update();

            EditorGUILayout.PropertyField(preprocessorOverride, Styles.overridePreprocessor);

            ShowTextures();

            extraDataSerializedObject.ApplyModifiedProperties();
            serializedObject.ApplyModifiedProperties();

            ApplyRevertGUI();
        }
    }
}
