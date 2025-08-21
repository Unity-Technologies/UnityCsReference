// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    class MainToolbarKebabButton : EditorToolbarButton
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : EditorToolbarButton.UxmlSerializedData
        {
            public override object CreateInstance() => new MainToolbarKebabButton();
        }

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
