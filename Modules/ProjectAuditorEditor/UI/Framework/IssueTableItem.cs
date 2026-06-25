// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    class IssueTableItem : TreeViewItem
    {
        public readonly string GroupName;
        public readonly ReportItem ReportItem;

        public int NumVisibleChildren;
        public int NumIgnoredChildren;

        // Cached severity/areas aggregations for group rows, set during tree construction.
        public bool GroupSeverityMixed;
        public Severity GroupSeverity;
        public bool GroupAreasMixed;
        public string GroupAreas;

        public virtual string DisplayName
        {
            get => displayName;
            set => displayName = value;
        }

        public IssueTableItem(int id, int depth, string displayName,
                              ReportItem reportItem, string groupName = null) : base(id, depth, displayName)
        {
            GroupName = groupName;
            ReportItem = reportItem;
        }

        public IssueTableItem(int id, int depth, string groupName) : base(id, depth)
        {
            GroupName = groupName;
        }

        public bool IsGroup()
        {
            return (ReportItem == null);
        }

        public string GetDisplayName()
        {
            if (IsGroup())
                return displayName;
            return ReportItem.Description;
        }

        public bool Find(ReportItem issue)
        {
            if (ReportItem == issue)
                return true;
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return children != null && children.FirstOrDefault(child => (child as IssueTableItem).ReportItem == issue) != null;
#pragma warning restore UA2001
        }
    }
}
