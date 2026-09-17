// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;
using UnityEditor;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Center.Editor
{
    [UxmlElement]
    partial class PackageIcon : VisualElement
    {
        /// <summary>
        /// Raised when any <see cref="PackageIcon"/> is clicked, passing the opened package ID.
        /// </summary>
        [AutoStaticsCleanupOnCodeReload]
        internal static event Action<string> PackageOpened;

        string m_PackageId;

        [UxmlAttribute, CreateProperty]
        public string PackageId
        {
            get => m_PackageId;
            set
            {
                m_PackageId = value;
                tooltip = L10n.Tr($"Opens the {PackageId} package in the package manager window.", null);
            }
        }

        public PackageIcon()
        {
            this.AddManipulator(new Clickable(OnClick));
        }

        void OnClick()
        {
            UnityEditor.PackageManager.UI.Window.Open(PackageId);
            PackageOpened?.Invoke(PackageId);
        }
    }
}
