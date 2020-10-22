// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace UnityEditor.DeviceSimulation
{
    class DeviceInfoAsset : ScriptableObject
    {
        public DeviceInfo deviceInfo;
        public string[] parseErrors;
    }

    [CustomEditor(typeof(DeviceInfoAsset))]
    class DeviceInfoAssetEditor : Editor
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
