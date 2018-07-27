// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental
{
    public abstract class UIElementsEditor : Editor
    {
        protected UIElementsEditor()
        {
        }

        public virtual VisualElement CreateInspectorGUI()
        {
            return null;
        }
    }
}
