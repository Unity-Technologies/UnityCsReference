// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.DeviceSimulation
{
    internal class DeviceInfoAsset : ScriptableObject, ISerializationCallbackReceiver
    {
        public DeviceInfo deviceInfo;
        public string[] parseErrors;

        [NonSerialized] public bool editorResource;
        public string directory;

        public HashSet<string> availableSystemInfoFields = new HashSet<string>();
        public Dictionary<GraphicsDeviceType, HashSet<string>> availableGraphicsSystemInfoFields = new Dictionary<GraphicsDeviceType, HashSet<string>>();

        [SerializeField] private List<string> m_AvailableSystemInfoFields;
        [SerializeField] private List<GraphicsTypeFields> m_AvailableGraphicsSystemInfoFields;

        public void OnBeforeSerialize()
        {
            m_AvailableSystemInfoFields = new List<string>(availableSystemInfoFields);
            m_AvailableGraphicsSystemInfoFields = new List<GraphicsTypeFields>();

            if (availableGraphicsSystemInfoFields == null) return;

            foreach (var graphicsDevice in availableGraphicsSystemInfoFields)
            {
                m_AvailableGraphicsSystemInfoFields.Add(new GraphicsTypeFields
                {
                    type = graphicsDevice.Key,
                    fields = new List<string>(graphicsDevice.Value)
                });
            }
        }

        public void OnAfterDeserialize()
        {
            availableSystemInfoFields = new HashSet<string>(m_AvailableSystemInfoFields);
            availableGraphicsSystemInfoFields = new Dictionary<GraphicsDeviceType, HashSet<string>>();
            foreach (var graphicsDevice in m_AvailableGraphicsSystemInfoFields)
                availableGraphicsSystemInfoFields.Add(graphicsDevice.type, new HashSet<string>(graphicsDevice.fields));
        }

        // Wrapper because Unity can't serialize a list of lists
        [Serializable]
        private class GraphicsTypeFields
        {
            public GraphicsDeviceType type;
            public List<string> fields;
        }
    }

    [CustomEditor(typeof(DeviceInfoAsset))]
    internal class DeviceInfoAssetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var asset = serializedObject.targetObject as DeviceInfoAsset;
            if (asset.parseErrors != null && asset.parseErrors.Length > 0)
            {
                foreach (var error in asset.parseErrors)
                {
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                }
            }
            else
            {
                EditorGUILayout.LabelField(asset.deviceInfo.friendlyName);
            }
        }
    }
}
