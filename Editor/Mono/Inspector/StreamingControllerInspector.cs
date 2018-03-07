// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(StreamingController))]
    [CanEditMultipleObjects]
    internal class StreamingControllerEditor : Editor
    {
        public SerializedProperty streamingMipmapBias { get; private set; }

        internal static class Styles
        {
            public static GUIContent streamingMipmapBias = EditorGUIUtility.TrTextContent("Mip Map Bias", "When texture streaming is active, Unity loads mipmap levels for textures based on their distance from all active cameras. This bias is added to all textures visible from this camera and allows you to force smaller or larger mipmap levels to be loaded for textures visible from this camera.");
        }

        public void OnEnable()
        {
            streamingMipmapBias = serializedObject.FindProperty("m_StreamingMipmapBias");
        }

        override public void OnInspectorGUI()
        {
            serializedObject.Update();

            //DrawDefaultInspector();
            EditorGUILayout.PropertyField(streamingMipmapBias, Styles.streamingMipmapBias);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
