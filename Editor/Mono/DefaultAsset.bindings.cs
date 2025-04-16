// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor
{
    [NativeHeader("Modules/AssetPipelineEditor/Public/DefaultImporter.h")]
    // This class is public for users to be able to make custom editors for this type (see case 656580)
    public class DefaultAsset : UnityEngine.Object
    {
        internal enum ErrorCodes
        {
            NoError,
            PrefabCorruptedFileIds,
            ImportFail_TooMuch_Data,
            ImportFail_MetaFile_GUID_Mismatch,
            ImportFail_ImporterCrashed,
            ErrorCodeCount
        };

        private protected DefaultAsset() {}
        internal extern string message { get; }

        internal extern int errorCode { get; }

        internal extern bool isWarning {[NativeName("IsWarning")] get; }
    }

    [CustomEditor(typeof(DefaultAsset), isFallback = true)] // fallback so broad-matching user inspectors always win (e.g. case #656580)
    class DefaultAssetInspector : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var container = new VisualElement();

            var defaultAsset = (DefaultAsset)target;
            if (defaultAsset.message.Length > 0)
            {
                var helpBox = new HelpBox(
                    defaultAsset.message,
                    defaultAsset.isWarning ? HelpBoxMessageType.Warning : HelpBoxMessageType.Info);
                container.Add(helpBox);
            }
            if (defaultAsset.errorCode == (int)DefaultAsset.ErrorCodes.ImportFail_MetaFile_GUID_Mismatch)
            {
                var button = new Button(() =>
                {
                    var metaPath = AssetDatabase.GetAssetPath(target).Trim('/') + ".meta";
                    File.SetLastWriteTimeUtc(metaPath,DateTime.UtcNow);
                    AssetDatabase.Refresh();
                });
                button.text = "Fix Now";
                container.Add(button);
            }

            return container;
        }
    }
}
