// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEditorInternal
{
    class AddCurvesPopupHierarchyDataSource : TreeViewDataSource
    {
        public AddCurvesPopupHierarchyDataSource(TreeViewController treeView)
            : base(treeView)
        {
            showRootItem = false;
            rootIsCollapsable = false;
        }

        private void SetupRootNodeSettings()
        {
            showRootItem = false;
            SetExpanded(root, true);
        }

        public override void FetchData()
        {
            m_RootItem = null;
            if (AddCurvesPopup.s_State.selection.canAddCurves)
            {
                var state = AddCurvesPopup.s_State;
                AddBindingsToHierarchy(state.controlInterface.GetAnimatableBindings());
            }

            SetupRootNodeSettings();
            m_NeedRefreshRows = true;
        }

        private void AddBindingsToHierarchy(EditorCurveBinding[] bindings)
        {
            if (bindings == null || bindings.Length == 0)
            {
                m_RootItem = new AddCurvesPopupObjectNode(null, "", "");
                return;
            }

            var builder = new AddCurvesPopupHierarchyBuilder(AddCurvesPopup.s_State);
            for (int i = 0; i < bindings.Length; i++)
            {
                builder.Add(bindings[i]);
            }

            m_RootItem = builder.CreateTreeView();
        }

        public void UpdateData()
        {
            m_TreeView.ReloadData();
        }
    }
}
