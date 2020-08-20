namespace UnityEngine.UIElements.UIR
{
    static class Shaders
    {
        public static readonly string k_AtlasBlit;
        public static readonly string k_Editor;
        public static readonly string k_Runtime;
        public static readonly string k_RuntimeWorld;
        public static readonly string k_GraphView;

        static Shaders()
        {
            if (UIElementsPackageUtility.IsUIEPackageLoaded)
            {
                k_AtlasBlit = "Hidden/UIE-AtlasBlit";
                k_Editor = "Hidden/UIE-Editor";
                k_Runtime = "Hidden/UIE-Runtime";
                k_RuntimeWorld = "Hidden/UIE-RuntimeWorld";
                k_GraphView = "Hidden/UIE-GraphView";
            }
            else
            {
                k_AtlasBlit = "Hidden/Internal-UIRAtlasBlitCopy";
                k_Editor = "Hidden/UIElements/EditorUIE";
                k_Runtime = "Hidden/Internal-UIRDefault";
                k_RuntimeWorld = "Hidden/Internal-UIRDefaultWorld";
                k_GraphView = "Hidden/GraphView/GraphViewUIE";
            }
        }
    }
}
