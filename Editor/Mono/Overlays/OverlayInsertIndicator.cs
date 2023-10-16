// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    class OverlayInsertIndicator : VisualElement
    {
        const string k_ClassName = "unity-overlay-insert-indicator";
        const string k_VerticalState = k_ClassName + "--vertical";
        const string k_Horizontal = k_ClassName + "--horizontal";
        const string k_VisualClass = k_ClassName + "__visual";
        const string k_FirstVisibleClass = k_ClassName + "--first-visible";

        public OverlayInsertIndicator()
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList(k_ClassName);

            var visual = new VisualElement();
            visual.AddToClassList(k_VisualClass);
            Add(visual);
        }

        public void Setup(bool horizontalContainer, bool firstVisible)
        {
            var verticalIndicator = horizontalContainer; // Horizontal container has vertical seperators 
            EnableInClassList(k_VerticalState, verticalIndicator);
            EnableInClassList(k_Horizontal, !verticalIndicator);
            style.width = new StyleLength(StyleKeyword.Auto);
            style.height = new StyleLength(StyleKeyword.Auto);
            EnableInClassList(k_FirstVisibleClass, firstVisible);
        }
    }
}
