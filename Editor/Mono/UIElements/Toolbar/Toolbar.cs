// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public class Toolbar : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<Toolbar> {}

        internal static void SetToolbarStyleSheet(VisualElement ve)
        {
            ve.AddStyleSheetPath("StyleSheets/ToolbarCommon.uss");
            ve.AddStyleSheetPath("StyleSheets/Toolbar" + (EditorGUIUtility.isProSkin ? "Dark" : "Light") + ".uss");
        }

        public static readonly string ussClassName = "unity-toolbar";

        public Toolbar()
        {
            AddToClassList(ussClassName);
            SetToolbarStyleSheet(this);
        }
    }
}
