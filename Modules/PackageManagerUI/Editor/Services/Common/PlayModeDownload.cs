// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
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
        private static readonly string k_DefaultGotItButtonText = L10n.Tr("Got it");
        private static readonly string k_DefaultCancelButtonText = L10n.Tr("Cancel");

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
                var assetStoreDownloadManager = ServicesContainer.instance.Resolve<IAssetStoreDownloadManager>();
                var applicationProxy = ServicesContainer.instance.Resolve<IApplicationProxy>();
                if (assetStoreDownloadManager.IsAnyDownloadInProgress())
                {
                    var title = L10n.Tr("Package download in progress");
                    var message = L10n.Tr("Please note that entering Play Mode while Unity is downloading a package may impact performance");
                    var accept = applicationProxy.DisplayDialog("enterPlayModeWhenDownloadInProgress", title, message, k_DefaultGotItButtonText, k_DefaultCancelButtonText);

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

            var applicationProxy = ServicesContainer.instance.Resolve<IApplicationProxy>();
            var title = L10n.Tr("Play Mode in progress");
            var message = L10n.Tr("Please note that making changes in the Package Manager while in Play Mode may impact performance.");
            var accept = applicationProxy.DisplayDialog("startDownloadWhenInPlayMode", title, message, k_DefaultGotItButtonText, k_DefaultCancelButtonText);

            if (accept)
                SetSkipDialog();

            return accept;
        }
    }
}
