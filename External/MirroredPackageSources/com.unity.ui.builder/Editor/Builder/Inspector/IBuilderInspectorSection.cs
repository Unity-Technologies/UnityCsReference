using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal interface IBuilderInspectorSection
    {
        VisualElement root { get; }

        void Refresh();

        void Enable();

        void Disable();
    }
}
