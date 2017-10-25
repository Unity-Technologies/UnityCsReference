// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/AssetPipeline/DefaultImporter.h")]
    // This class is public for users to be able to make custom editors for this type (see case 656580)
    public class DefaultAsset : UnityEngine.Object
    {
        private DefaultAsset() {}
        internal extern string message { get; }
        internal extern bool isWarning {[NativeName("IsWarning")] get; }
    }

    [CustomEditor(typeof(DefaultAsset), isFallback = true)] // fallback so broad-matching user inspectors always win (e.g. case #656580)
    class DefaultAssetInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            var defaultAsset = (DefaultAsset)target;
            if (defaultAsset.message.Length > 0)
            {
                EditorGUILayout.HelpBox(
                    defaultAsset.message,
                    defaultAsset.isWarning ? MessageType.Warning : MessageType.Info);
            }
        }
    }
}
