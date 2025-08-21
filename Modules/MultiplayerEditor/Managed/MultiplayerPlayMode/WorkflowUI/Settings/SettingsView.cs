// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class SettingsView : VisualElement
    {
        static readonly string UXML = $"{UXMLPaths.UIRoot}/SettingsView.uxml";

        public Toggle IsMppmActiveToggle => this.Q<Toggle>(nameof(IsMppmActiveToggle));
        public Toggle ShowLaunchScreenToggle => this.Q<Toggle>(nameof(ShowLaunchScreenToggle));
        public Toggle MutePlayersToggle => this.Q<Toggle>(nameof(MutePlayersToggle));
        public IntegerField AssetDatabaseRefreshTimeoutSlider => this.Q<IntegerField>(nameof(AssetDatabaseRefreshTimeoutSlider));

        public SettingsView()
        {
            (EditorGUIUtility.LoadRequired(UXML) as VisualTreeAsset).CloneTree(this);
        }
    }
}
