using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal interface IBuilderViewportWindow
    {
        BuilderSelection selection { get; }

        BuilderViewport viewport { get; }
        VisualElement documentRootElement { get; }
        BuilderCanvas canvas { get; }
    }
}
