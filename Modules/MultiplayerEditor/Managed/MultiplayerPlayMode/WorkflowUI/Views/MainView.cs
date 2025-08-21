// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class MainView : VisualElement
    {
        public const string k_HasCompileErrorsClassName = "hasCompileErrors";
        public const string k_HasMPPMDisabled = "hasMPPMDisabled";
        public const string k_HasPlayerLaunchingClassName = "hasPlayerLaunching";
        public const string k_VirtualListViewName = "VirtualListView";
        static readonly string UXML = $"{UXMLPaths.UIRoot}/MainView.uxml";
        public readonly PlayersListView MainListView;
        public readonly PlayersListView VirtualListView;

        public MainView()
        {
            (EditorGUIUtility.LoadRequired(UXML) as VisualTreeAsset).CloneTree(this);

            MainListView = this.Q<PlayersListView>(nameof(MainListView));
            VirtualListView = this.Q<PlayersListView>(nameof(VirtualListView));
        }
    }
}
