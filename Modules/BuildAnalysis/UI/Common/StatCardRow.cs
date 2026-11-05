// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    // Addressables' SummaryTabEmbedded.uxml uses this tag and its attributes.
    // To rename one, keep the old name with [MovedFrom] or [UxmlAttribute("new-name", "old-name")].
    [UxmlElement]
    internal sealed partial class StatCardRow : VisualElement
    {
        public static readonly string ussClassName = "stat-card-row";

        private const string k_UssPath = "BuildAnalysis/StyleSheets/StatCard.uss";

        public StatCardRow()
        {
            var styleSheet = EditorGUIUtility.LoadRequired(k_UssPath) as StyleSheet;
            styleSheets.Add(styleSheet);
            AddToClassList(ussClassName);
        }
    }
}
