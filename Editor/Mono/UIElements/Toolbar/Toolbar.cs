// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class Toolbar : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<Toolbar> {}

        internal static void SetToolbarStyleSheet(VisualElement ve)
        {
            ve.AddStyleSheetPath("StyleSheets/ToolbarCommon.uss");
            ve.AddStyleSheetPath("StyleSheets/Toolbar" + (EditorGUIUtility.isProSkin ? "Dark" : "Light") + ".uss");
        }

        public Toolbar()
        {
            SetToolbarStyleSheet(this);
        }
    }
}
