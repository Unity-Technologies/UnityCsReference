using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    class VariableInfo
    {
        public string name { get; set; }
        public StylePropertyValue value { get; set; }
        public bool isEditorVar => value.sheet ? value.sheet.isUnityStyleSheet : false;
        public string description { get; set; }
    }
}
