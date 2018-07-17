// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class ToolbarSpacer : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<ToolbarSpacer> {}

        const string k_ClassName = "toolbarSpacer";
        public ToolbarSpacer()
        {
            Toolbar.SetToolbarStyleSheet(this);
            AddToClassList(k_ClassName);
        }
    }

    public class ToolbarFlexSpacer : ToolbarSpacer
    {
        public new class UxmlFactory : UxmlFactory<ToolbarFlexSpacer> {}

        const string k_ClassName = "toolbarFlexSpacer";
        public ToolbarFlexSpacer()
        {
            ClearClassList();
            AddToClassList(k_ClassName);
        }
    }
}
