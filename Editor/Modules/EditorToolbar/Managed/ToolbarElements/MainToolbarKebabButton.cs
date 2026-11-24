// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [UxmlElement]
    partial class MainToolbarKebabButton : EditorToolbarButton
    {

        public MainToolbarKebabButton()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            pickingMode = PickingMode.Position;
            clicked += OnAction;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            clicked -= OnAction;
        }

        void OnAction()
        {
            MainToolbarWindow.instance.ShowMenu(worldBound);
        }
    }
}
