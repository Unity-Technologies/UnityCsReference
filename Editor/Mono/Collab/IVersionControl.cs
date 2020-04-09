// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Collaboration
{
    internal interface IVersionControl
    {
        bool SupportsDownloads();
        bool SupportsAsyncChanges();
        bool OnEnableVersionControl();
        void OnDisableVersionControl();
        ChangeItem[] GetChanges();
        void MergeDownloadedFiles(bool isFullDownload);
        Collab.CollabStates GetAssetState(string assetGuid, string assetPath);
    }

    internal interface IVersionControl_V2 : IVersionControl
    {
        void RefreshAvailableLocalChangesSynchronous();
    }
}
