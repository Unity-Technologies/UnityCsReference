// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine;

namespace UnityEditor
{
    [NativeType(Header = "Modules/AssetDatabase/Editor/Public/AssetImportInProgressProxy.h")]
    class AssetImportInProgressProxy : UnityEngine.Object
    {
        public extern GUID asset
        {
            [NativeMethod("GetAsset")]
            get;
            [NativeMethod("SetAsset")]
            set;
        }

        [NativeMethod]
        public extern static bool IsProxyAsset(int instanceID);
    }

    [CustomEditor(typeof(AssetImportInProgressProxy))]
    class AssetImportInProgressProxyEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var proxy = (AssetImportInProgressProxy)target;

            if (GUILayout.Button("Import"))
            {
                var mainAsset = AssetDatabase.LoadMainAssetAtGUID(proxy.asset);
                Selection.activeObject = mainAsset;
                //@TODO: Properly call this from C++ when asset import completes...
                //EditorApplication.projectWindowChanged();
            }
        }
    }
}
