// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    // Shows builds as a comma-separated list of links.
    // Addressables' Build Details section uses this tag, and gets the builds from BuildAnalysisUtility.GetRelatedBuilds.
    // To rename it, keep the old name with [MovedFrom].
    [UxmlElement]
    internal sealed partial class RelatedBuildLinks : VisualElement
    {
        public static readonly string ussClassName = "related-build-links";
        public static readonly string linkUssClassName = ussClassName + "__link";
        public static readonly string separatorUssClassName = ussClassName + "__separator";

        private const string k_UssPath = "BuildAnalysis/StyleSheets/RelatedBuildLinks.uss";

        // Without a handler, clicking a link opens that build in the Build Analysis window.
        public event Action<GUID> buildClicked;

        public RelatedBuildLinks()
        {
            var styleSheet = EditorGUIUtility.LoadRequired(k_UssPath) as StyleSheet;
            styleSheets.Add(styleSheet);
            AddToClassList(ussClassName);
        }

        public void SetBuilds(IReadOnlyList<BuildEntry> builds)
        {
            Clear();
            foreach (var build in builds)
                AddLink(build.BuildSessionGUID, build.BuildName);
        }

        public void SetBuilds(IReadOnlyList<BuildReportSummary> builds)
        {
            Clear();
            foreach (var build in builds)
                AddLink(build.BuildSessionGUID, build.BuildName);
        }

        private void AddLink(GUID buildSessionGuid, string buildName)
        {
            if (childCount > 0)
                Add(CreateValue(", ", separatorUssClassName));

            var link = CreateValue($"<u><noparse>{buildName}</noparse></u>", linkUssClassName);
            link.RegisterCallback<ClickEvent>(_ => OnLinkClicked(buildSessionGuid));
            Add(link);
        }

        // The value class gives the labels the Build Details value style when inside a BuildDetailsGrid.
        private static Label CreateValue(string text, string ussClass)
        {
            var label = new Label(text);
            label.AddToClassList(BuildDetailsGrid.valueUssClassName);
            label.AddToClassList(ussClass);
            return label;
        }

        private void OnLinkClicked(GUID buildSessionGuid)
        {
            if (buildClicked != null)
                buildClicked(buildSessionGuid);
            else
                BuildAnalysisUtility.OpenWindow(buildSessionGuid);
        }
    }
}
