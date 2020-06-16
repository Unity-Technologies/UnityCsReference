// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Profiling.ModuleEditor
{
    class DragIndicator : VisualElement
    {
        const string k_UssClass_DragIndicatorContainer = "drag-indicator";
        const string k_UssClass_DragIndicatorLine = "drag-indicator__line";
        const string k_UssClass_DragIndicatorLineSpacer = "drag-indicator__line-spacer";
        const int k_DraggableIconLineCount = 3;

        public DragIndicator()
        {
            AddToClassList(k_UssClass_DragIndicatorContainer);

            for (int i = 0; i < k_DraggableIconLineCount; i++)
            {
                var line = new VisualElement();
                line.AddToClassList(k_UssClass_DragIndicatorLine);
                Add(line);

                if (i < (k_DraggableIconLineCount - 1))
                {
                    var lineSpacer = new VisualElement();
                    lineSpacer.AddToClassList(k_UssClass_DragIndicatorLineSpacer);
                    Add(lineSpacer);
                }
            }
        }
    }
}
