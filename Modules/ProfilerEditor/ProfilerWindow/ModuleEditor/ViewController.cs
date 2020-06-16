// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Profiling.ModuleEditor
{
    class ViewController
    {
        public virtual void ConfigureView(VisualElement root)
        {
            CollectViewElements(root);
        }

        protected virtual void CollectViewElements(VisualElement root) {}
    }
}
