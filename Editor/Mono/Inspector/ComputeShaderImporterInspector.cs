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
    [CustomEditor(typeof(ComputeShaderImporter))]
    internal class ComputeShaderImporterInspector : AssetImporterEditor
    {
        internal class Styles
        {
            public static GUIContent overridePreprocessor = EditorGUIUtility.TrTextContent("Override preprocessor", "Select preprocessor to use for this shader.");
        }

        SerializedProperty preprocessorOverride;

        internal override void OnHeaderControlsGUI()
        {
            GUILayout.FlexibleSpace();
            ShowOpenButton(new[] { assetTarget });
        }

        public override void OnEnable()
        {
            base.OnEnable();

            preprocessorOverride = serializedObject.FindProperty("m_PreprocessorOverride");
        }

        protected override void Apply()
        {
            base.Apply();

            var importer = target as ComputeShaderImporter;
            if (importer == null)
                return;

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(importer));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(preprocessorOverride, Styles.overridePreprocessor);

            serializedObject.ApplyModifiedProperties();

            ApplyRevertGUI();
        }
    }
}
