// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class BuildView : SummaryView
    {
        const string k_Message =
            $"Build information has been removed from {ProjectAuditor.DisplayName} and moved to the new Build Analysis window. New builds will automatically show up in the Build Analysis window.";
        static readonly GUIContent k_OpenBuildAnalysisButton = new GUIContent("Open Build Analysis");

        public override string Description => "";

        public BuildView(ViewManager viewManager) : base(viewManager)
        {
        }

        protected override bool MatchesSummaryFilter(ReportItem issue)
        {
            return false;
        }

        public override void DrawContent()
        {
            GUILayout.Space(16);
            GUILayout.BeginHorizontal();
            GUILayout.Space(16);
            Utility.DrawHelpBoxWithButton(k_Message, k_OpenBuildAnalysisButton, MessageType.Info, EditorInterop.OpenBuildAnalysisWindow);
            GUILayout.Space(16);
            GUILayout.EndHorizontal();
        }
    }
}
