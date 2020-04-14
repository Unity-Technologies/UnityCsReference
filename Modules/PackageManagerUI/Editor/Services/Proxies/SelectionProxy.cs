// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI
{
    internal class SelectionProxy
    {
        public virtual event Action onSelectionChanged = delegate {};

        public SelectionProxy()
        {
            Selection.selectionChanged += OnSelectionChanged;
        }

        public virtual UnityEngine.Object activeObject
        {
            get { return Selection.activeObject; }
            set { Selection.activeObject = value; }
        }

        private void OnSelectionChanged()
        {
            onSelectionChanged?.Invoke();
        }
    }
}
