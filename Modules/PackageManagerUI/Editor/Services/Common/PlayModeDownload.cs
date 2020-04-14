// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class PlayModeDownloadState : ScriptableSingleton<PlayModeDownloadState>
    {
        [SerializeField]
        public bool skipShowDialog;
    }

    [InitializeOnLoad]
    internal static class PlayModeDownload
    {
        static PlayModeDownload()
        {
            if (!PlayModeDownloadState.instance.skipShowDialog)
                EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }

        private static void PlayModeStateChanged(PlayModeStateChange state)
        {
            if (PlayModeDownloadState.instance.skipShowDialog)
                return;

            if (state == PlayModeStateChange.ExitingEditMode)
            {
                var assetStoreDownloadManager = ServicesContainer.instance.Resolve<AssetStoreDownloadManager>();
                if (assetStoreDownloadManager.IsAnyDownloadInProgress())
                {
                    var accept = EditorUtility.DisplayDialog(L10n.Tr("Package download in progress"),
                        L10n.Tr("Please note that entering Play Mode while Unity is downloading a package may impact performance"),
                        L10n.Tr("Got it"), L10n.Tr("Cancel"));

                    if (accept)
                    {
                        SetSkipDialog();
                    }
                    else
                    {
                        EditorApplication.isPlaying = false;
                    }
                }
            }
        }

        private static void SetSkipDialog()
        {
            PlayModeDownloadState.instance.skipShowDialog = true;
            // Checking for this event is no longer needed
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
        }

        public static bool CanBeginDownload()
        {
            if (!EditorApplication.isPlaying || PlayModeDownloadState.instance.skipShowDialog)
                return true;

            var accept = EditorUtility.DisplayDialog(L10n.Tr("Play Mode in progress"),
                L10n.Tr("Please note that making changes in the Package Manager while in Play Mode may impact performance."),
                L10n.Tr("Got it"), L10n.Tr("Cancel"));

            if (accept)
                SetSkipDialog();

            return accept;
        }
    }
}
