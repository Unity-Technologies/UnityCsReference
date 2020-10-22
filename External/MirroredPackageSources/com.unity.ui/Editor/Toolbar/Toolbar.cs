using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// A toolbar for tool windows.
    /// </summary>
    public class Toolbar : VisualElement
    {
        private static readonly string s_ToolbarDarkStyleSheetPath = "StyleSheets/Generated/ToolbarDark.uss.asset";
        private static readonly string s_ToolbarLightStyleSheetPath = "StyleSheets/Generated/ToolbarLight.uss.asset";

        private static readonly StyleSheet s_ToolbarDarkStyleSheet;
        private static readonly StyleSheet s_ToolbarLightStyleSheet;

        /// <summary>
        /// Instantiates a <see cref="Toolbar"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<Toolbar> {}

        static Toolbar()
        {
            s_ToolbarDarkStyleSheet = EditorGUIUtility.Load(UIElementsEditorUtility.GetStyleSheetPathForCurrentFont(s_ToolbarDarkStyleSheetPath)) as StyleSheet;
            s_ToolbarDarkStyleSheet.isUnityStyleSheet = true;

            s_ToolbarLightStyleSheet = EditorGUIUtility.Load(UIElementsEditorUtility.GetStyleSheetPathForCurrentFont(s_ToolbarLightStyleSheetPath)) as StyleSheet;
            s_ToolbarLightStyleSheet.isUnityStyleSheet = true;
        }

        internal static void SetToolbarStyleSheet(VisualElement ve)
        {
            if (EditorGUIUtility.isProSkin)
            {
                ve.styleSheets.Add(s_ToolbarDarkStyleSheet);
            }
            else
            {
                ve.styleSheets.Add(s_ToolbarLightStyleSheet);
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-toolbar";

        /// <summary>
        /// Constructor.
        /// </summary>
        public Toolbar()
        {
            AddToClassList(ussClassName);
            SetToolbarStyleSheet(this);
        }
    }
}
