// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.AssetImporters
{
    [NativeHeader("Modules/AssetPipelineEditor/ImportSettings/AssetImporterEditorUtility.h")]
    public abstract partial class AssetImporterEditor
    {
        [FreeFunction]
        private static extern Object CreateOrReloadInspectorCopy(int instanceID, Editor editor);
        [FreeFunction]
        private static extern void SaveUserData(int instanceID, Object userData);
        [FreeFunction]
        private static extern bool ReleaseInspectorCopy(int instanceID, Editor editor);
        [FreeFunction]
        private static extern void FixCacheCount(int instanceID, [Unmarshalled] int[] editors);
        [FreeFunction]
        private static extern int GetInspectorCopyCount(int instanceID);
        [FreeFunction("IsMetaDataSerializationEqual")]
        private static extern bool IsSerializedDataEqual([NotNull] Object source);
        [FreeFunction]
        private static extern void RevertObject([NotNull] Object source);
        [FreeFunction]
        private static extern void UpdateSavedData([NotNull] Object source);
        [FreeFunction]
        private static extern void FixSavedAssetbundleSettings(int instanceID, PropertyModification[] assetBundleProperties);

        [UsedByNativeCode]
        private static void UpdateUnsavedChangesState(Editor editor)
        {
            if (editor is AssetImporterEditor importerEditor)
            {
                importerEditor.hasUnsavedChanges = importerEditor.HasModified();
            }
        }
    }
}
